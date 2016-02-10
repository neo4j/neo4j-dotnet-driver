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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Tests
{
    using Neo4j.Driver.Exceptions;

    class ResultCreator
    {
        public static ResultCursor CreateResult(int keySize, int recordSize=1, Func<IResultSummary> getSummaryFunc = null)
        {
            var records = new List<Record>(recordSize);

            var keys = new List<string>(keySize);
            for (int i = 0; i < keySize; i++)
            {
                keys.Add($"str{i}");
            }

            for (int j = 0; j < recordSize; j++)
            {
                var values = new List<dynamic>();
                for (int i = 0; i < keySize; i++)
                {
                    values.Add(i);
                }
                records.Add(new Record(keys.ToArray(), values.ToArray()));
            }
            
            return new ResultCursor(keys.ToArray(), records, getSummaryFunc);
        }
    }
    public class ResultCursorTests
    {
        public class Constructor
        {
            [Fact]
            public void ShouldThrowArgumentNullExceptionIfRecordsIsNull()
            {
                var ex = Xunit.Record.Exception(() => new ResultCursor(new string[] {"test"}, (IEnumerable<Record>)null));
                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldThrowArgumentNullExceptionIfKeysIsNull()
            {
                var ex = Xunit.Record.Exception(() => new ResultCursor(null, new List<Record>()));
                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldSetKeysProperlyIfKeysNotNull()
            {
                var result = new ResultCursor(new string[] {"test"}, new List<Record>());
                result.Keys.Should().HaveCount(1);
                result.Keys.Should().Contain("test");
            }

            [Fact]
            public void ShouldGetEnumeratorFromRecords()
            {
                Mock<IEnumerable<Record>> mock = new Mock<IEnumerable<Record>>();
                var cursor = new ResultCursor(new string[] {"test"}, mock.Object);

                mock.Verify(x => x.GetEnumerator(), Times.Once);
            }
        }

        public class CloseMethod
        {
            [Fact]
            public void ShouldSetOpenToFalse()
            {
                var cursor = ResultCreator.CreateResult(1);
                cursor.Close();
                cursor.IsOpen().Should().BeFalse();
            }

            [Fact]
            public void ShouldCallDiscardOnEnumberator()
            {
                Mock<IPeekingEnumerator<Record>> mock = new Mock<IPeekingEnumerator<Record>>();

                var cursor = new ResultCursor(new string[] { "test" }, mock.Object);
                cursor.Close();
                mock.Verify(x => x.Discard(), Times.Once);
            }

            [Fact]
            public void ShouldThrowInvalidOperationExceptionWhenCallingCloseMultipleTimes()
            {
              
                var cursor = ResultCreator.CreateResult(1);

                cursor.Close();
                var ex = Xunit.Record.Exception(() => cursor.Close());
                ex.Should().BeOfType<InvalidOperationException>();
            }
        }

        public class NextMethod
        {
            [Fact]
            public void ShouldThrowExceptionIfCursorIsClosed()
            {
                var cursor = ResultCreator.CreateResult(1);

                cursor.Close();
                var ex = Xunit.Record.Exception(() => cursor.Next());
                ex.Should().BeOfType<InvalidOperationException>();
            }

            [Fact]
            public void ShouldReturnTrueAndMoveCursorToNext()
            {
                Mock<IPeekingEnumerator<Record>> mock = new Mock<IPeekingEnumerator<Record>>();
                mock.Setup(x => x.HasNext()).Returns(true);
                var cursor = new ResultCursor(new string[] { "test" }, mock.Object);

                cursor.Next().Should().BeTrue();
                mock.Verify(x => x.HasNext(), Times.Once);
                mock.Verify(x => x.Next(), Times.Once);
                cursor.Position.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnFalseAndNotMoveCursorIfLast()
            {
                Mock<IPeekingEnumerator<Record>> mock = new Mock<IPeekingEnumerator<Record>>();
                mock.Setup(x => x.HasNext()).Returns(false);
                var cursor = new ResultCursor(new string[] { "test" }, mock.Object);

                cursor.Next().Should().BeFalse();
                mock.Verify(x => x.HasNext(), Times.Once);
                mock.Verify(x => x.Next(), Times.Never);
                cursor.Position.Should().Be(-1);
            }

            [Fact(Skip = "Pending API Review")]
            public void ShouldDiscardIfLimitReached()
            {
                throw new NotImplementedException();
            }
        }

        public class RecordMethod
        {
            [Fact]
            public void ShouldReturnRecordIfHasRecord()
            {
                var cursor = ResultCreator.CreateResult(1);
                cursor.Next();
                cursor.Record().Should().NotBeNull();
            }

            [Fact]
            public void ShouldThrowInvalidOperationExceptionIfHasNoRecord()
            {
                var cursor = ResultCreator.CreateResult(1);
                var ex = Xunit.Record.Exception(() => cursor.Record());
                ex.Should().BeOfType<InvalidOperationException>();
            }
        }

        public class StreamMethod
        {
            [Fact]
            public void ShouldReturnRecords()
            {
                var cursor = ResultCreator.CreateResult(2,2);
                var records = cursor.Stream().ToList();
                records.Count.Should().Be(2);
                Assert.Equal(0, records[0].Values["str0"]);
                Assert.Equal(1, records[1].Values["str1"]);
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
                        _records.Add(new Record(Keys, new dynamic[] { "Test", 123 }));
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
                var cursor = new ResultCursor( TestRecordYielder.Keys, recordYielder.RecordsWithAutoLoad);
                var records = cursor.Stream().ToList();
                records.Count.Should().Be(10);
            }

            [Fact]
            public void ShouldWaitForAllRecordsArrive()
            {
                var recordYielder = new TestRecordYielder(5, 10, _output);

                int count = 0;
                var cursor = new ResultCursor(TestRecordYielder.Keys, recordYielder.Records);
                var t =  Task.Factory.StartNew(() =>
                {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var item in cursor.Stream())
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
                var cursor = new ResultCursor(TestRecordYielder.Keys, recordYielder.Records);
                var records = cursor.Stream().Take(5).ToList();
                records.Count.Should().Be(5);
            }
        }

        public class SummaryProperty
        {
            [Fact]
            public void ShouldThrowClientExceptionWhenNotAtEnd()
            {
                var cursor = ResultCreator.CreateResult(1, 1);
                cursor.AtEnd.Should().BeFalse();

                var ex = Xunit.Record.Exception(() => cursor.Summary);
                ex.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldCallGetSummaryWhenGetSummaryIsNotNull()
            {
                bool getSummaryCalled = false;
                var cursor = ResultCreator.CreateResult(1, 0, () => { getSummaryCalled = true; return null; });

                // ReSharper disable once UnusedVariable
                var summary = cursor.Summary;

                getSummaryCalled.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnNullWhenGetSummaryIsNull()
            {
                var cursor = ResultCreator.CreateResult(1, 0, null);

                cursor.Summary.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnExistingSummaryWhenSummaryHasBeenRetrieved()
            {
                int getSummaryCalled = 0;
                var cursor = ResultCreator.CreateResult(1, 0, () => { getSummaryCalled++; return new FakeSummary(); });

                // ReSharper disable once NotAccessedVariable
                var summary = cursor.Summary;
                // ReSharper disable once RedundantAssignment
                summary = cursor.Summary;
                getSummaryCalled.Should().Be(1);
            }

            private class FakeSummary : IResultSummary {
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
}
