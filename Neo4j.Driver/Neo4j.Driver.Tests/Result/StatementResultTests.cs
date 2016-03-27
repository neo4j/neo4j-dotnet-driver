//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Neo4j.Driver.Internal.Result;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests
{
    class ResultCreator
    {
        public static StatementResult CreateResult(int keySize, int recordSize=1, Func<IResultSummary> getSummaryFunc = null)
        {
            var records = new List<Record>(recordSize);

            var keys = new List<string>(keySize);
            for (int i = 0; i < keySize; i++)
            {
                keys.Add($"str{i}");
            }

            for (int j = 0; j < recordSize; j++)
            {
                var values = new List<object>();
                for (int i = 0; i < keySize; i++)
                {
                    values.Add(i);
                }
                records.Add(new Record(keys.ToArray(), values.ToArray()));
            }
            
            return new StatementResult(keys.ToArray(), records, getSummaryFunc);
        }
    }
    public class StatementResultTests
    {
        public class Constructor
        {
            [Fact]
            public void ShouldThrowArgumentNullExceptionIfRecordsIsNull()
            {
                var ex = Xunit.Record.Exception(() => new StatementResult(new string[] {"test"}, (IEnumerable<Record>)null));
                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldThrowArgumentNullExceptionIfKeysIsNull()
            {
                var ex = Xunit.Record.Exception(() => new StatementResult(null, new List<Record>()));
                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldSetKeysProperlyIfKeysNotNull()
            {
                var result = new StatementResult(new string[] {"test"}, new List<Record>());
                result.Keys.Should().HaveCount(1);
                result.Keys.Should().Contain("test");
            }

            [Fact]
            public void ShouldGetEnumeratorFromRecords()
            {
                Mock<IEnumerable<Record>> mock = new Mock<IEnumerable<Record>>();
                var result = new StatementResult(new string[] {"test"}, mock.Object);

                mock.Verify(x => x.GetEnumerator(), Times.Once);
            }
        }

        public class ConsumeMethod
        {
            [Fact]
            public void ShouldCallDiscardOnEnumberator()
            {
                Mock<IPeekingEnumerator<Record>> mock = new Mock<IPeekingEnumerator<Record>>();

                var result = new StatementResult(new string[] { "test" }, mock.Object);
                result.Consume();
                mock.Verify(x => x.Consume(), Times.Once);
            }

            [Fact]
            public void ShouldConsumeSummaryCorrectly()
            {
                int getSummaryCalled = 0;
                var result = ResultCreator.CreateResult(1, 0, () => { getSummaryCalled++; return new FakeSummary(); });


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
                result.Position.Should().Be(3);

                result.GetEnumerator().Current.Should().BeNull();
                result.GetEnumerator().MoveNext().Should().BeFalse();
            }
        }

        public class StreamingRecords
        {
            private readonly ITestOutputHelper _output;

            private class TestRecordYielder
            {
                private readonly IList<Record> _records = new List<Record>();
                private readonly int _total = 0;

                private readonly ITestOutputHelper _output;
                public static string[] Keys => new[] {"Test", "Keys"};

                public TestRecordYielder(int count, int total, ITestOutputHelper output)
                {
                   Add(count);
                    _total = total;
                    _output = output;
                }

                public void AddNew(int count)
                {
                    Add(count);
                }

                private void Add(int count)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _records.Add(new Record(Keys, new object[] { "Test", 123 }));
                    }
                }

                public IEnumerable<Record> Records
                {
                    get
                    {
                        int i = 0;
                        while (i < _total)
                        {
                            while (i == _records.Count)
                            {
                                _output.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} -> Waiting for more Records");
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
                        int i = 0;
                        while (i < _total)
                        {
                            while (i == _records.Count)
                            {
                                _output.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} -> Waiting for more Records");
                                Thread.Sleep(500);
                                AddNew(1);
                                _output.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} -> Record arrived");
                            }

                            yield return _records[i];
                            i++;
                        }
                    }
                }
            }

            public StreamingRecords(ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public void ShouldReturnRecords()
            {
                var recordYielder = new TestRecordYielder(5, 10, _output);
                var cursor = new StatementResult( TestRecordYielder.Keys, recordYielder.RecordsWithAutoLoad);
                var records = cursor.ToList();
                records.Count.Should().Be(10);
            }

            [Fact]
            public void ShouldWaitForAllRecordsToArrive()
            {
                var recordYielder = new TestRecordYielder(5, 10, _output);

                int count = 0;
                var cursor = new StatementResult(TestRecordYielder.Keys, recordYielder.Records);
                var t =  Task.Factory.StartNew(() =>
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
                var result = new StatementResult(TestRecordYielder.Keys, recordYielder.Records);
                var temp = result.Take(5);
                var records = temp.ToList();
                records.Count.Should().Be(5);
            }
        }

        public class SummaryProperty
        {
            [Fact]
            public void ShouldThrowInvalidOperationExceptionWhenNotAtEnd()
            {
                var result = ResultCreator.CreateResult(1);
                result.AtEnd.Should().BeFalse();

                var ex = Xunit.Record.Exception(() => result.Summary);
                ex.Should().BeOfType<InvalidOperationException>();
            }

            [Fact]
            public void ShouldCallGetSummaryWhenGetSummaryIsNotNull()
            {
                bool getSummaryCalled = false;
                var result = ResultCreator.CreateResult(1, 0, () => { getSummaryCalled = true; return null; });

                // ReSharper disable once UnusedVariable
                var summary = result.Summary;

                getSummaryCalled.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnNullWhenGetSummaryIsNull()
            {
                var result = ResultCreator.CreateResult(1, 0);

                result.Summary.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnExistingSummaryWhenSummaryHasBeenRetrieved()
            {
                int getSummaryCalled = 0;
                var result = ResultCreator.CreateResult(1, 0, () => { getSummaryCalled++; return new FakeSummary(); });

                // ReSharper disable once NotAccessedVariable
                var summary = result.Summary;
                // ReSharper disable once RedundantAssignment
                summary = result.Summary;
                getSummaryCalled.Should().Be(1);
            }
        }

        public class SingleMethod
        {
            [Fact]
            public void ShouldThrowInvalidOperationExceptionIfNoRecordFound()
            {
                var result = new StatementResult(new [] { "test" }, new List<Record>());
                var ex = Xunit.Record.Exception(() => result.Single());
                ex.Should().BeOfType<InvalidOperationException>();
                ex.Message.Should().Be("No record found.");
            }

            [Fact]
            public void ShouldThrowInvalidOperationExceptionIfMoreThanOneRecordFound()
            {
                var result = ResultCreator.CreateResult(1, 2);
                var ex = Xunit.Record.Exception(() => result.Single());
                ex.Should().BeOfType<InvalidOperationException>();
                ex.Message.Should().Be("More than one record found.");
            }

            [Fact]
            public void ShouldThrowInvalidOperationExceptionIfNotTheFistRecord()
            {
                var result = ResultCreator.CreateResult(1, 2);
                var enumerator = result.GetEnumerator();
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().NotBeNull();

                var ex = Xunit.Record.Exception(() => result.Single());
                ex.Should().BeOfType<InvalidOperationException>();
                ex.Message.Should().Be("The first record is already consumed.");
            }

            [Fact]
            public void ShouldReturnRecordIfSingle()
            {
                var result = ResultCreator.CreateResult(1);
                var record = result.Single();
                record.Should().NotBeNull();
                record.Keys.Count.Should().Be(1);
            }
        }

        public class PeekMethod
        {
            [Fact]
            public void ShouldReturnNextRecordWithoutMovingCurrentRecord()
            {
                var result = ResultCreator.CreateResult(1);
                result.Position.Should().Be(-1);
                var record = result.Peek();
                record.Should().NotBeNull();

                result.Position.Should().Be(-1);
                result.GetEnumerator().Current.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullIfAtEnd()
            {
                var result = ResultCreator.CreateResult(1);
                result.Take(1).ToList();
                result.Position.Should().Be(0);
                var record = result.Peek();
                record.Should().BeNull();
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldNotConsumeRecordStream()
            {
                var result = ResultCreator.CreateResult(1, 2);

                result.Dispose();

                result.AtEnd.Should().BeTrue();
                result.Position.Should().Be(-1);
                result.GetEnumerator().MoveNext().Should().BeFalse();
                result.GetEnumerator().Current.Should().BeNull();
            }

            [Fact]
            public void ShouldNotPullSummary()
            {
                int getSummaryCalled = 0;
                var result = ResultCreator.CreateResult(1, 0, () => { getSummaryCalled++; return new FakeSummary(); });

                result.Dispose();
                getSummaryCalled.Should().Be(0);
            }

            [Fact]
            public void PeakShouldThrowAfterDispose()
            {
                var result = ResultCreator.CreateResult(1, 2);
                result.Dispose();

                Assert.Throws<ObjectDisposedException>(() => result.Peek());
            }

            [Fact]
            public void ConsumeShouldThrowAfterDispose()
            {
                var result = ResultCreator.CreateResult(1, 2);
                result.Dispose();

                Assert.Throws<ObjectDisposedException>(() => result.Consume());
            }

            [Fact]
            public void SingleShouldThrowAfterDispose()
            {
                var result = ResultCreator.CreateResult(1, 2);
                result.Dispose();

                Assert.Throws<ObjectDisposedException>(() => result.Single());
            }
        }

        private class FakeSummary : IResultSummary
        {
            public Statement Statement { get; }
            public ICounters Counters { get; }
            public StatementType StatementType { get; }
            public bool HasPlan { get; }
            public bool HasProfile { get; }
            public IPlan Plan { get; }
            public IProfiledPlan Profile { get; }
            public IList<INotification> Notifications { get; }
        }
    }
}
