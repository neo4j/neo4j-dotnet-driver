// Copyright (c) 2002-2019 "Neo4j,"
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
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Neo4j.Driver.Internal.MessageHandling;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class SessionTests
    {
        internal static Session NewSession(IConnection connection, IDriverLogger logger = null,
            IRetryLogic retryLogic = null, AccessMode mode = AccessMode.Write, string bookmark = null)
        {
            return new Session(new TestConnectionProvider(connection), logger, new SyncExecutor(), retryLogic, mode,
                Bookmark.From(bookmark));
        }

        internal static Session NewSession(IBoltProtocol protocol, IDriverLogger logger = null,
            IRetryLogic retryLogic = null, AccessMode mode = AccessMode.Write, string bookmark = null)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.Setup(x => x.IsOpen).Returns(true);
            mockConn.Setup(x => x.BoltProtocol).Returns(protocol);
            return new Session(new TestConnectionProvider(mockConn.Object), logger, new SyncExecutor(), retryLogic,
                mode, Bookmark.From(bookmark));
        }

        internal static Mock<IConnection> NewMockedConnection(IBoltProtocol boltProtocol = null)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.Setup(x => x.IsOpen).Returns(true);
            if (boltProtocol == null)
            {
                var protocol = new Mock<IBoltProtocol>();
                protocol.Setup(x => x.LoginAsync(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<IAuthToken>()))
                    .Returns(TaskHelper.GetCompletedTask());
                protocol.Setup(x => x.RunInAutoCommitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Statement>(), true,
                        It.IsAny<IBookmarkTracker>(),
                        It.IsAny<IResultResourceHandler>(), It.IsAny<Bookmark>(), It.IsAny<TransactionConfig>()))
                    .ReturnsAsync(new Mock<IStatementResultCursor>().Object);
                protocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Bookmark>(),
                            It.IsAny<TransactionConfig>()))
                    .Returns(TaskHelper.GetCompletedTask());
                protocol.Setup(x =>
                        x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Statement>(), true))
                    .ReturnsAsync(new Mock<IStatementResultCursor>().Object);
                protocol.Setup(x => x.CommitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<IBookmarkTracker>()))
                    .Returns(TaskHelper.GetCompletedTask());
                protocol.Setup(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()))
                    .Returns(TaskHelper.GetCompletedTask());
                protocol.Setup(x => x.ResetAsync(It.IsAny<IConnection>()))
                    .Returns(TaskHelper.GetCompletedTask());
                protocol.Setup(x => x.LogoutAsync(It.IsAny<IConnection>()))
                    .Returns(TaskHelper.GetCompletedTask());

                boltProtocol = protocol.Object;
            }

            mockConn.Setup(x => x.BoltProtocol).Returns(boltProtocol);
            return mockConn;
        }

        internal static string FakeABookmark(int num)
        {
            return $"{Bookmark.BookmarkPrefix}{num}";
        }

        public class RunMethod
        {
            [Fact]
            public void ShouldDelegateToProtocolRunAutoCommitTx()
            {
                var mockProtocol = new Mock<IBoltProtocol>();
                mockProtocol.Setup(x => x.RunInAutoCommitTransactionAsync(It.IsAny<IConnection>(),
                        It.IsAny<Statement>(), true, It.IsAny<IBookmarkTracker>(), It.IsAny<IResultResourceHandler>(),
                        It.IsAny<Bookmark>(), It.IsAny<TransactionConfig>()))
                    .ReturnsAsync(new Mock<IStatementResultCursor>().Object);

                var session = NewSession(mockProtocol.Object);
                session.Run("lalalal");

                mockProtocol.Verify(
                    x => x.RunInAutoCommitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Statement>(), true, session,
                        session,
                        It.IsAny<Bookmark>(), It.IsAny<TransactionConfig>()), Times.Once);
            }
        }

        public class RunAsyncMethod
        {
            [Fact]
            public async Task ShouldDelegateToProtocolRunAutoCommitTxAsync()
            {
                var mockProtocol = new Mock<IBoltProtocol>();
                var session = NewSession(mockProtocol.Object);
                await session.RunAsync("lalalal");

                mockProtocol.Verify(
                    x => x.RunInAutoCommitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Statement>(), true, session,
                        session, It.IsAny<Bookmark>(), It.IsAny<TransactionConfig>()), Times.Once);
            }
        }

        public class BeginTransactionMethod
        {
            [Fact]
            public void NullDefaultBookmark()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = NewSession(mockConn.Object);
                session.LastBookmark.Should().Be(null);
            }

            [Fact]
            public void ShouldIgnoreNullBookmark()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object, bookmark: FakeABookmark(123));
                session.LastBookmark.Should().EndWith("123");
                session.BeginTransaction((string) null);
                session.LastBookmark.Should().EndWith("123");
            }

            [Fact]
            public void ShouldSetNewBookmark()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object, bookmark: FakeABookmark(123));
                session.LastBookmark.Should().EndWith("123");
                // begin tx will directly override the bookmark that was originally set before
                session.BeginTransaction(FakeABookmark(12));
                session.LastBookmark.Should().EndWith("12");
            }

            [Fact]
            public void ShouldNotAllowNewTxWhileOneIsRunning()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                session.BeginTransaction();
                var error = Record.Exception(() => session.BeginTransaction());
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldBeAbleToOpenTxAfterPreviousIsClosed()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                var tx = session.BeginTransaction();
                tx.Dispose();
                tx = session.BeginTransaction();
            }

            [Fact]
            public void ShouldNotBeAbleToUseSessionWhileOngoingTransaction()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                var tx = session.BeginTransaction();

                var error = Record.Exception(() => session.Run("lalal"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldBeAbleToUseSessionAgainWhenTransactionIsClosed()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                var tx = session.BeginTransaction();
                tx.Dispose();

                session.Run("lalal");
            }

            [Fact]
            public void ShouldClosePreviousRunConnectionWhenRunMoreStatements()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                session.Run("lalal");

                session.Run("bibib");
                mockConn.Verify(c => c.CloseAsync(), Times.Once);
            }

            [Fact]
            public void ShouldClosePreviousRunConnectionWhenRunMoreTransactions()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                session.Run("lala");

                session.BeginTransaction();
                mockConn.Verify(c => c.CloseAsync(), Times.Once);
            }

            [Fact]
            public void ShouldDisposeConnectionOnRunIfBeginTxFailed()
            {
                // Given
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(mockProtocol.Object);
                mockProtocol.Setup(x =>
                        x.RunInAutoCommitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Statement>(), true,
                            It.IsAny<IBookmarkTracker>(),
                            It.IsAny<IResultResourceHandler>(), It.IsAny<Bookmark>(),
                            It.IsAny<TransactionConfig>()))
                    .ReturnsAsync(new Mock<IStatementResultCursor>().Object);
                mockProtocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Bookmark>(),
                            It.IsAny<TransactionConfig>()))
                    .ThrowsAsync(new IOException("Triggered an error when beginTx"));
                var session = NewSession(mockConn.Object);
                Record.Exception(() => session.BeginTransaction()).Should().BeOfType<IOException>();

                // When
                session.Run("lala");

                // Then
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public void ShouldDisposeConnectionOnNewBeginTxIfBeginTxFailed()
            {
                // Given
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(mockProtocol.Object);
                var calls = 0;
                mockProtocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Bookmark>(),
                            It.IsAny<TransactionConfig>()))
                    .Returns(TaskHelper.GetCompletedTask())
                    .Callback(() =>
                    {
                        // only throw exception on the first beginTx call
                        calls++;
                        if (calls == 1)
                        {
                            throw new IOException("Triggered an error when beginTx");
                        }
                    });

                var session = NewSession(mockConn.Object);
                Record.Exception(() => session.BeginTransaction()).Should().BeOfType<IOException>();

                // When
                session.BeginTransaction();

                // Then
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }
        }

        public class BeginTransactionAsyncMethod
        {
            [Fact]
            public async void ShouldNotAllowNewTxWhileOneIsRunning()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                await session.BeginTransactionAsync();
                var error = await Record.ExceptionAsync(() => session.BeginTransactionAsync());
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public async void ShouldBeAbleToOpenTxAfterPreviousIsClosed()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                var tx = await session.BeginTransactionAsync();
                await tx.RollbackAsync();
                tx = await session.BeginTransactionAsync();
            }

            [Fact]
            public async void ShouldNotBeAbleToUseSessionWhileOngoingTransaction()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                var tx = await session.BeginTransactionAsync();

                var error = await Record.ExceptionAsync(() => session.RunAsync("lalal"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public async void ShouldBeAbleToUseSessionAgainWhenTransactionIsClosed()
            {
                var mockConn = MockedConnectionWithSuccessResponse();

                var session = NewSession(mockConn.Object);
                var tx = await session.BeginTransactionAsync();
                await tx.RollbackAsync();

                await session.RunAsync("lalal");
            }

            [Fact]
            public async void ShouldClosePreviousRunConnectionWhenRunMoreStatements()
            {
                var mockConn = MockedConnectionWithSuccessResponse();
                var session = NewSession(mockConn.Object);
                await session.RunAsync("lalal");

                await session.RunAsync("bibib");
                mockConn.Verify(c => c.CloseAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldClosePreviousRunConnectionWhenRunMoreTransactions()
            {
                var mockConn = MockedConnectionWithSuccessResponse();
                mockConn.Setup(x => x.IsOpen).Returns(false);
                var session = NewSession(mockConn.Object);
                await session.RunAsync("lala");

                await session.BeginTransactionAsync();
                mockConn.Verify(c => c.CloseAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldDisposeConnectionOnRunIfBeginTxFailed()
            {
                // Given
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = MockedConnectionWithSuccessResponse(mockProtocol.Object);

                mockProtocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Bookmark>(),
                            It.IsAny<TransactionConfig>()))
                    .Throws(new IOException("Triggered an error when beginTx"));
                var session = NewSession(mockConn.Object);
                var exc = await Record.ExceptionAsync(() => session.BeginTransactionAsync());
                exc.Should().BeOfType<IOException>();

                // When
                await session.RunAsync("lala");

                // Then
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldDisposeConnectionOnNewBeginTxIfBeginTxFailed()
            {
                // Given
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(mockProtocol.Object);
                var calls = 0;
                mockProtocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Bookmark>(),
                            It.IsAny<TransactionConfig>()))
                    .Returns(TaskHelper.GetCompletedTask()).Callback(() =>
                    {
                        // only throw exception on the first beginTx call
                        calls++;
                        if (calls == 1)
                        {
                            throw new IOException("Triggered an error when beginTx");
                        }
                    });

                var session = NewSession(mockConn.Object);
                var exc = await Record.ExceptionAsync(() => session.BeginTransactionAsync());
                exc.Should().BeOfType<IOException>();

                // When
                await session.BeginTransactionAsync();

                // Then
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldDisposeConnectionIfBeginTxFailed()
            {
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(mockProtocol.Object);
                mockProtocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Bookmark>(),
                            It.IsAny<TransactionConfig>()))
                    .Returns(TaskHelper.GetFailedTask(new IOException("Triggered an error when beginTx")));
                var session = NewSession(mockConn.Object);
                Record.Exception(() => session.BeginTransaction()).Should().BeOfType<IOException>();
                session.Dispose();

                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public void ShouldDisposeTxOnDispose()
            {
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(mockProtocol.Object);
                var session = NewSession(mockConn.Object);
                session.BeginTransaction();
                session.Dispose();

                mockProtocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Once);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public void ShouldDisposeConnectionOnDispose()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                session.Run("lalal");
                session.Dispose();

                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public void ShouldAllowDisposeMultipleTimes()
            {
                // Given
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                session.Run("lalal");

                // When
                session.Dispose();
                session.Dispose();

                // Then
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }
        }

        public class DisposeMethodOnAsync
        {
            [Fact]
            public async void ShouldDisposeConnectionIfBeginTxFailed()
            {
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(mockProtocol.Object);
                mockProtocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Bookmark>(),
                            It.IsAny<TransactionConfig>()))
                    .Throws(new IOException("Triggered an error when beginTx"));
                var session = NewSession(mockConn.Object);
                var error = await Record.ExceptionAsync(() => session.BeginTransactionAsync());
                error.Should().BeOfType<IOException>();
                await session.CloseAsync();

                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldDisposeTxOnDispose()
            {
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(mockProtocol.Object);
                var session = NewSession(mockConn.Object);
                var tx = await session.BeginTransactionAsync();
                await session.CloseAsync();

                mockProtocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Once);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldDisposeConnectionOnDispose()
            {
                var mockConn = NewMockedConnection();
                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (m1, h1, m2, h2) => { h1.OnSuccess(new Dictionary<string, object>()); });
                var session = NewSession(mockConn.Object);
                await session.RunAsync("lalal");
                await session.CloseAsync();

                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldAllowDisposeAfterCloseAsync()
            {
                // Given
                var mockConn = NewMockedConnection();
                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (m1, h1, m2, h2) => { h1.OnSuccess(new Dictionary<string, object>()); });
                var session = NewSession(mockConn.Object);
                await session.RunAsync("lalal");

                // When
                await session.CloseAsync();
                session.Dispose();

                // Then
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldAllowCloseAsyncAfterDispose()
            {
                // Given
                var mockConn = NewMockedConnection();
                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (m1, h1, m2, h2) => { h1.OnSuccess(new Dictionary<string, object>()); });
                var session = NewSession(mockConn.Object);
                await session.RunAsync("lalal");

                // When
                session.Dispose();
                await session.CloseAsync();

                // Then
                mockConn.Verify(x => x.SyncAsync(), Times.Once);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }
        }

        internal static Mock<IConnection> MockedConnectionWithSuccessResponse(IBoltProtocol protocol = null)
        {
            var mockConn = new Mock<IConnection>();
            // Whenever you enqueue any message, you immediately receives a response
            mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                    It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                .Returns(TaskHelper.GetCompletedTask())
                .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                    (m1, h1, m2, h2) =>
                    {
                         h1.OnSuccess(new Dictionary<string, object>());
                        if (m2 != null)
                        {
                            h2.OnSuccess(new Dictionary<string, object>());
                        }
                    });

            if (protocol == null)
            {
                var mockProtocol = new Mock<IBoltProtocol>();
                protocol = mockProtocol.Object;
            }

            mockConn.Setup(x => x.BoltProtocol).Returns(protocol);
            mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Write);
            return mockConn;
        }

        private class TestConnectionProvider : IConnectionProvider
        {
            private IConnection Connection { get; set; }
            private AccessMode Mode { get; set; }

            public TestConnectionProvider(IConnection connection)
            {
                Connection = connection;
            }

            public void Dispose()
            {
                // do nothing
            }

            public IConnection Acquire(AccessMode mode)
            {
                Mode = mode;
                return Connection;
            }

            public Task<IConnection> AcquireAsync(AccessMode mode)
            {
                return Task.FromResult(Connection);
            }

            public void Close()
            {
            }

            public Task CloseAsync()
            {
                return TaskHelper.GetCompletedTask();
            }
        }
    }
}