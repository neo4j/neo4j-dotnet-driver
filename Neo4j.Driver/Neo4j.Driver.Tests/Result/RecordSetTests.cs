using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests.Result
{
    internal class ListBasedRecordSet : IRecordSet
    {
        private readonly IList<IRecord> _records;
        private readonly RecordSet _recordSet;

        public int Position => _recordSet.Position;

        public ListBasedRecordSet(IList<IRecord> records)
        {
            _records = records;
            _recordSet = new RecordSet(_records, ()=>AtEnd);
        }

        public bool AtEnd => _recordSet.Position >= _records.Count;
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
        public static IList<string> CreateKeys(int keySize=1)
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

        public static IList<IRecord> CreateRecords(int recordSize, IList<string> keys)
        {
            var records = new List<IRecord>(recordSize);


            for (int j = 0; j < recordSize; j++)
            {
                var values = new List<object>();
                for (int i = 0; i < keys.Count; i++)
                {
                    values.Add($"record{j}:key{i}");
                }
                records.Add(new Record(keys.ToArray(), values.ToArray()));
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
                    record[0].As<string>().Should().Be($"record{i++}:key0");
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
                var newRecord = new Record(keys.ToArray(), new object[] {"record5:key0"});
                records.Add(newRecord);

                int i = 0;
                foreach (var record in recordSet.Records())
                {
                    record[0].As<string>().Should().Be($"record{i++}:key0");
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
                record[0].As<string>().Should().Be("record0:key0");

                // not moving further no matter how many times are called
                record = recordSet.Peek();
                record.Should().NotBeNull();
                record[0].As<string>().Should().Be("record0:key0");
            }

            [Fact]
            public void ShouldReturnNextRecordAfterNextWithoutMovingCurrentRecord()
            {
                var records = RecordCreator.CreateRecords(5);
                var recordSet = new ListBasedRecordSet(records);

                recordSet.Records().Take(1).ToList();
                var record = recordSet.Peek();
                record.Should().NotBeNull();
                record[0].As<string>().Should().Be("record1:key0");

                // not moving further no matter how many times are called
                record = recordSet.Peek();
                record.Should().NotBeNull();
                record[0].As<string>().Should().Be("record1:key0");
            }

            [Fact]
            public void ShouldReturnNullIfAtEnd()
            {
                var records = RecordCreator.CreateRecords(5);
                var recordSet = new ListBasedRecordSet(records);
                recordSet.Records().Take(4).ToList(); // [0, 1, 2, 3]

                var record = recordSet.Peek();
                record.Should().NotBeNull();
                record[0].As<string>().Should().Be("record4:key0");

                var moveNext = recordSet.Records().GetEnumerator().MoveNext();
                moveNext.Should().BeTrue();

                record.Should().NotBeNull();
                record[0].As<string>().Should().Be("record4:key0");

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
                    record[0].As<string>().Should().Be($"record{i}:key0");

                    enumerator = recordSet.Records().GetEnumerator();
                    enumerator.MoveNext().Should().BeTrue();

                    // peeked record = current
                    enumerator.Current[0].As<string>().Should().Be($"record{i}:key0");
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
                record[0].As<string>().Should().Be("record0:key0");

                record = recordStream.First();
                record[0].As<string>().Should().Be("record1:key0");
            }

            [Fact]
            public void ShouldAlwaysAdvanceRecordPosition()
            {
                var recordSet = new ListBasedRecordSet(RecordCreator.CreateRecords(5));
                var recordStream = recordSet.Records();

                var enumerable = recordStream.Take(1);
                var records = recordStream.Take(2).ToList();

                records[0][0].As<string>().Should().Be("record0:key0");
                records[1][0].As<string>().Should().Be("record1:key0");

                records = enumerable.ToList();
                records[0][0].As<string>().Should().Be("record2:key0");
            }
        }
    }
}
