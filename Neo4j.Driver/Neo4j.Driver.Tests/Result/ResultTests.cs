// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;
using Xunit.Abstractions;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests.Result;

public static class ResultTests
{
    private static class ResultCreator
    {
        public static InternalResult CreateResult(
            int keySize,
            int recordSize = 1,
            Func<IResultSummary> getSummaryFunc = null)
        {
            var keys = RecordCreator.CreateKeys(keySize);
            var records = RecordCreator.CreateRecords(recordSize, keys);

            return new InternalResult(
                new ListBasedRecordCursor(keys, () => records, getSummaryFunc),
                new BlockingExecutor());
        }
    }

    public class Constructor
    {
        [Fact]
        public void ShouldThrowArgumentNullExceptionIfCursorIsNull()
        {
            var ex = Xunit.Record.Exception(() => new InternalResult(null, new BlockingExecutor()));
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentNullException>();
        }
    }

    public class ConsumeMethod
    {
        // INFO: Rewritten because Result no longer takes IPeekingEnumerator in constructor
        [Fact]
        public void ShouldConsumeAllRecords()
        {
            var result = ResultCreator.CreateResult(0, 3);
            result.Consume();
            result.Count().Should().Be(0);
            result.Peek().Should().BeNull();
        }

        [Fact]
        public void ShouldConsumeSummaryCorrectly()
        {
            var getSummaryCalled = 0;
            var result = ResultCreator.CreateResult(
                1,
                0,
                () =>
                {
                    getSummaryCalled++;
                    return new FakeSummary();
                });

            result.Consume();
            getSummaryCalled.Should().Be(1);

            // the same if we call it multiple times
            result.Consume();
            getSummaryCalled.Should().Be(1);
        }

        [Fact]
        public void ShouldThrowNoExceptionWhenCallingMultipleTimes()
        {
            var result = ResultCreator.CreateResult(1);

            result.Consume();
            var ex = Xunit.Record.Exception(() => result.Consume());
            ex.Should().BeNull();
        }

        [Fact]
        public void ShouldConsumeRecordCorrectly()
        {
            var result = ResultCreator.CreateResult(1, 3);

            result.Consume();
            result.Count().Should().Be(0); // the records left after consume

            result.GetEnumerator().Current.Should().BeNull();
            result.GetEnumerator().MoveNext().Should().BeFalse();
        }
    }

    public class StreamingRecords
    {
        private readonly ITestOutputHelper _output;

        public StreamingRecords(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ShouldReturnRecords()
        {
            var recordYielder = new TestRecordYielder(5, 10, _output);
            var cursor =
                new InternalResult(
                    new ListBasedRecordCursor(
                        TestRecordYielder.Keys,
                        () => recordYielder.RecordsWithAutoLoad),
                    new BlockingExecutor());

            var records = cursor.ToList();
            records.Count.Should().Be(10);
        }

        [Fact]
        public void ShouldWaitForAllRecordsToArrive()
        {
            var recordYielder = new TestRecordYielder(5, 10, _output);

            var count = 0;
            var cursor =
                new InternalResult(
                    new ListBasedRecordCursor(TestRecordYielder.Keys, () => recordYielder.Records),
                    new BlockingExecutor());

            var t = Task.Factory.StartNew(
                () =>
                {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var item in cursor)
                    {
                        count++;
                    }

                    count.Should().Be(10);
                });

            while (count < 5)
            {
                Thread.Sleep(10);
            }

            recordYielder.AddNew(5);
            t.Wait();
        }

        [Fact]
        public void ShouldReturnRecordsImmediatelyWhenReady()
        {
            var recordYielder = new TestRecordYielder(5, 10, _output);
            var result =
                new InternalResult(
                    new ListBasedRecordCursor(TestRecordYielder.Keys, () => recordYielder.Records),
                    new BlockingExecutor());

            var temp = result.Take(5);
            var records = temp.ToList();
            records.Count.Should().Be(5);
        }

        private class TestRecordYielder
        {
            private readonly ITestOutputHelper _output;
            private readonly IList<Record> _records = new List<Record>();
            private readonly int _total;

            public TestRecordYielder(int count, int total, ITestOutputHelper output)
            {
                Add(count);
                _total = total;
                _output = output;
            }

            public static string[] Keys => new[] { "Test", "Keys" };

            public IEnumerable<Record> Records
            {
                get
                {
                    var i = 0;
                    while (i < _total)
                    {
                        while (i == _records.Count)
                        {
                            _output.WriteLine($"{DateTime.Now:HH:mm:ss.fff} -> Waiting for more Records");

                            Thread.Sleep(50);
                        }

                        yield return _records[i];

                        i++;
                    }
                }
            }

            public IEnumerable<Record> RecordsWithAutoLoad
            {
                get
                {
                    var i = 0;
                    while (i < _total)
                    {
                        while (i == _records.Count)
                        {
                            _output.WriteLine($"{DateTime.Now:HH:mm:ss.fff} -> Waiting for more Records");

                            Thread.Sleep(500);
                            AddNew(1);
                            _output.WriteLine($"{DateTime.Now:HH:mm:ss.fff} -> Record arrived");
                        }

                        yield return _records[i];

                        i++;
                    }
                }
            }

            public void AddNew(int count)
            {
                Add(count);
            }

            private void Add(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    _records.Add(TestRecord.Create(Keys, new object[] { "Test", 123 }));
                }
            }
        }

        private class FuncBasedRecordSet : IRecordSet
        {
            private readonly Func<IEnumerable<IRecord>> _getRecords;

            public FuncBasedRecordSet(Func<IEnumerable<IRecord>> getRecords)
            {
                _getRecords = getRecords;
            }

            public bool AtEnd => throw new NotImplementedException();

            public IRecord Peek()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IRecord> Records()
            {
                return _getRecords();
            }
        }
    }

    public class ResultNavigation
    {
        [Fact]
        public void ShouldGetTheFirstRecordAndMoveToNextPosition()
        {
            var result = ResultCreator.CreateResult(1, 3);
            var record = result.First();
            record[0].As<string>().Should().Be("record0:key0");

            record = result.First();
            record[0].As<string>().Should().Be("record1:key0");
        }

        [Fact]
        public void ShouldAlwaysAdvanceRecordPosition()
        {
            var result = ResultCreator.CreateResult(1, 3);
            var enumerable = result.Take(1);
            var records = result.Take(2).ToList();

            records[0][0].As<string>().Should().Be("record0:key0");
            records[1][0].As<string>().Should().Be("record1:key0");

            records = enumerable.ToList();
            records[0][0].As<string>().Should().Be("record2:key0");
        }
    }

    public class SummaryProperty
    {
        [Fact]
        public void ShouldCallGetSummaryWhenGetSummaryIsNotNull()
        {
            var getSummaryCalled = false;
            var result = ResultCreator.CreateResult(
                1,
                0,
                () =>
                {
                    getSummaryCalled = true;
                    return null;
                });

            // ReSharper disable once UnusedVariable
            var summary = result.Consume();

            getSummaryCalled.Should().BeTrue();
        }

        [Fact]
        public void ShouldReturnNullWhenGetSummaryIsNull()
        {
            var result = ResultCreator.CreateResult(1, 0);

            result.Consume().Should().BeNull();
        }

        [Fact]
        public void ShouldReturnExistingSummaryWhenSummaryHasBeenRetrieved()
        {
            var getSummaryCalled = 0;
            var result = ResultCreator.CreateResult(
                1,
                0,
                () =>
                {
                    getSummaryCalled++;
                    return new FakeSummary();
                });

            // ReSharper disable once NotAccessedVariable
            var summary = result.Consume();
            // ReSharper disable once RedundantAssignment
            summary = result.Consume();
            getSummaryCalled.Should().Be(1);
        }
    }

    public class PeekMethod
    {
        [Fact]
        public void ShouldReturnNextRecordWithoutMovingCurrentRecord()
        {
            var result = ResultCreator.CreateResult(1);
            var record = result.Peek();
            record.Should().NotBeNull();

            result.GetEnumerator().Current.Should().BeNull();
        }

        [Fact]
        public void ShouldReturnNullIfAtEnd()
        {
            var result = ResultCreator.CreateResult(1);
            result.Take(1).ToList();
            var record = result.Peek();
            record.Should().BeNull();
        }
    }

    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    private class FakeSummary : IResultSummary
    {
        public Query Query { get; }
        public ICounters Counters { get; }
        public QueryType QueryType { get; }
        public bool HasPlan { get; }
        public bool HasProfile { get; }
        public IPlan Plan { get; }
        public IProfiledPlan Profile { get; }
        public IList<INotification> Notifications { get; }
        public IList<IGqlStatusObject> GqlStatusObjects { get; }
        public TimeSpan ResultAvailableAfter { get; }
        public TimeSpan ResultConsumedAfter { get; }
        public IServerInfo Server { get; }
        public IDatabaseInfo Database { get; }
    }
}
