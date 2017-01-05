// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests
{
    internal class ListBasedRecordSet : IRecordSet
    {
        private readonly IList<IRecord> _records;
        private int _count = 0;
        private readonly RecordSet _recordSet;

        public ListBasedRecordSet(IList<IRecord> records)
        {
            _records = records;
            _recordSet = new RecordSet(NextRecord);
        }

        public bool AtEnd => _count >= _records.Count;

        private IRecord NextRecord()
        {
            return AtEnd ? null : _records[_count++];
        }

        public IRecord Peek()
        {
            return _recordSet.Peek();
        }

        public IEnumerable<IRecord> Records()
        {
            return _recordSet.Records();
        }
    }

    internal static class RecordCreator
    {
        public static List<string> CreateKeys(int keySize=1)
        {
            var keys = new List<string>(keySize);
            for (int i = 0; i < keySize; i++)
            {
                keys.Add($"key{i}");
            }
            return keys;
        }

        public static IList<IRecord> CreateRecords(int recordSize, int keySize=1)
        {
            var keys = CreateKeys(keySize);
            return CreateRecords(recordSize, keys);
        }

        public static IList<IRecord> CreateRecords(int recordSize, List<string> keys)
        {
            var records = new List<IRecord>(recordSize);


            for (int j = 0; j < recordSize; j++)
            {
                var values = new List<object>();
                for (int i = 0; i < keys.Count; i++)
                {
                    values.Add($"record{j}:key{i}");
                }
                records.Add(new Record(keys, values.ToArray()));
            }
            return records;
        } 
    }

    public class RecordSetTests
    {
        public class RecordMethod
        {
            [Fact]
            public void ShouldReturnRecordsInOrder()
            {
                var records = RecordCreator.CreateRecords(5);
                var recordSet = new ListBasedRecordSet(records);

                int i = 0;
                foreach (var record in recordSet.Records())
                {
                    record[0].ValueAs<string>().Should().Be($"record{i++}:key0");
                }
                i.Should().Be(5);
            }

            [Fact]
            public void ShouldReturnRecordsAddedLatter()
            {
                var keys = RecordCreator.CreateKeys();
                var records = RecordCreator.CreateRecords(5, keys);
                var recordSet = new ListBasedRecordSet(records);

                // I add a new record after RecordSet is created
                var newRecord = new Record(keys, new object[] {"record5:key0"});
                records.Add(newRecord);

                int i = 0;
                foreach (var record in recordSet.Records())
                {
                    record[0].ValueAs<string>().Should().Be($"record{i++}:key0");
                }
                i.Should().Be(6);
            }
        }

        public class PeekMethod
        {
            [Fact]
            public void ShouldReturnNextRecordWithoutMovingCurrentRecord()
            {
                var records = RecordCreator.CreateRecords(5);
                var recordSet = new ListBasedRecordSet(records);

                var record = recordSet.Peek();
                record.Should().NotBeNull();
                record[0].ValueAs<string>().Should().Be("record0:key0");

                // not moving further no matter how many times are called
                record = recordSet.Peek();
                record.Should().NotBeNull();
                record[0].ValueAs<string>().Should().Be("record0:key0");
            }

            [Fact]
            public void ShouldReturnNextRecordAfterNextWithoutMovingCurrentRecord()
            {
                var records = RecordCreator.CreateRecords(5);
                var recordSet = new ListBasedRecordSet(records);

                recordSet.Records().Take(1).ToList();
                var record = recordSet.Peek();
                record.Should().NotBeNull();
                record[0].ValueAs<string>().Should().Be("record1:key0");

                // not moving further no matter how many times are called
                record = recordSet.Peek();
                record.Should().NotBeNull();
                record[0].ValueAs<string>().Should().Be("record1:key0");
            }

            [Fact]
            public void ShouldReturnNullIfAtEnd()
            {
                var records = RecordCreator.CreateRecords(5);
                var recordSet = new ListBasedRecordSet(records);
                recordSet.Records().Take(4).ToList(); // [0, 1, 2, 3]

                var record = recordSet.Peek();
                record.Should().NotBeNull();
                record[0].ValueAs<string>().Should().Be("record4:key0");

                var moveNext = recordSet.Records().GetEnumerator().MoveNext();
                moveNext.Should().BeTrue();

                record.Should().NotBeNull();
                record[0].ValueAs<string>().Should().Be("record4:key0");

                record = recordSet.Peek();
                record.Should().BeNull();
            }

            [Fact]
            public void ShouldBeTheSameWithEnumeratorMoveNextCurrent()
            {
                var records = RecordCreator.CreateRecords(2);
                var recordSet = new ListBasedRecordSet(records);

                IRecord record;
                IEnumerator<IRecord> enumerator;
                for (int i = 0; i < 2; i++)
                {
                    record = recordSet.Peek();
                    record.Should().NotBeNull();
                    record[0].ValueAs<string>().Should().Be($"record{i}:key0");

                    enumerator = recordSet.Records().GetEnumerator();
                    enumerator.MoveNext().Should().BeTrue();

                    // peeked record = current
                    enumerator.Current[0].ValueAs<string>().Should().Be($"record{i}:key0");
                }

                record = recordSet.Peek();
                record.Should().BeNull();

                enumerator = recordSet.Records().GetEnumerator();
                enumerator.MoveNext().Should().BeFalse();
                enumerator.Current.Should().BeNull();
            }
            
        }

        public class RecordNavigation
        {
            [Fact]
            public void EnumeratorResetShouldDoNothing()
            {
                var records = RecordCreator.CreateRecords(5);
                var recordSet = new ListBasedRecordSet(records);

                var ex = Xunit.Record.Exception(()=>recordSet.Records().GetEnumerator().Reset());
                ex.Should().BeOfType<NotSupportedException>();
            }

            [Fact]
            public void ShouldGetTheFirstRecordAndMoveToNextPosition()
            {
                var records = RecordCreator.CreateRecords(5);
                var recordSet = new ListBasedRecordSet(records);
                var recordStream = recordSet.Records();

                var record = recordStream.First();
                record[0].ValueAs<string>().Should().Be("record0:key0");

                record = recordStream.First();
                record[0].ValueAs<string>().Should().Be("record1:key0");
            }

            [Fact]
            public void ShouldAlwaysAdvanceRecordPosition()
            {
                var recordSet = new ListBasedRecordSet(RecordCreator.CreateRecords(5));
                var recordStream = recordSet.Records();

                var enumerable = recordStream.Take(1);
                var records = recordStream.Take(2).ToList();

                records[0][0].ValueAs<string>().Should().Be("record0:key0");
                records[1][0].ValueAs<string>().Should().Be("record1:key0");

                records = enumerable.ToList();
                records[0][0].ValueAs<string>().Should().Be("record2:key0");
            }
        }
    }
}
