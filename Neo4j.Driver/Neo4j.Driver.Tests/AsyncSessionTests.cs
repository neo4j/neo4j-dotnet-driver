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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Routing;
using Xunit;

namespace Neo4j.Driver.Tests;

public class AsyncSessionTests
{
    internal static AsyncSession NewSession(IConnection connection, bool reactive = false, ILogger logger = null)
    {
        return new AsyncSession(
            new TestConnectionProvider(connection),
            logger ?? NullLogger.Instance,
            null,
            0,
            new Driver.SessionConfig(),
            reactive,
            false);
    }

    internal static Mock<IConnection> NewMockedConnection(Mock<IBoltProtocol> protocol = null)
    {
        var mockProtocol = protocol ?? new Mock<IBoltProtocol>();
        var mockConn = new Mock<IConnection>();
        mockConn.Setup(x => x.IsOpen).Returns(true);
        mockConn
            .SetupGet(x => x.BoltProtocol)
            .Returns(mockProtocol.Object);

        return mockConn;
    }

    internal static string FakeABookmark(int num)
    {
        return $"bookmark-{num}";
    }

    internal static Mock<IConnection> MockedConnectionWithSuccessResponse(IBoltProtocol protocol = null)
    {
        var mockConn = new Mock<IConnection>();
        // Whenever you enqueue any message, you immediately receives a response
        mockConn.Setup(
                x => x.EnqueueAsync(
                    It.IsAny<IRequestMessage>(),
                    It.IsAny<IResponseHandler>()))
            .Returns(Task.CompletedTask)
            .Callback((IRequestMessage _, IResponseHandler h1) => { h1.OnSuccess(new Dictionary<string, object>()); });

        if (protocol == null)
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            protocol = mockProtocol.Object;
        }

        mockConn.Setup(x => x.BoltProtocol).Returns(protocol);
        mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Write);
        return mockConn;
    }

    public class RunAsyncMethod
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldDelegateToProtocolRunAutoCommitTxAsync(bool reactive)
        {
            var mockConn = new Mock<IConnection>();
            var session = NewSession(mockConn.Object, reactive);
            await session.RunAsync("lalalal");

            mockConn.Verify(
                x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>()),
                Times.Once);
        }
    }

    public class BeginTransactionAsyncMethod
    {
        [Fact]
        public async void ShouldReturnTransactionConfigAsItIs()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection();
            mockConn
                .SetupGet(x => x.BoltProtocol)
                .Returns(mockProtocol.Object);

            var session = NewSession(mockConn.Object);
            var tx = await session.BeginTransactionAsync(
                o => o.WithMetadata(new Dictionary<string, object> { ["key"] = "value" })
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
        public async void ShouldDefaultToBlockingTransactionStart()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = new Mock<IConnection>();
            mockConn.Setup(x => x.IsOpen).Returns(true);
            mockConn
                .SetupGet(x => x.BoltProtocol)
                .Returns(mockProtocol.Object);

            var session = NewSession(mockConn.Object);
            var tx = await session.BeginTransactionAsync();

            mockProtocol.Verify(
                x =>
                    x.BeginTransactionAsync(
                        It.IsAny<IConnection>(),
                        It.Is<BeginTransactionParams>(y => y.TransactionInfo.AwaitBegin == true)),
                Times.Once);
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
            var mockConn = NewMockedConnection(mockProtocol);
            mockProtocol.Setup(
                    x =>
                        x.BeginTransactionAsync(
                            It.IsAny<IConnection>(),
                            It.IsAny<BeginTransactionParams>()))
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
            var mockConn = NewMockedConnection(mockProtocol);
            var calls = 0;
            mockProtocol.Setup(
                    x =>
                        x.BeginTransactionAsync(
                            It.IsAny<IConnection>(),
                            It.IsAny<BeginTransactionParams>()))
                .Returns(Task.CompletedTask)
                .Callback(
                    () =>
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

    public class PipelinedRunTransactionMethod
    {
        [Fact]
        public async void PipelinedShouldBeginWithoutBlocking()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = new Mock<IConnection>();
            mockConn.Setup(x => x.IsOpen).Returns(true);

            mockConn
                .SetupGet(x => x.BoltProtocol)
                .Returns(mockProtocol.Object);

            var session = new AsyncSession(
                new TestConnectionProvider(mockConn.Object),
                null,
                new AsyncRetryLogic(TimeSpan.Zero, null),
                0,
                new Driver.SessionConfig(),
                false,
                false);

            await session.PipelinedExecuteReadAsync(
                _ => Task.FromResult(null as EagerResult<IRecord[]>),
                new TransactionConfig());

            mockProtocol.Verify(
                x =>
                    x.BeginTransactionAsync(
                        It.IsAny<IConnection>(),
                        It.Is<BeginTransactionParams>(y => y.TransactionInfo.AwaitBegin == false)),
                Times.Once);
        }
    }

    public class CloseAsyncMethod
    {
        [Fact]
        public async void ShouldCloseConnectionIfBeginTxFailed()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection(mockProtocol);
            mockProtocol.Setup(
                    x =>
                        x.BeginTransactionAsync(
                            It.IsAny<IConnection>(),
                            It.IsAny<BeginTransactionParams>()))
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
            var mockConn = NewMockedConnection(mockProtocol);
            var session = NewSession(mockConn.Object);
            await session.BeginTransactionAsync();
            await session.CloseAsync();

            mockProtocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Once);
            mockConn.Verify(x => x.CloseAsync(), Times.Once);
        }

        [Fact]
        public async void ShouldCloseConnectionOnCloseAsync()
        {
            var mockConn = NewMockedConnection();
            mockConn.Setup(
                    x => x.EnqueueAsync(
                        It.IsAny<IRequestMessage>(),
                        It.IsAny<IResponseHandler>()))
                .Callback<IRequestMessage, IResponseHandler>(
                    (_, h1) => { h1.OnSuccess(new Dictionary<string, object>()); });

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
            var session = driver.AsyncSession(
                b =>
                    b.WithDatabase("molly")
                        .WithDefaultAccessMode(AccessMode.Read)
                        .WithFetchSize(17)
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

        private static Driver.Internal.Driver NewDriver()
        {
            var driver = new Driver.Internal.Driver(
                new Uri("neo4j://myTest.org"),
                new TestConnectionProvider(Mock.Of<IConnection>()),
                null,
                TestDriverContext.MockContext);

            return driver;
        }
    }

    private class TestConnectionProvider : IConnectionProvider
    {
        public TestConnectionProvider(IConnection connection)
        {
            Connection = connection;
            RoutingContext = Connection.RoutingContext;
        }

        private IConnection Connection { get; }
        private AccessMode Mode { get; set; }
        public IDictionary<string, string> RoutingContext { get; }

        public Task<IConnection> AcquireAsync(
            AccessMode mode,
            string database,
            Driver.SessionConfig sessionConfig,
            Bookmarks bookmarks,
            bool forceAuth = false)
        {
            return Task.FromResult(Connection);
        }

        public Task<IServerInfo> VerifyConnectivityAndGetInfoAsync()
        {
            throw new NotSupportedException();
        }

        public DriverContext DriverContext => new(
            new Uri("neo4j://myTest.org"),
            AuthTokenManagers.Static(AuthTokens.None),
            new Config());

        public Task<bool> SupportsMultiDbAsync()
        {
            return Task.FromResult(true);
        }

        public Task<bool> SupportsReAuthAsync()
        {
            throw new NotImplementedException();
        }

        public IRoutingTable GetRoutingTable(string database)
        {
            throw new NotSupportedException();
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // do nothing
        }

        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        public Task VerifyConnectivityAsync()
        {
            return Task.CompletedTask;
        }
    }

    public class BookmarksManager
    {
        [Fact]
        public void ShouldSyncBookmarksOnUpdateBookmarks()
        {
            var bookmarkManager = new Mock<IBookmarkManager>();

            var cfg = new SessionConfigBuilder(new Driver.SessionConfig())
                .WithDatabase("test")
                .WithBookmarkManager(bookmarkManager.Object)
                .Build();

            using (var session = new AsyncSession(null, null, null, 0, cfg, false, false))
            {
                session.UpdateBookmarks(new InternalBookmarks("a"));
                bookmarkManager.Verify(
                    x => x.UpdateBookmarksAsync(
                        Array.Empty<string>(),
                        new[] { "a" },
                        It.IsAny<CancellationToken>()),
                    Times.Once);

                session.UpdateBookmarks(new InternalBookmarks("b"));
                bookmarkManager.Verify(
                    x => x.UpdateBookmarksAsync(new[] { "a" }, new[] { "b" }, It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            bookmarkManager.Verify(
                x => x.UpdateBookmarksAsync(
                    It.IsAny<string[]>(),
                    It.IsAny<string[]>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            bookmarkManager.Verify(
                x => x.UpdateBookmarksAsync(new[] { "a" }, new[] { "b" }, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
