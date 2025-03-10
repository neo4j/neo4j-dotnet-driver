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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests.Result;

public class ResultCursorBuilderTests
{
    [Fact]
    public void ShouldStartInRunRequestedStateRx()
    {
        var builder =
            new ResultCursorBuilder(
                CreateSummaryBuilder(),
                CreateTaskQueue(),
                null,
                null,
                null,
                1000,
                true,
                new Mock<IInternalAsyncTransaction>().Object);

        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunRequested);
    }

    [Fact]
    public void ShouldStartInRunAndRecordsRequestedState()
    {
        var builder =
            new ResultCursorBuilder(
                CreateSummaryBuilder(),
                CreateTaskQueue(),
                null,
                null,
                null,
                1000,
                false,
                new Mock<IInternalAsyncTransaction>().Object);

        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunAndRecordsRequested);
    }

    [Fact]
    public void ShouldTransitionToRunCompletedWhenRunCompletedRx()
    {
        var builder =
            new ResultCursorBuilder(
                CreateSummaryBuilder(),
                CreateTaskQueue(),
                null,
                null,
                null,
                1000,
                true,
                new Mock<IInternalAsyncTransaction>().Object);

        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunRequested);

        builder.RunCompleted(0, new[] { "a", "b", "c" }, null);
        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunCompleted);
    }

    [Fact]
    public void ShouldNotTransitionToRunCompletedWhenRunCompleted()
    {
        var builder =
            new ResultCursorBuilder(
                CreateSummaryBuilder(),
                CreateTaskQueue(),
                null,
                null,
                null,
                1000,
                false,
                new Mock<IInternalAsyncTransaction>().Object);

        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunAndRecordsRequested);

        builder.RunCompleted(0, new[] { "a", "b", "c" }, null);
        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunAndRecordsRequested);
    }

    [Fact]
    public void ShouldTransitionToRecordsStreamingStreamingWhenRecordIsPushedRx()
    {
        var builder =
            new ResultCursorBuilder(
                CreateSummaryBuilder(),
                CreateTaskQueue(),
                null,
                null,
                null,
                1000,
                true,
                new Mock<IInternalAsyncTransaction>().Object);

        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunRequested);

        builder.RunCompleted(0, new[] { "a", "b", "c" }, null);
        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunCompleted);

        builder.PushRecord(new object[] { 1, 2, 3 });
        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RecordsStreaming);
    }

    [Fact]
    public void ShouldTransitionToRecordsStreamingStreamingWhenRecordIsPushed()
    {
        var builder =
            new ResultCursorBuilder(
                CreateSummaryBuilder(),
                CreateTaskQueue(),
                null,
                null,
                null,
                1000,
                false,
                new Mock<IInternalAsyncTransaction>().Object);

        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunAndRecordsRequested);

        builder.RunCompleted(0, new[] { "a", "b", "c" }, null);
        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunAndRecordsRequested);

        builder.PushRecord(new object[] { 1, 2, 3 });
        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RecordsStreaming);
    }

    [Fact]
    public void ShouldTransitionToRunCompletedWhenPullCompletedWithHasMore()
    {
        var builder =
            new ResultCursorBuilder(
                CreateSummaryBuilder(),
                CreateTaskQueue(),
                null,
                null,
                null,
                1000,
                false,
                new Mock<IInternalAsyncTransaction>().Object)
            {
                CurrentState = ResultCursorBuilder.State.RecordsStreaming
            };

        builder.PullCompleted(true, null);
        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunCompleted);
    }

    [Fact]
    public void ShouldTransitionToCompletedWhenPullCompleted()
    {
        var builder =
            new ResultCursorBuilder(
                CreateSummaryBuilder(),
                CreateTaskQueue(),
                null,
                null,
                null,
                1000,
                false,
                new Mock<IInternalAsyncTransaction>().Object)
            {
                CurrentState = ResultCursorBuilder.State.RecordsStreaming
            };

        builder.PullCompleted(false, null);
        builder.CurrentState.Should().Be(ResultCursorBuilder.State.Completed);
    }

    [Fact]
    public async Task ShouldInvokeResourceHandlerWhenCompleted()
    {
        var actions = new Queue<Action>();
        var resourceHandler = new Mock<IResultResourceHandler>();
        var builder =
            new ResultCursorBuilder(
                CreateSummaryBuilder(),
                CreateTaskQueue(actions),
                null,
                null,
                resourceHandler.Object,
                1000,
                false,
                new Mock<IInternalAsyncTransaction>().Object);

        actions.Enqueue(() => builder.RunCompleted(0, new[] { "a" }, null));
        actions.Enqueue(() => builder.PushRecord(new object[] { 1 }));
        actions.Enqueue(() => builder.PushRecord(new object[] { 2 }));
        actions.Enqueue(() => builder.PushRecord(new object[] { 3 }));
        actions.Enqueue(() => builder.PullCompleted(false, null));

        var cursor = builder.CreateCursor();

        var hasNext = await cursor.FetchAsync();
        hasNext.Should().BeTrue();
        resourceHandler.Verify(x => x.OnResultConsumedAsync(), Times.Never);

        hasNext = await cursor.FetchAsync();
        hasNext.Should().BeTrue();
        resourceHandler.Verify(x => x.OnResultConsumedAsync(), Times.Never);

        hasNext = await cursor.FetchAsync();
        hasNext.Should().BeTrue();
        resourceHandler.Verify(x => x.OnResultConsumedAsync(), Times.Never);

        hasNext = await cursor.FetchAsync();
        hasNext.Should().BeFalse();
        resourceHandler.Verify(x => x.OnResultConsumedAsync(), Times.Once);
    }

    [Fact]
    public async Task ShouldPauseAndResumeStreamingWithWatermarks()
    {
        var actions = new Queue<Action>();
        var resourceHandler = new Mock<IResultResourceHandler>();
        var builder =
            new ResultCursorBuilder(
                CreateSummaryBuilder(),
                CreateTaskQueue(),
                CreateMoreTaskQueue(actions),
                null,
                resourceHandler.Object,
                2,
                false,
                new Mock<IInternalAsyncTransaction>().Object);

        var counter = 0;
        builder.RunCompleted(0, new[] { "a" }, null);
        builder.PullCompleted(true, null);
        builder.CurrentState.Should().Be(ResultCursorBuilder.State.RunCompleted);
        actions.Enqueue(
            () =>
            {
                builder.PushRecord(new object[] { 1 });
                counter++;
                builder.PushRecord(new object[] { 2 });
                counter++;
                builder.PullCompleted(true, null);
            });

        actions.Enqueue(
            () =>
            {
                builder.PushRecord(new object[] { 3 });
                counter++;
                builder.PullCompleted(false, null);
            });

        var cursor = builder.CreateCursor();

        var hasNext = await cursor.FetchAsync();
        hasNext.Should().BeTrue();
        resourceHandler.Verify(x => x.OnResultConsumedAsync(), Times.Never);
        counter.Should().Be(2);

        hasNext = await cursor.FetchAsync();
        hasNext.Should().BeTrue();
        resourceHandler.Verify(x => x.OnResultConsumedAsync(), Times.Once);
        counter.Should().Be(3);

        hasNext = await cursor.FetchAsync();
        hasNext.Should().BeTrue();
        counter.Should().Be(3);

        hasNext = await cursor.FetchAsync();
        hasNext.Should().BeFalse();
        counter.Should().Be(3);
    }

    private static SummaryBuilder CreateSummaryBuilder()
    {
        return new SummaryBuilder(new Query("Fake"), Mock.Of<IServerInfo>());
    }

    private static Func<Task> CreateTaskQueue(Queue<Action> actions = null)
    {
        if (actions == null)
        {
            actions = new Queue<Action>();
        }

        return () =>
        {
            if (actions.TryDequeue(out var action))
            {
                action();
            }

            return Task.CompletedTask;
        };
    }

    private static Func<IResultStreamBuilder, long, long, Task> CreateMoreTaskQueue(Queue<Action> actions)
    {
        return (_, _, _) =>
        {
            if (actions.TryDequeue(out var action))
            {
                action();
            }

            return Task.CompletedTask;
        };
    }

    public class Reactive
    {
        private int cancelCallCount;
        private int moreCallCount;

        [Fact]
        public async Task ShouldCallMoreOnceAndReturnRecords()
        {
            var actions = new Queue<Action>();
            var builder =
                new ResultCursorBuilder(
                    CreateSummaryBuilder(),
                    CreateTaskQueue(actions),
                    MoreFunction(),
                    CancelFunction(),
                    null,
                    1000,
                    true,
                    new Mock<IInternalAsyncTransaction>().Object);

            actions.Enqueue(() => builder.RunCompleted(0, new[] { "a" }, null));
            actions.Enqueue(() => builder.PushRecord(new object[] { 1 }));
            actions.Enqueue(() => builder.PushRecord(new object[] { 2 }));
            actions.Enqueue(() => builder.PushRecord(new object[] { 3 }));
            actions.Enqueue(() => builder.PullCompleted(false, null));

            var list = await builder.CreateCursor().ToListAsync(r => r[0].As<int>());

            list.Should().BeEquivalentTo([1, 2, 3]);
            moreCallCount.Should().Be(1);
            cancelCallCount.Should().Be(0);
        }

        [Fact]
        public async Task ShouldCallMoreTwiceAndReturnRecords()
        {
            var actions = new Queue<Action>();
            var builder =
                new ResultCursorBuilder(
                    CreateSummaryBuilder(),
                    CreateTaskQueue(actions),
                    MoreFunction(),
                    CancelFunction(),
                    null,
                    1000,
                    true,
                    new Mock<IInternalAsyncTransaction>().Object);

            actions.Enqueue(() => builder.RunCompleted(0, new[] { "a" }, null));
            actions.Enqueue(() => builder.PushRecord(new object[] { 1 }));
            actions.Enqueue(() => builder.PullCompleted(true, null));
            actions.Enqueue(() => builder.PushRecord(new object[] { 2 }));
            actions.Enqueue(() => builder.PushRecord(new object[] { 3 }));
            actions.Enqueue(() => builder.PullCompleted(false, null));

            var list = await builder.CreateCursor().ToListAsync(r => r[0].As<int>());

            list.Should().BeEquivalentTo([1, 2, 3]);
            moreCallCount.Should().Be(2);
            cancelCallCount.Should().Be(0);
        }

        [Fact]
        public async Task ShouldCallMoreThreeTimesAndReturnRecords()
        {
            var actions = new Queue<Action>();
            var builder =
                new ResultCursorBuilder(
                    CreateSummaryBuilder(),
                    CreateTaskQueue(actions),
                    MoreFunction(),
                    CancelFunction(),
                    null,
                    1000,
                    true,
                    new Mock<IInternalAsyncTransaction>().Object);

            actions.Enqueue(() => builder.RunCompleted(0, new[] { "a" }, null));
            actions.Enqueue(() => builder.PushRecord(new object[] { 1 }));
            actions.Enqueue(() => builder.PullCompleted(true, null));
            actions.Enqueue(() => builder.PushRecord(new object[] { 2 }));
            actions.Enqueue(() => builder.PullCompleted(true, null));
            actions.Enqueue(() => builder.PushRecord(new object[] { 3 }));
            actions.Enqueue(() => builder.PullCompleted(false, null));

            var list = await builder.CreateCursor().ToListAsync(r => r[0].As<int>());

            list.Should().BeEquivalentTo([1, 2, 3]);
            moreCallCount.Should().Be(3);
            cancelCallCount.Should().Be(0);
        }

        [Fact]
        public async Task ShouldCallCancelAndReturnNoRecords()
        {
            var actions = new Queue<Action>();
            var builder =
                new ResultCursorBuilder(
                    CreateSummaryBuilder(),
                    CreateTaskQueue(actions),
                    MoreFunction(),
                    CancelFunction(),
                    null,
                    1000,
                    true,
                    new Mock<IInternalAsyncTransaction>().Object);

            actions.Enqueue(() => builder.RunCompleted(0, new[] { "a" }, null));
            actions.Enqueue(() => builder.PullCompleted(false, null));

            var cursor = builder.CreateCursor();

            var keys = await cursor.KeysAsync();
            keys.Should().BeEquivalentTo("a");

            cursor.Cancel();

            var list = await cursor.ToListAsync(r => r[0].As<int>());

            list.Should().BeEmpty();
            moreCallCount.Should().Be(0);
            cancelCallCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldReturnFirstBatchOfRecordsAndCancel()
        {
            var actions = new Queue<Action>();
            var builder =
                new ResultCursorBuilder(
                    CreateSummaryBuilder(),
                    CreateTaskQueue(actions),
                    MoreFunction(),
                    CancelFunction(),
                    null,
                    1000,
                    true,
                    new Mock<IInternalAsyncTransaction>().Object);

            actions.Enqueue(() => builder.RunCompleted(0, new[] { "a" }, null));
            actions.Enqueue(() => builder.PushRecord(new object[] { 1 }));
            actions.Enqueue(() => builder.PushRecord(new object[] { 2 }));
            actions.Enqueue(() => builder.PullCompleted(true, null));
            actions.Enqueue(() => builder.PullCompleted(false, null));

            var cursor = builder.CreateCursor();

            var keys = await cursor.KeysAsync();
            keys.Should().BeEquivalentTo("a");

            var hasRecord1 = await cursor.FetchAsync();
            var record1 = cursor.Current;
            hasRecord1.Should().BeTrue();
            record1[0].Should().Be(1);

            var hasRecord2 = await cursor.FetchAsync();
            var record2 = cursor.Current;
            hasRecord2.Should().BeTrue();
            record2[0].Should().Be(2);

            cursor.Cancel();

            var list = await cursor.ToListAsync(r => r[0].As<int>());

            list.Should().BeEmpty();
            moreCallCount.Should().Be(1);
            cancelCallCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldThrowIfTranasactionTerminatedOnFetch()
        {
            var expected = new ClientException("Neo.Broken.Db", "it's broken!") as Exception;
            var mockTx = new Mock<IInternalAsyncTransaction>();
            mockTx.Setup(x => x.IsErrored(out expected)).Returns(true).Verifiable();

            var builder =
                new ResultCursorBuilder(
                    CreateSummaryBuilder(),
                    () => Task.CompletedTask,
                    MoreFunction(),
                    CancelFunction(),
                    null,
                    1000,
                    true,
                    mockTx.Object);

            var cursor = builder.CreateCursor();
            var exception = await Record.ExceptionAsync(() => cursor.FetchAsync());

            exception.Should().BeOfType<TransactionTerminatedException>();
        }

        [Fact]
        public async Task ShouldThrowIfTranasactionTerminatedOnConsume()
        {
            var expected = new ClientException("Neo.Broken.Db", "it's broken!") as Exception;
            var mockTx = new Mock<IInternalAsyncTransaction>();
            mockTx.Setup(x => x.IsErrored(out expected)).Returns(true).Verifiable();

            var builder =
                new ResultCursorBuilder(
                    CreateSummaryBuilder(),
                    () => Task.CompletedTask,
                    MoreFunction(),
                    CancelFunction(),
                    null,
                    1000,
                    true,
                    mockTx.Object);

            var cursor = builder.CreateCursor();
            var exception = await Record.ExceptionAsync(() => cursor.ConsumeAsync());

            exception.Should().BeOfType<TransactionTerminatedException>();
        }

        private Func<IResultStreamBuilder, long, long, Task> MoreFunction()
        {
            return (_, _, _) =>
            {
                Interlocked.Increment(ref moreCallCount);
                return Task.CompletedTask;
            };
        }

        private Func<IResultStreamBuilder, long, Task> CancelFunction()
        {
            return (_, _) =>
            {
                Interlocked.Increment(ref cancelCallCount);
                return Task.CompletedTask;
            };
        }
    }
}
