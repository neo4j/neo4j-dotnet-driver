﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Xunit.Abstractions;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests
{
    public class ResultCursorTests
    {
        public class Constructor
        {
            [Fact]
            public async Task ShouldSetKeysProperlyIfKeysNotNull()
            {
                var stream = new Mock<IResultStream>();
                stream.Setup(x => x.NextRecordAsync()).Returns(NextRecordFromEnum(new List<IRecord>().GetEnumerator()));
                stream.Setup(x => x.GetKeysAsync()).Returns(() => Task.FromResult(new[] {"test"}));

                var result =
                    new ResultCursor(stream.Object);
                var keys = await result.KeysAsync();

                keys.Should().HaveCount(1).And.Contain("test");
            }
        }

        public class Keys
        {
            [Fact]
            public async Task KeysShouldReturnTheSameGivenInConstructor()
            {
                var result = ResultCursorCreator.CreateResultCursor(1);
                var keys = await result.KeysAsync();

                keys.Should().HaveCount(1).And.Contain("key0");
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
            public async void ShouldReturnRecords()
            {
                var recordYielder = new TestRecordYielder(5, 10, _output);
                var recordYielderEnum = recordYielder.RecordsWithAutoLoad.GetEnumerator();

                var stream = new Mock<IResultStream>();
                stream.Setup(x => x.NextRecordAsync()).Returns(() => NextRecordFromEnum(recordYielderEnum));
                stream.Setup(x => x.GetKeysAsync()).Returns(() => Task.FromResult(TestRecordYielder.Keys.ToArray()));

                var cursor = new ResultCursor(stream.Object);
                var records = new List<IRecord>();
                while (await cursor.FetchAsync())
                {
                    records.Add(cursor.Current);
                }

                records.Count.Should().Be(10);
            }

            [Fact]
            public async void ShouldWaitForAllRecordsToArrive()
            {
                var recordYielder = new TestRecordYielder(5, 10, _output);
                var recordYielderEnum = recordYielder.Records.GetEnumerator();

                var stream = new Mock<IResultStream>();
                stream.Setup(x => x.NextRecordAsync()).Returns(() => NextRecordFromEnum(recordYielderEnum));
                stream.Setup(x => x.GetKeysAsync()).Returns(() => Task.FromResult(TestRecordYielder.Keys.ToArray()));

                int count = 0;
                var cursor = new ResultCursor(stream.Object);
                var t = Task.Factory.StartNew(async () =>
                {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    while (await cursor.FetchAsync())
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

                await t;
            }

            [Fact]
            public async void ShouldReturnRecordsImmediatelyWhenReady()
            {
                var recordYielder = new TestRecordYielder(5, 10, _output);
                var recordYielderEnum = recordYielder.Records.GetEnumerator();

                var stream = new Mock<IResultStream>();
                stream.Setup(x => x.NextRecordAsync()).Returns(() => NextRecordFromEnum(recordYielderEnum));
                stream.Setup(x => x.GetKeysAsync()).Returns(() => Task.FromResult(TestRecordYielder.Keys.ToArray()));

                var result = new ResultCursor(stream.Object);
                var records = new List<IRecord>();
                var count = 5;
                while (count > 0 && await result.FetchAsync())
                {
                    records.Add(result.Current);
                    count--;
                }

                records.Count.Should().Be(5);
            }

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
                        _records.Add(new Record(Keys, new object[] {"Test", 123}));
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
                                _output.WriteLine(
                                    $"{DateTime.Now.ToString("HH:mm:ss.fff")} -> Waiting for more Records");
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
                                _output.WriteLine(
                                    $"{DateTime.Now.ToString("HH:mm:ss.fff")} -> Waiting for more Records");
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
        }

        public class ResultNavigation
        {
            [Fact]
            public async void ShouldGetTheFirstRecordAndMoveToNextPosition()
            {
                var result = ResultCursorCreator.CreateResultCursor(1, 3);
                var read = await result.FetchAsync();
                read.Should().BeTrue();
                var record = result.Current;
                record[0].As<string>().Should().Be("record0:key0");

                read = await result.FetchAsync();
                read.Should().BeTrue();
                record = result.Current;
                record[0].As<string>().Should().Be("record1:key0");
            }

        }

        public class ConsumeAsyncMethod
        {
            [Fact]
            public async void ShouldCallGetSummary()
            {
                bool getSummaryCalled = false;
                var result = ResultCursorCreator.CreateResultCursor(1, 0, () =>
                {
                    getSummaryCalled = true;
                    return Task.FromResult((IResultSummary) null);
                });

                // ReSharper disable once UnusedVariable
                var summary = await result.ConsumeAsync();

                getSummaryCalled.Should().BeTrue();
            }

            [Fact]
            public async void ShouldOnlyThrowErrorOnce()
            {
                int getSummaryCalled = 0;
                var result = ResultCursorCreator.CreateResultCursor(1, 0,
                    () => getSummaryCalled++ == 0
                        ? Task.FromException<IResultSummary>(new Exception("error!"))
                        : Task.FromResult((IResultSummary) new FakeSummary()));
                var ex = await Xunit.Record.ExceptionAsync(async () => await result.ConsumeAsync());

                ex.Should().NotBeNull();
                ex.Should().BeOfType<Exception>();
                await result.ConsumeAsync();
            }

            [Fact]
            public async void ShouldReturnExistingSummaryWhenSummaryHasBeenRetrieved()
            {
                int getSummaryCalled = 0;
                var result = ResultCursorCreator.CreateResultCursor(1, 0, () =>
                {
                    getSummaryCalled++;
                    return Task.FromResult((IResultSummary) new FakeSummary());
                });

                // ReSharper disable once NotAccessedVariable
                var summary = await result.ConsumeAsync();
                // ReSharper disable once RedundantAssignment
                summary = await result.ConsumeAsync();
                getSummaryCalled.Should().Be(1);
            }
        }

        public class PeekAsyncMethod
        {
            [Fact]
            public async void ShouldReturnNextRecordWithoutMovingCurrentRecord()
            {
                var result = ResultCursorCreator.CreateResultCursor(1);
                var record = await result.PeekAsync();
                record.Should().NotBeNull();

                result.Current.Should().BeNull();
            }

            [Fact]
            public async void ShouldReturnNullJustBeforeAtEnd()
            {
                var result = ResultCursorCreator.CreateResultCursor(1);
                var read = await result.FetchAsync();
                read.Should().BeTrue();
                var record = await result.PeekAsync();
                record.Should().BeNull();
            }

            [Fact]
            public async void ShouldReturnNullIfAtEnd()
            {
                var result = ResultCursorCreator.CreateResultCursor(1);
                var read = await result.FetchAsync();
                read.Should().BeTrue();
                read = await result.FetchAsync();
                read.Should().BeFalse();
                var record = await result.PeekAsync();
                record.Should().BeNull();
            }

            [Fact]
            public async void ShouldReturnSameRecordIfPeekedTwice()
            {
                var result = ResultCursorCreator.CreateResultCursor(1);
                var peeked1 = await result.PeekAsync();
                peeked1.Should().NotBeNull();
                var peeked2 = await result.PeekAsync();
                peeked2.Should().NotBeNull();
                peeked2.Should().Be(peeked1);
            }
        }

        public class FetchAsyncMethod
        {
            [Fact]
            public async void FetchAsyncAndCurrentWillReturnPeekedAfterPeek()
            {
                var result = ResultCursorCreator.CreateResultCursor(1);
                var peeked = await result.PeekAsync();
                peeked.Should().NotBeNull();
                var read = await result.FetchAsync();
                read.Should().BeTrue();
                var record = result.Current;
                record.Should().NotBeNull();
                record.Should().Be(peeked);
            }
        }

        public class CurrentProperty
        {
            [Fact]
            public void ShouldThrowExceptionIfFetchOrPeekNotCalled()
            {
                var result = ResultCursorCreator.CreateResultCursor(1);

                var ex = Xunit.Record.Exception(() => result.Current);

                ex.Should().NotBeNull();
                ex.Should().BeOfType<InvalidOperationException>();
            }

            [Fact]
            public async void ShouldNotThrowExceptionWhenCursorIsEmptyAndFetched()
            {
                var result = ResultCursorCreator.CreateResultCursor(1, 0);

                var hasNext = await result.FetchAsync();
                hasNext.Should().BeFalse();

                var next = result.Current;
                next.Should().BeNull();
            }

            [Fact]
            public async void ShouldNotThrowExceptionWhenCursorIsEmptyAndPeeked()
            {
                var result = ResultCursorCreator.CreateResultCursor(1, 0);

                var peeked = await result.PeekAsync();
                peeked.Should().BeNull();

                var next = result.Current;
                next.Should().BeNull();
            }
        }

        public class CancelMethod
        {
            [Fact]
            public void ShouldCancelTokenWhenCancel()
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var result =
                    ResultCursorCreator.CreateResultCursor(1, 0, cancellationTokenSource: cancellationTokenSource);

                cancellationTokenSource.IsCancellationRequested.Should().BeFalse();

                result.Cancel();

                cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
            }
        }

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
            public TimeSpan ResultAvailableAfter { get; }
            public TimeSpan ResultConsumedAfter { get; }
            public IServerInfo Server { get; }
            public IDatabaseInfo Database { get; }
        }

        private static Task<IRecord> NextRecordFromEnum(IEnumerator<IRecord> resultEnum)
        {
            if (resultEnum.MoveNext())
            {
                return Task.FromResult(resultEnum.Current);
            }
            else
            {
                return Task.FromResult((IRecord) null);
            }
        }

        internal static class ResultCursorCreator
        {
            public static ResultCursor CreateResultCursor(int keySize, int recordSize = 1,
                Func<Task<IResultSummary>> getSummaryFunc = null,
                CancellationTokenSource cancellationTokenSource = null)
            {
                var keys = RecordCreator.CreateKeys(keySize);
                var records = RecordCreator.CreateRecords(recordSize, keys);
                var recordsEnum = records.GetEnumerator();

                var stream = new Mock<IResultStream>();
                if (getSummaryFunc == null)
                {
                    getSummaryFunc = () => Task.FromResult((IResultSummary) new FakeSummary());
                }
                stream.Setup(x => x.GetKeysAsync()).Returns(() => Task.FromResult(keys.ToArray()));
                stream.Setup(x => x.NextRecordAsync()).Returns(() => NextRecordFromEnum(recordsEnum));
                stream.Setup(x => x.ConsumeAsync()).Returns(getSummaryFunc);
                stream.Setup(x => x.Cancel()).Callback(() => cancellationTokenSource?.Cancel());

                return new ResultCursor(stream.Object);
            }
        }
    }
}