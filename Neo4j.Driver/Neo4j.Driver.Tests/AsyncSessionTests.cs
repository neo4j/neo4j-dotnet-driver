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
using Neo4j.Driver.Internal.HomeDbCaching;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.Internal.Telemetry;
using Xunit;

namespace Neo4j.Driver.Tests;

public class AsyncSessionTests
{
    internal static AsyncSession NewSession(IConnection connection, bool reactive = false, INeo4jLogger neo4JLogger = null)
    {
        return new AsyncSession(
            new TestConnectionProvider(connection),
            neo4JLogger ?? NullNeo4JLogger.Instance,
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
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldAcceptAnonymousObjectParametersWithTransactionConfig()
        {
            // https://github.com/neo4j/neo4j-dotnet-driver/issues/855
            var mockConn = new Mock<IConnection>();
            IAsyncSession session = NewSession(mockConn.Object);

            await session.RunAsync(
                "CREATE (a:Person {name: $name})",
                new { name = "John" },
                conf => conf.WithTimeout(TimeSpan.FromSeconds(5)));

            mockConn.Verify(
                x => x.RunInAutoCommitTransactionAsync(
                    It.Is<AutoCommitParams>(p =>
                        p.Query.Text == "CREATE (a:Person {name: $name})" &&
                        p.Query.Parameters.ContainsKey("name") &&
                        (string)p.Query.Parameters["name"] == "John" &&
                        p.Config.Timeout == TimeSpan.FromSeconds(5)),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()),
                Times.Once);
        }
    }

    public class BeginTransactionAsyncMethod
    {
        [Fact]
        public async Task ShouldReturnTransactionConfigAsItIs()
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
        public async Task ShouldNotAllowNewTxWhileOneIsRunning()
        {
            var mockConn = NewMockedConnection();
            var session = NewSession(mockConn.Object);
            await session.BeginTransactionAsync();
            var error = await Record.ExceptionAsync(() => session.BeginTransactionAsync());
            error.Should().BeOfType<TransactionNestingException>();
        }

        [Fact]
        public async Task ShouldBeAbleToOpenTxAfterPreviousIsClosed()
        {
            var mockConn = NewMockedConnection();
            var session = NewSession(mockConn.Object);
            var tx = await session.BeginTransactionAsync();
            await tx.RollbackAsync();
            tx = await session.BeginTransactionAsync();
        }

        [Fact]
        public async Task ShouldNotBeAbleToUseSessionWhileOngoingTransaction()
        {
            var mockConn = NewMockedConnection();
            var session = NewSession(mockConn.Object);
            var tx = await session.BeginTransactionAsync();

            var error = await Record.ExceptionAsync(() => session.RunAsync("lalal"));
            error.Should().BeOfType<TransactionNestingException>();
        }

        [Fact]
        public async Task ShouldDefaultToBlockingTransactionStart()
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
                        It.Is<BeginTransactionParams>(y => y.TransactionInfo.AwaitBegin == true),
                        It.IsAny<HomeDbCacheKey>(),
                        It.IsAny<IHomeDbCache>(),
                        It.IsAny<Driver.SessionConfig>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldBeAbleToUseSessionAgainWhenTransactionIsClosed()
        {
            var mockConn = MockedConnectionWithSuccessResponse();

            var session = NewSession(mockConn.Object);
            var tx = await session.BeginTransactionAsync();
            await tx.RollbackAsync();

            await session.RunAsync("lalal");
        }

        [Fact]
        public async Task ShouldClosePreviousRunConnectionWhenRunMoreQueries()
        {
            var mockConn = MockedConnectionWithSuccessResponse();
            var session = NewSession(mockConn.Object);
            await session.RunAsync("lalal");

            await session.RunAsync("bibib");
            mockConn.Verify(c => c.CloseAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldClosePreviousRunConnectionWhenRunMoreTransactions()
        {
            var mockConn = MockedConnectionWithSuccessResponse();
            mockConn.Setup(x => x.IsOpen).Returns(false);
            var session = NewSession(mockConn.Object);
            await session.RunAsync("lala");

            await session.BeginTransactionAsync();
            mockConn.Verify(c => c.CloseAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldCloseConnectionOnRunIfBeginTxFailed()
        {
            // Given
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection(mockProtocol);
            mockProtocol.Setup(
                    x =>
                        x.BeginTransactionAsync(
                            It.IsAny<IConnection>(),
                            It.IsAny<BeginTransactionParams>(),
                            It.IsAny<HomeDbCacheKey>(),
                            It.IsAny<IHomeDbCache>(),
                            It.IsAny<Driver.SessionConfig>()))
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
        public async Task ShouldCloseConnectionOnNewBeginTxIfBeginTxFailed()
        {
            // Given
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection(mockProtocol);
            var calls = 0;
            mockProtocol.Setup(
                    x =>
                        x.BeginTransactionAsync(
                            It.IsAny<IConnection>(),
                            It.IsAny<BeginTransactionParams>(),
                            It.IsAny<HomeDbCacheKey>(),
                            It.IsAny<IHomeDbCache>(),
                            It.IsAny<Driver.SessionConfig>()))
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
        public async Task PipelinedShouldBeginWithoutBlocking()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = new Mock<IConnection>();
            mockConn.Setup(x => x.IsOpen).Returns(true);

            mockConn
                .SetupGet(x => x.BoltProtocol)
                .Returns(mockProtocol.Object);

            var session = new AsyncSession(
                new TestConnectionProvider(mockConn.Object),
                NullNeo4JLogger.Instance,
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
                        It.Is<BeginTransactionParams>(y => y.TransactionInfo.AwaitBegin == false),
                        It.IsAny<HomeDbCacheKey>(),
                        It.IsAny<IHomeDbCache>(),
                        It.IsAny<Driver.SessionConfig>()),
                Times.Once);
        }
    }

    public class CloseAsyncMethod
    {
        [Fact]
        public async Task ShouldCloseConnectionIfBeginTxFailed()
        {
            var mockProtocol = new Mock<IBoltProtocol>();
            var mockConn = NewMockedConnection(mockProtocol);
            mockProtocol.Setup(
                    x =>
                        x.BeginTransactionAsync(
                            It.IsAny<IConnection>(),
                            It.IsAny<BeginTransactionParams>(),
                            It.IsAny<HomeDbCacheKey>(),
                            It.IsAny<IHomeDbCache>(),
                            It.IsAny<Driver.SessionConfig>()))
                .Throws(new IOException("Triggered an error when beginTx"));

            var session = NewSession(mockConn.Object);
            var error = await Record.ExceptionAsync(() => session.BeginTransactionAsync());
            error.Should().BeOfType<IOException>();
            await session.CloseAsync();

            mockConn.Verify(x => x.CloseAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldCloseTxOnCloseAsync()
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
        public async Task ShouldCloseConnectionOnCloseAsync()
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
            var context = TestDriverContext.MockContext;
            var server = new BoltProtocolAdapter(
                new TestConnectionProvider(Mock.Of<IConnection>()),
                context);

            var driver = new Driver.Internal.Driver(
                new Uri("neo4j://myTest.org"),
                server,
                context,
                Mock.Of<IDriverComposition>());

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

        /// <inheritdoc />
        public bool IsDirectDriver => false;

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

        public Task<IRoutingTable> ForceRoutingTableUpdateAsync(string database, Driver.SessionConfig sessionConfig, Bookmarks bookmarks)
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

            using (var session = new AsyncSession(
                       null,
                       null,
                       null,
                       0,
                       cfg,
                       false,
                       false))
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

    // ── Idempotent retry tests ──────────────────────────────────────────────

    public class AutoCommitIdempotentRetry
    {
        internal static TransientException IdempotentFailure(string message = "admission control")
        {
            var fm = new FailureMessage("Neo.TransientError.General.MemoryPoolOutOfMemoryError", message)
            {
                GqlDiagnosticRecord = new Dictionary<string, object> { ["_idempotent"] = true }
            };

            return new TransientException(fm, null);
        }

        private static Mock<IInternalResultCursor> MockCursor(Exception runError = null)
        {
            var cursor = new Mock<IInternalResultCursor>();
            cursor.Setup(x => x.GetRunCompletionErrorAsync())
                .ReturnsAsync(runError);

            return cursor;
        }

        private static AsyncSession NewSessionWith(
            IConnectionProvider provider,
            bool? sessionDisable = null,
            bool driverDisable = false)
        {
            var config = new Config { DisableAutoCommitRetries = driverDisable };
            var sessionConfig = new Driver.SessionConfig { DriverContext = new DriverContext(new Uri("bolt://localhost"), null, config) };
            if (sessionDisable.HasValue)
            {
                sessionConfig.DisableAutoCommitRetries = sessionDisable;
            }

            return new AsyncSession(
                provider,
                NullNeo4JLogger.Instance,
                null,
                1000,
                sessionConfig,
                false,
                false);
        }

        /// <summary>
        /// A <see cref="IConnectionProvider"/> that serves connections from a queue so tests can
        /// inject different behavior on the first vs. second acquire.
        /// </summary>
        private class SequentialConnectionProvider : IConnectionProvider
        {
            private readonly Queue<IConnection> _connections;

            public SequentialConnectionProvider(params IConnection[] connections)
            {
                _connections = new Queue<IConnection>(connections);
            }

            public IDictionary<string, string> RoutingContext => null;
            public bool IsDirectDriver => false;
            public DriverContext DriverContext => null;

            public Task<IConnection> AcquireAsync(
                AccessMode mode,
                string database,
                Driver.SessionConfig sessionConfig,
                Bookmarks bookmarks,
                bool forceAuth = false)
            {
                return Task.FromResult(_connections.Dequeue());
            }

            public Task VerifyConnectivityAsync() => Task.CompletedTask;
            public Task<IServerInfo> VerifyConnectivityAndGetInfoAsync() => Task.FromResult<IServerInfo>(null);
            public Task<bool> SupportsMultiDbAsync() => Task.FromResult(true);
            public Task<bool> SupportsReAuthAsync() => Task.FromResult(false);
            public IRoutingTable GetRoutingTable(string database) => null;
            public Task<IRoutingTable> ForceRoutingTableUpdateAsync(string database, Driver.SessionConfig sessionConfig, Bookmarks bookmarks) =>
                Task.FromResult<IRoutingTable>(null);
            public ValueTask DisposeAsync() => new(Task.CompletedTask);
            public void Dispose() { }
            public Task CloseAsync() => Task.CompletedTask;
        }

        // ── Happy path: retry succeeds ────────────────────────────────────

        [Fact]
        public async Task ShouldRetryOnceWhenRunFailsWithIdempotentError()
        {
            var failingCursor = MockCursor(IdempotentFailure());
            var successCursor = MockCursor();

            var firstConn = new Mock<IConnection>();
            firstConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(failingCursor.Object);

            firstConn.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask);

            var secondConn = new Mock<IConnection>();
            secondConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(successCursor.Object);

            var session = NewSessionWith(new SequentialConnectionProvider(firstConn.Object, secondConn.Object));
            var cursor = await session.RunAsync("RETURN 1");

            firstConn.Verify(
                x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()),
                Times.Once);

            secondConn.Verify(
                x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()),
                Times.Once);

            firstConn.Verify(x => x.CloseAsync(), Times.Once);
            cursor.Should().BeSameAs(successCursor.Object);
        }

        [Fact]
        public async Task ShouldPassSameQueryParamsOnRetry()
        {
            AutoCommitParams capturedFirst = null;
            AutoCommitParams capturedRetry = null;

            var firstCursor = MockCursor(IdempotentFailure());
            var firstConn = new Mock<IConnection>();
            firstConn
                .Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync((AutoCommitParams p, INotificationsConfig _, IHomeDbCache __) =>
                {
                    capturedFirst = p;
                    return firstCursor.Object;
                });

            firstConn.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask);

            var secondConn = new Mock<IConnection>();
            secondConn
                .Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync((AutoCommitParams p, INotificationsConfig _, IHomeDbCache __) =>
                {
                    capturedRetry = p;
                    return MockCursor().Object;
                });

            var session = NewSessionWith(new SequentialConnectionProvider(firstConn.Object, secondConn.Object));
            await session.RunAsync("RETURN 1");

            capturedFirst.TransactionInfo.Should()
                .BeSameAs(
                    capturedRetry.TransactionInfo,
                    "TransactionInfo must be reused so telemetry isn't double-counted");
        }

        // ── Retry also fails: propagate ───────────────────────────────────

        [Fact]
        public async Task ShouldReturnRetryCursorWhenRetryAlsoFails()
        {
            var firstCursor = MockCursor(IdempotentFailure("first"));
            var retryError = IdempotentFailure("second");
            var retryCursor = MockCursor(retryError);

            var firstConn = new Mock<IConnection>();
            firstConn
                .Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(firstCursor.Object);

            firstConn.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask);

            var secondConn = new Mock<IConnection>();
            secondConn
                .Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(retryCursor.Object);

            var session = NewSessionWith(new SequentialConnectionProvider(firstConn.Object, secondConn.Object));
            var cursor = await session.RunAsync("RETURN 1");

            cursor.Should().BeSameAs(retryCursor.Object,
                "retry failure should surface lazily, same as a non-retried failure");
        }

        // ── Non-retryable errors: return cursor, error surfaces lazily ────

        [Fact]
        public async Task ShouldNotRetryWhenGetRunCompletionErrorReturnsNull()
        {
            // GetRunCompletionErrorAsync returning null means the failure came from a
            // pre-RUN message (e.g. TELEMETRY). Even if the underlying error would be
            // idempotent, the session must NOT retry — the cursor is returned and the
            // error surfaces lazily when the user consumes it.
            var cursor = MockCursor(null); // GetRunCompletionErrorAsync returns null

            var conn = new Mock<IConnection>();
            conn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(cursor.Object);

            var session = NewSessionWith(new SequentialConnectionProvider(conn.Object));
            var result = await session.RunAsync("RETURN 1");

            result.Should().BeSameAs(cursor.Object);
            conn.Verify(
                x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()),
                Times.Once,
                "null from GetRunCompletionErrorAsync must not trigger a retry, " +
                "even if the underlying failure would be idempotent");
        }

        [Fact]
        public async Task ShouldNotRetryOnNonIdempotentTransientError()
        {
            var nonIdempotentError = new TransientException("Neo.TransientError.General.OutOfMemoryError", "OOM");
            var failingCursor = MockCursor(nonIdempotentError);

            var conn = new Mock<IConnection>();
            conn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(failingCursor.Object);

            var session = NewSessionWith(new SequentialConnectionProvider(conn.Object));
            var cursor = await session.RunAsync("RETURN 1");

            cursor.Should().BeSameAs(failingCursor.Object);
            conn.Verify(
                x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()),
                Times.Once,
                "non-idempotent failures must not trigger a retry");
        }

        [Fact]
        public async Task ShouldNotRetryOnIoError()
        {
            var failingCursor = MockCursor(new IOException("connection reset"));

            var conn = new Mock<IConnection>();
            conn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(failingCursor.Object);

            var session = NewSessionWith(new SequentialConnectionProvider(conn.Object));
            var cursor = await session.RunAsync("RETURN 1");

            cursor.Should().BeSameAs(failingCursor.Object);
            conn.Verify(
                x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()),
                Times.Once,
                "IO errors must not trigger a retry");
        }

        // ── Opt-out via config ────────────────────────────────────────────

        [Fact]
        public async Task ShouldNotRetryWhenDisabledAtSessionLevel()
        {
            var failingCursor = new Mock<IResultCursor>();
            failingCursor.Setup(x => x.KeysAsync()).ThrowsAsync(IdempotentFailure());

            var conn = new Mock<IConnection>();
            conn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(failingCursor.Object);

            var session = NewSessionWith(
                new SequentialConnectionProvider(conn.Object),
                sessionDisable: true);

            await session.RunAsync("RETURN 1");

            conn.Verify(
                x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()),
                Times.Once,
                "retries disabled at session level should prevent retry");
        }

        [Fact]
        public async Task SessionLevelOverridesTrueWhenDriverLevelIsFalse()
        {
            var failingCursor = new Mock<IResultCursor>();

            var conn = new Mock<IConnection>();
            conn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(failingCursor.Object);

            var session = NewSessionWith(
                new SequentialConnectionProvider(conn.Object),
                sessionDisable: true,
                driverDisable: false);

            await session.RunAsync("RETURN 1");

            conn.Verify(
                x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()),
                Times.Once);
        }

        [Fact]
        public async Task SessionLevelOverridesFalseWhenDriverLevelIsTrue()
        {
            var firstCursor = MockCursor(IdempotentFailure());
            var successCursor = MockCursor();

            var firstConn = new Mock<IConnection>();
            firstConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(firstCursor.Object);

            firstConn.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask);

            var secondConn = new Mock<IConnection>();
            secondConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(successCursor.Object);

            var session = NewSessionWith(
                new SequentialConnectionProvider(firstConn.Object, secondConn.Object),
                sessionDisable: false,
                driverDisable: true);

            var cursor = await session.RunAsync("RETURN 1");
            cursor.Should().BeSameAs(successCursor.Object,
                "session-level false should override driver-level true and allow retry");
        }

        // ── Bug regressions ──────────────────────────────────────────────

        [Fact]
        public async Task RetryQueryShouldUseFreshBookmarks()
        {
            AutoCommitParams capturedFirst = null;
            AutoCommitParams capturedRetry = null;

            var failingCursor = MockCursor(IdempotentFailure());
            var firstConn = new Mock<IConnection>();
            firstConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync((AutoCommitParams p, INotificationsConfig _, IHomeDbCache __) =>
                {
                    capturedFirst = p;
                    return failingCursor.Object;
                });

            firstConn.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask);

            var secondConn = new Mock<IConnection>();
            secondConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync((AutoCommitParams p, INotificationsConfig _, IHomeDbCache __) =>
                {
                    capturedRetry = p;
                    return MockCursor().Object;
                });

            var session = NewSessionWith(new SequentialConnectionProvider(firstConn.Object, secondConn.Object));
            await session.RunAsync("RETURN 1");

            capturedFirst.Should().NotBeNull();
            capturedRetry.Should().NotBeNull();
            capturedRetry.Bookmarks.Should().BeSameAs(capturedFirst.Bookmarks,
                "retry must use the session's LastBookmarks (refreshed after re-acquiring connection)");
        }

        [Fact]
        public async Task ShouldNotThrowNullReferenceWhenResourceHandlerNullsConnectionBeforeRetryClosure()
        {
            var failingCursor = MockCursor(IdempotentFailure());

            var firstConn = new Mock<IConnection>();
            firstConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(failingCursor.Object);

            firstConn.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask);

            var secondConn = new Mock<IConnection>();
            secondConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(MockCursor().Object);

            var session = NewSessionWith(new SequentialConnectionProvider(firstConn.Object, secondConn.Object));

            var exception = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));
            exception.Should().BeNull("the null-connection guard must prevent NullReferenceException");
        }

        // ── Retry transparency ───────────────────────────────────────────

        [Fact]
        public async Task RetryShouldBeInvisibleToTheCaller()
        {
            var serverError = new ClientException("Neo.ClientError.Statement.SyntaxError", "bad query");

            // Scenario A: no retry occurred (non-idempotent error on first RUN).
            var directCursor = MockCursor(serverError);
            var directConn = new Mock<IConnection>();
            directConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(directCursor.Object);

            var directSession = NewSessionWith(new SequentialConnectionProvider(directConn.Object));
            var directExc = await Record.ExceptionAsync(() => directSession.RunAsync("BAD"));
            // RunAsync itself should not throw.
            directExc.Should().BeNull();

            // Scenario B: an idempotent retry happened, but the retry produced
            // the same non-idempotent error.
            var firstCursor = MockCursor(IdempotentFailure());
            var retryCursor = MockCursor(serverError);

            var firstConn = new Mock<IConnection>();
            firstConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(firstCursor.Object);

            firstConn.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask);

            var retryConn = new Mock<IConnection>();
            retryConn.Setup(x => x.RunInAutoCommitTransactionAsync(
                    It.IsAny<AutoCommitParams>(),
                    It.IsAny<INotificationsConfig>(),
                    It.IsAny<IHomeDbCache>()))
                .ReturnsAsync(retryCursor.Object);

            var retrySession = NewSessionWith(
                new SequentialConnectionProvider(firstConn.Object, retryConn.Object));

            var retryExc = await Record.ExceptionAsync(() => retrySession.RunAsync("BAD"));
            // RunAsync itself should not throw here either — identical to scenario A.
            retryExc.Should().BeNull(
                "the caller should not be able to tell that an internal retry happened");
        }
    }
}
