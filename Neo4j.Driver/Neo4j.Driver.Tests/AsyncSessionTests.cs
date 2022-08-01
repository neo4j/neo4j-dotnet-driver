// Copyright (c) "Neo4j"
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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.MessageHandling;
using Xunit;
using Record = Xunit.Record;
using Neo4j.Driver.Internal.Routing;

namespace Neo4j.Driver.Tests
{
    public class AsyncSessionTests
    {
        internal static AsyncSession NewSession(IConnection connection, ILogger logger = null)
        {
            return new AsyncSession(new TestConnectionProvider(connection), logger);
        }

        internal static AsyncSession NewSession(IBoltProtocol protocol, bool reactive = false)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.Setup(x => x.IsOpen).Returns(true);
            mockConn.Setup(x => x.BoltProtocol).Returns(protocol);
            return new AsyncSession(new TestConnectionProvider(mockConn.Object), null, reactive: reactive);
        }

        internal static Mock<IConnection> NewMockedConnection(IBoltProtocol boltProtocol = null)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.Setup(x => x.IsOpen).Returns(true);
            if (boltProtocol == null)
            {
                var protocol = new Mock<IBoltProtocol>();
                protocol.Setup(x => x.LoginAsync(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<IAuthToken>()))
                    .Returns(Task.CompletedTask);
                protocol.Setup(x => x.RunInAutoCommitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Query>(),
                        false,
                        It.IsAny<IBookmarksTracker>(), It.IsAny<IResultResourceHandler>(), It.IsAny<string>(),
                        It.IsAny<Bookmarks>(), It.IsAny<TransactionConfig>(), It.IsAny<string>(), It.IsAny<long>()))
                    .ReturnsAsync(new Mock<IResultCursor>().Object);
                protocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<Bookmarks>(),
                            It.IsAny<TransactionConfig>(), null))
                    .Returns(Task.CompletedTask);
                protocol.Setup(x =>
                        x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Query>(), false, It.IsAny<long>()))
                    .ReturnsAsync(new Mock<IResultCursor>().Object);
                protocol.Setup(x => x.CommitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<IBookmarksTracker>()))
                    .Returns(Task.CompletedTask);
                protocol.Setup(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()))
                    .Returns(Task.CompletedTask);
                protocol.Setup(x => x.ResetAsync(It.IsAny<IConnection>()))
                    .Returns(Task.CompletedTask);
                protocol.Setup(x => x.LogoutAsync(It.IsAny<IConnection>()))
                    .Returns(Task.CompletedTask);

                boltProtocol = protocol.Object;
            }

            mockConn.Setup(x => x.BoltProtocol).Returns(boltProtocol);
            return mockConn;
        }

        internal static string FakeABookmark(int num)
        {
            return $"bookmark-{num}";
        }

        public class RunAsyncMethod
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ShouldDelegateToProtocolRunAutoCommitTxAsync(bool reactive)
            {
                var mockProtocol = new Mock<IBoltProtocol>();
                var session = NewSession(mockProtocol.Object, reactive);
                await session.RunAsync("lalalal");

                mockProtocol.Verify(
                    x => x.RunInAutoCommitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Query>(), reactive,
                        session, session, It.IsAny<string>(), It.IsAny<Bookmarks>(), It.IsAny<TransactionConfig>(), It.IsAny<string>(), It.IsAny<long>()),
                    Times.Once);
            }
        }

        public class BeginTransactionAsyncMethod
        {
            [Fact]
            public async void ShouldReturnTransactionConfigAsItIs()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                var tx = await session.BeginTransactionAsync(o =>
                    o.WithMetadata(new Dictionary<string, object> {{"key", "value"}})
                        .WithTimeout(TimeSpan.MaxValue));

                var config = tx.TransactionConfig;
                var item = config.Metadata.Single();
                item.Key.Should().Be("key");
                item.Value.Should().Be("value");

                config.Timeout.Should().Be(TimeSpan.MaxValue);
            }

            [Fact]
            public async void ShouldNotAllowNewTxWhileOneIsRunning()
            {
                var mockConn = NewMockedConnection();
                var session = NewSession(mockConn.Object);
                await session.BeginTransactionAsync();
                var error = await Record.ExceptionAsync(() => session.BeginTransactionAsync());
                error.Should().BeOfType<TransactionNestingException>();
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
                error.Should().BeOfType<TransactionNestingException>();
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
            public async void ShouldClosePreviousRunConnectionWhenRunMoreQueries()
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
            public async void ShouldCloseConnectionOnRunIfBeginTxFailed()
            {
                // Given
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = MockedConnectionWithSuccessResponse(mockProtocol.Object);

                mockProtocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<Bookmarks>(),
                            It.IsAny<TransactionConfig>(), It.IsAny<string>()))
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
            public async void ShouldCloseConnectionOnNewBeginTxIfBeginTxFailed()
            {
                // Given
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(mockProtocol.Object);
                var calls = 0;
                mockProtocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<Bookmarks>(),
                            It.IsAny<TransactionConfig>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask).Callback(() =>
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

        public class CloseAsyncMethod
        {
            [Fact]
            public async void ShouldCloseConnectionIfBeginTxFailed()
            {
                var mockProtocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(mockProtocol.Object);
                mockProtocol.Setup(x =>
                        x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<Bookmarks>(),
                            It.IsAny<TransactionConfig>(), It.IsAny<string>()))
                    .Throws(new IOException("Triggered an error when beginTx"));
                var session = NewSession(mockConn.Object);
                var error = await Record.ExceptionAsync(() => session.BeginTransactionAsync());
                error.Should().BeOfType<IOException>();
                await session.CloseAsync();

                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public async void ShouldCloseTxOnCloseAsync()
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
            public async void ShouldCloseConnectionOnCloseAsync()
            {
                var mockConn = NewMockedConnection();
                mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (m1, h1, m2, h2) => { h1.OnSuccess(new Dictionary<string, object>()); });
                var session = NewSession(mockConn.Object);
                await session.RunAsync("lalal");
                await session.CloseAsync();

                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }
        }

        public class SessionConfig
        {

            [Fact]
            public void ShouldReturnSessionConfigAsItIs()
            {
                var driver = NewDriver();
                {
                    var session = driver.AsyncSession(b =>
                        b.WithDatabase("molly").WithDefaultAccessMode(AccessMode.Read).WithFetchSize(17)
                            .WithBookmarks(Bookmarks.From("bookmark1")));
                    var config = session.SessionConfig;

                    config.Database.Should().Be("molly");
                    config.FetchSize.Should().Be(17L);
                    config.DefaultAccessMode.Should().Be(AccessMode.Read);

                    var bookmarks = config.Bookmarks.ToList();
                    bookmarks.Count.Should().Be(1);
                    bookmarks[0].Values.Length.Should().Be(1);
                    bookmarks[0].Values[0].Should().Be("bookmark1");
                }
            }

            private static Internal.Driver NewDriver()
            {
                var driver = new Internal.Driver(new Uri("neo4j://myTest.org"), false,
                    new TestConnectionProvider(Mock.Of<IConnection>()),
                    null, null, null, Config.Default);
                return driver;
            }
        }

        internal static Mock<IConnection> MockedConnectionWithSuccessResponse(IBoltProtocol protocol = null)
        {
            var mockConn = new Mock<IConnection>();
            // Whenever you enqueue any message, you immediately receives a response
            mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                    It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                .Returns(Task.CompletedTask)
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
            public IDictionary<string, string> RoutingContext { get; set; }

            public TestConnectionProvider(IConnection connection)
            {
                Connection = connection;
                RoutingContext = Connection.RoutingContext;
            }

            public void Dispose()
            {
                // do nothing
            }

            public Task<IConnection> AcquireAsync(AccessMode mode, string database, string impersonatedUser, Bookmarks bookmarks)
            {
                return Task.FromResult(Connection);
            }

            public Task CloseAsync()
            {
                return Task.CompletedTask;
            }
            
            public Task<IServerInfo> VerifyConnectivityAndGetInfoAsync()
            {
                throw new NotSupportedException();
            }

            public Task VerifyConnectivityAsync()
            {
                return Task.CompletedTask;
            }

            public Task<bool> SupportsMultiDbAsync()
            {
                return Task.FromResult(true);
            }

			public IRoutingTable GetRoutingTable(string database)
			{
				throw new NotSupportedException();
			}
        }

        public class BookmarksManager
        {
            [Fact]
            public void ShouldSyncBookmarksOnUpdateBookmarks()
            {
                var bookmarkManager = new Mock<IBookmarkManager>();

                using (var session = new AsyncSession(null, null, database: "test", bookmarkManager: bookmarkManager.Object))
                {
                    session.UpdateBookmarks(new InternalBookmarks("a"));
                    bookmarkManager.Verify(x => x.UpdateBookmarks("test", Array.Empty<string>(), new[] { "a" }), Times.Once);
                    session.UpdateBookmarks(new InternalBookmarks("b"));
                    bookmarkManager.Verify(x => x.UpdateBookmarks("test", new[] { "a" }, new[] { "b" }), Times.Once);
                }   

                bookmarkManager.Verify(x => x.UpdateBookmarks("test", It.IsAny<string[]>(), It.IsAny<string[]>()), Times.Exactly(2));
                bookmarkManager.Verify(x => x.UpdateBookmarks("test", new[] { "a" }, new[] { "b" }), Times.Once);
            }
        }
    }
}