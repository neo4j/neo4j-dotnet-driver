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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Telemetry;
using Xunit;
using static Neo4j.Driver.Tests.AsyncSessionTests;
using static Xunit.Record;

namespace Neo4j.Driver.Tests;

public class TransactionTests
{
    public class Constructor
    {
        [Fact]
        public async Task ShouldSaveBookmark()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection(mockProtocol);
            var bookmarks = Bookmarks.From(FakeABookmark(123));
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                null,
                null,
                bookmarks);

            await tx.BeginTransactionAsync(
                null,
                new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true));

            mockProtocol.Verify(
                x => x.BeginTransactionAsync(
                    It.IsAny<IConnection>(),
                    It.IsAny<BeginTransactionParams>()),
                Times.Once);
        }
    }

    public class BeginTransactionAsyncMethod
    {
        [Fact]
        public async Task ShouldDelegateToProtocolBeginTxMethod()
        {
            var protocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection(protocol);
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            await tx.BeginTransactionAsync(
                TransactionConfig.Default,
                new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true));

            protocol.Verify(
                x => x.BeginTransactionAsync(
                    It.IsAny<IConnection>(),
                    It.IsAny<BeginTransactionParams>()),
                Times.Once);
        }
    }

    public class RunAsyncMethod
    {
        [Fact]
        public async void ShouldDelegateToBoltProtocol()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection(mockProtocol);
            var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>(), NullLogger.Instance);

            var query = new Query("lala");
            await tx.RunAsync(query);

            mockProtocol.Verify(
                x =>
                    x.RunInExplicitTransactionAsync(
                        It.IsAny<IConnection>(),
                        query,
                        false,
                        It.IsAny<long>(),
                        It.IsAny<IInternalAsyncTransaction>()));
        }

        [Fact]
        public async void ShouldThrowExceptionIfPreviousTxFailed()
        {
            var mockConn = new Mock<IConnection>();
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            tx.TransactionError = new Exception();
            await tx.MarkToCloseAsync();

            var error = await ExceptionAsync(() => tx.RunAsync("ttt"));
            error.Should().BeOfType<TransactionTerminatedException>();
        }

        [Fact]
        public async void ShouldThrowExceptionIfFailedToRunAndFetchResult()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection(mockProtocol);
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            var query = new Query("lala");

            mockProtocol.Setup(
                    x =>
                        x.RunInExplicitTransactionAsync(
                            It.IsAny<IConnection>(),
                            query,
                            false,
                            It.IsAny<long>(),
                            It.IsAny<IInternalAsyncTransaction>()))
                .Throws<Neo4jException>();

            var error = await ExceptionAsync(() => tx.RunAsync(query));
            error.Should().BeOfType<Neo4jException>();
        }
    }

    public class CloseAsyncMethod
    {
        [Fact]
        public async void ShouldCommitOnSuccess()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection(mockProtocol);
            var mockHandler = new Mock<ITransactionResourceHandler>();
            var tx = new AsyncTransaction(mockConn.Object, mockHandler.Object, NullLogger.Instance);

            mockConn.Invocations.Clear();
            await tx.CommitAsync();

            mockProtocol.Verify(x => x.CommitTransactionAsync(It.IsAny<IConnection>(), tx));
            mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmarks>(), null), Times.Once);
        }

        [Fact]
        public async void ShouldRollbackOnFailure()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection(mockProtocol);
            var mockHandler = new Mock<ITransactionResourceHandler>();
            var tx = new AsyncTransaction(mockConn.Object, mockHandler.Object, NullLogger.Instance);

            mockConn.Invocations.Clear();
            await tx.RollbackAsync();
            mockProtocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()));
            mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmarks>(), null), Times.Once);
        }

        [Fact]
        public async Task ShouldNotDisposeIfAlreadyClosed()
        {
            var mockConn = NewMockedConnection();
            var mockHandler = new Mock<ITransactionResourceHandler>();
            var tx = new AsyncTransaction(mockConn.Object, mockHandler.Object, NullLogger.Instance);

            mockConn.Invocations.Clear();
            await tx.CommitAsync();
            mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmarks>(), null), Times.Once);
        }
    }

    public class MarkToClosedMethod
    {
        [Fact]
        public async Task ShouldNotEnqueueMoreMessagesAfterMarkToClosed()
        {
            var mockConn = NewMockedConnection();
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            mockConn.Invocations.Clear();

            await tx.MarkToCloseAsync();

            mockConn.Verify(x => x.RollbackTransactionAsync(), Times.Never);
            mockConn.Verify(x => x.SyncAsync(), Times.Never);
        }

        [Fact]
        public async Task ShouldThrowExceptionToRunAfterMarkToClosed()
        {
            var mockConn = NewMockedConnection();
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            mockConn.Invocations.Clear();
            tx.TransactionError = new Exception();
            await tx.MarkToCloseAsync();

            tx.Awaiting(t => t.RunAsync("should not run"))
                .Should()
                .ThrowAsync<ClientException>();

            mockConn.Verify(x => x.RollbackTransactionAsync(), Times.Never);
            mockConn.Verify(x => x.SyncAsync(), Times.Never);
        }

        [Fact]
        public async Task ShouldNotEnqueueMoreMessagesAfterMarkToClosedInCommitAsync()
        {
            var mockConn = NewMockedConnection();
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            mockConn.Invocations.Clear();
            tx.TransactionError = new Exception();
            await tx.MarkToCloseAsync();
            await tx.Awaiting(t => t.CommitAsync())
                .Should()
                .ThrowAsync<ClientException>();

            mockConn.Verify(x => x.CommitTransactionAsync(tx), Times.Never);
            mockConn.Verify(x => x.SyncAsync(), Times.Never);
        }

        [Fact]
        public async Task ShouldNotEnqueueMoreMessagesAfterMarkToClosedInRollbackAsync()
        {
            var mockConn = NewMockedConnection();
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            mockConn.Invocations.Clear();

            await tx.MarkToCloseAsync();
            await tx.Awaiting(t => t.RollbackAsync()).Should().NotThrowAsync();
            mockConn.Verify(x => x.RollbackTransactionAsync(), Times.Never);
            mockConn.Verify(x => x.SyncAsync(), Times.Never);
        }
    }

    public class IsOpenTests
    {
        [Fact]
        public async Task ShouldBeOpenWhenConstructed()
        {
            var mockConn = NewMockedConnection();
            var tx = new AsyncTransaction(mockConn.Object, Mock.Of<ITransactionResourceHandler>(), NullLogger.Instance);

            await tx.BeginTransactionAsync(
                TransactionConfig.Default,
                new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true));

            tx.IsOpen.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldBeOpenWhenRun()
        {
            var mockConn = NewMockedConnection();
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            await tx.BeginTransactionAsync(
                TransactionConfig.Default,
                new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true));

            await tx.RunAsync("RETURN 1");

            tx.IsOpen.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldBeClosedWhenFailed()
        {
            var mockConn = NewMockedConnection();
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            await tx.BeginTransactionAsync(
                TransactionConfig.Default,
                new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true));

            await tx.MarkToCloseAsync();

            tx.IsOpen.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldBeClosedWhenCommitted()
        {
            var mockConn = NewMockedConnection();
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            await tx.BeginTransactionAsync(
                TransactionConfig.Default,
                new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true));

            await tx.CommitAsync();

            tx.IsOpen.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldBeClosedWhenRollBacked()
        {
            var mockConn = NewMockedConnection();
            var tx = new AsyncTransaction(
                mockConn.Object,
                Mock.Of<ITransactionResourceHandler>(),
                NullLogger.Instance);

            await tx.BeginTransactionAsync(
                TransactionConfig.Default,
                new TransactionInfo(QueryApiType.UnmanagedTransaction, false, true));

            await tx.RollbackAsync();

            tx.IsOpen.Should().BeFalse();
        }
    }
}
