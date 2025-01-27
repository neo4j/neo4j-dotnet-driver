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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Util;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Tests;

public class ConnectionPoolTests
{
    private static IPooledConnectionFactory ReusableConnectionFactory
    {
        get
        {
            var mock = new Mock<IConnection>();
            mock.Setup(x => x.IsOpen).Returns(true);
            mock.Setup(x => x.ToString()).Returns("ReusableConnectionFactory");
            mock.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            return new MockedConnectionFactory(mock.Object);
        }
    }

    private static ITimer MockedTimer
    {
        get
        {
            var mock = new Mock<ITimer>();
            mock.Setup(t => t.ElapsedMilliseconds).Returns(0);
            return mock.Object;
        }
    }

    private static ConnectionPool NewConnectionPool(
        BlockingCollection<IPooledConnection> idleConnections = null,
        ConcurrentHashSet<IPooledConnection> inUseConnections = null,
        DriverContext driverContext = null,
        bool isConnectionValid = true)
    {
        driverContext ??= TestDriverContext.MockContext;

        return new ConnectionPool(
            new MockedConnectionFactory(),
            idleConnections,
            inUseConnections,
            driverContext,
            new TestConnectionValidator(isConnectionValid));
    }

    private static ConnectionPool NewConnectionPoolWithConnectionTimeoutCheckDisabled(
        BlockingCollection<IPooledConnection> idleConnections,
        ConcurrentHashSet<IPooledConnection> inUseConnections = null)
    {
        return new ConnectionPool(
            new MockedConnectionFactory(),
            idleConnections,
            inUseConnections,
            validator: new ConnectionValidator(Config.InfiniteInterval, Config.InfiniteInterval),
            driverContext: TestDriverContext.MockContext);
    }

    public class AcquireMethod
    {
        private readonly ITestOutputHelper _output;

        public AcquireMethod(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ShouldCallConnInit()
        {
            // Given
            var mock = new Mock<IConnection>();
            var connFactory = new MockedConnectionFactory(mock.Object);
            var connectionPool = new ConnectionPool(
                connFactory,
                validator: new TestConnectionValidator(),
                driverContext: TestDriverContext.MockContext);

            // When
            await connectionPool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);

            //Then
            mock.Verify(
                x => x.InitAsync(It.IsAny<SessionConfig>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldBlockWhenMaxPoolSizeReached()
        {
            const int delayTime = 2000; // Delay time in milliseconds.
            var pool = NewConnectionPool(
                driverContext: TestDriverContext.With(
                    config: x =>
                        x.WithMaxConnectionPoolSize(2).WithConnectionAcquisitionTimeout(TimeSpan.FromMinutes(2))));

            var conn1 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            var conn2 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(2);

            var timer = new Stopwatch();
            var blockingAcquire =
                new Task<Task<IConnection>>(() => pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));

            timer.Start();
            blockingAcquire.Start();

            await Task.Delay(delayTime); // delay a bit here
            await conn1.CloseAsync();

            var conn3 = await blockingAcquire.Unwrap();
            timer.Stop();

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(2);
            timer.Elapsed.TotalMilliseconds.Should().BeGreaterOrEqualTo(delayTime, 100);
            //TotalSeconds.Should().BeGreaterOrEqualTo(1, 0.1);
        }

        [Fact]
        public async Task ShouldThrowClientExceptionWhenFailedToAcquireWithinTimeout()
        {
            var pool = NewConnectionPool(
                driverContext: TestDriverContext.With(
                    config: x =>
                        x.WithMaxConnectionPoolSize(2)
                            .WithConnectionAcquisitionTimeout(TimeSpan.FromMilliseconds(250))));

            await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(2);

            var exception =
                await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));

            exception.Should()
                .BeOfType<ClientException>()
                .Which.Message.Should()
                .StartWith("Failed to obtain a connection from pool");
        }

        [Fact]
        public async Task ShouldNotExceedIdleLimit()
        {
            var pool = NewConnectionPool(
                driverContext: TestDriverContext.With(config: x => x.WithMaxIdleConnectionPoolSize(2)));

            var conns = new List<IConnection>();
            for (var i = 0; i < 4; i++)
            {
                conns.Add(await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));
                pool.NumberOfIdleConnections.Should().Be(0);
            }

            foreach (var conn in conns)
            {
                await conn.CloseAsync();
                pool.NumberOfIdleConnections.Should().BeLessOrEqualTo(2);
            }

            pool.NumberOfIdleConnections.Should().Be(2);
        }

        [Fact]
        public async Task ShouldAcquireFromPoolIfAvailable()
        {
            var pool = NewConnectionPool(
                driverContext: TestDriverContext.With(config: x => x.WithMaxIdleConnectionPoolSize(2)));

            for (var i = 0; i < 4; i++)
            {
                var conn = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
                pool.NumberOfInUseConnections.Should().Be(1);
                pool.NumberOfIdleConnections.Should().Be(0);
                await conn.CloseAsync();
                pool.NumberOfIdleConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);
            }

            pool.NumberOfIdleConnections.Should().Be(1);
            pool.NumberOfInUseConnections.Should().Be(0);
        }

        [Fact]
        public async Task ShouldCreateNewWhenQueueIsEmpty()
        {
            var pool = NewConnectionPool();

            await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);
        }

        [Fact]
        public async Task ShouldCloseConnectionIfFailedToCreate()
        {
            var connMock = new Mock<IPooledConnection>();
            connMock.SetupGet(x => x.Version).Returns(BoltProtocolVersion.V5_0);
            connMock.Setup(
                    x => x.InitAsync(
                        It.IsAny<SessionConfig>(),
                        It.IsAny<CancellationToken>()))
                .Throws<NotImplementedException>();

            var connFactory = new MockedConnectionFactory(connMock.Object);
            var pool = new ConnectionPool(
                connFactory,
                driverContext: TestDriverContext.MockContext);

            var exc = await Record.ExceptionAsync(
                () => pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));

            exc.Should().BeOfType<NotImplementedException>();
            connMock.Verify(x => x.DestroyAsync(), Times.Once);
            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);
        }

        [Fact]
        public async Task ShouldCreateNewWhenQueueOnlyContainsClosedConnections()
        {
            var conns = new BlockingCollection<IPooledConnection>();
            var closedMock = new Mock<IPooledConnection>();
            closedMock.Setup(x => x.IdleTimer).Returns(new StopwatchBasedTimer());
            closedMock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            closedMock.Setup(x => x.IsOpen).Returns(false);

            conns.Add(closedMock.Object);
            var pool = new ConnectionPool(
                ReusableConnectionFactory,
                conns,
                driverContext: new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config()));

            pool.NumberOfIdleConnections.Should().Be(1);
            pool.NumberOfInUseConnections.Should().Be(0);

            var conn = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);
            closedMock.Verify(x => x.IsOpen, Times.Once);
            closedMock.Verify(x => x.DestroyAsync(), Times.Once);

            conn.Should().NotBeNull();
            conn.Should().NotBe(closedMock.Object);
        }

        private Mock<IConnectionValidator> MockValidator(Action<Mock<IConnectionValidator>> setup = null)
        {
            var validator = new Mock<IConnectionValidator>();
            if (setup != null)
            {
                setup(validator);
            }
            else
            {
                validator.Setup(x => x.OnReleaseAsync(It.IsAny<IPooledConnection>())).ReturnsAsync(true);
                validator.Setup(x => x.GetConnectionLifetimeStatus(It.IsAny<IPooledConnection>()))
                    .Returns(AcquireStatus.Healthy);
            }

            return validator;
        }

        [Fact]
        public async Task ShouldReuseWhenOpenConnectionInQueue()
        {
            var conns = new BlockingCollection<IPooledConnection>();
            var mock = new Mock<IPooledConnection>();
            mock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            mock.Setup(x => x.IdleTimer).Returns(new StopwatchBasedTimer());
            mock.Setup(x => x.IsOpen).Returns(true);
            mock.Setup(x => x.LifetimeTimer).Returns(MockedTimer);

            conns.Add(mock.Object);
            var pool = new ConnectionPool(
                new MockedConnectionFactory(),
                conns,
                driverContext: new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config()));

            pool.NumberOfIdleConnections.Should().Be(1);
            pool.NumberOfInUseConnections.Should().Be(0);

            var conn = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);
            mock.Verify(x => x.IsOpen, Times.Once);
            conn.Should().Be(mock.Object);
        }

        [Fact]
        public async Task ShouldReuseOpenConnectionWhenOpenAndClosedConnectionsInQueue()
        {
            var conns = new BlockingCollection<IPooledConnection>();
            var healthyMock = new Mock<IPooledConnection>();
            healthyMock.Setup(x => x.IsOpen).Returns(true);
            healthyMock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            healthyMock.Setup(x => x.LifetimeTimer).Returns(MockedTimer);
            healthyMock.Setup(x => x.IdleTimer).Returns(new StopwatchBasedTimer());
            var unhealthyMock = new Mock<IPooledConnection>();
            unhealthyMock.Setup(x => x.IsOpen).Returns(false);
            unhealthyMock.Setup(x => x.IdleTimer).Returns(new StopwatchBasedTimer());

            conns.Add(unhealthyMock.Object);
            conns.Add(healthyMock.Object);
            var pool = new ConnectionPool(
                new MockedConnectionFactory(),
                conns,
                driverContext:
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config()));

            pool.NumberOfIdleConnections.Should().Be(2);
            pool.NumberOfInUseConnections.Should().Be(0);

            var conn = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);
            unhealthyMock.Verify(x => x.DestroyAsync(), Times.Once);
            healthyMock.Verify(x => x.DestroyAsync(), Times.Never);
            conn.Should().Be(healthyMock.Object);
        }

        [Fact]
        public async Task ShouldCloseIdleTooLongConn()
        {
            // Given
            var mock = new Mock<IPooledConnection>();
            mock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            mock.Setup(x => x.IsOpen).Returns(true);
            var timerMock = new Mock<ITimer>();
            timerMock.Setup(x => x.ElapsedMilliseconds).Returns(1000);
            mock.Setup(x => x.IdleTimer).Returns(timerMock.Object);
            var idleTooLongId = "Molly";
            mock.Setup(x => x.ToString()).Returns(idleTooLongId);

            var conns = new BlockingCollection<IPooledConnection>();
            conns.Add(mock.Object);
            var enableIdleTooLongTest = TimeSpan.FromMilliseconds(100);

            var pool = new ConnectionPool(
                ReusableConnectionFactory,
                conns,
                driverContext: new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config
                    {
                        MaxIdleConnectionPoolSize = 2,
                        ConnectionIdleTimeout = enableIdleTooLongTest
                    }));

            pool.NumberOfIdleConnections.Should().Be(1);
            pool.NumberOfInUseConnections.Should().Be(0);

            // When
            var conn = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);

            // Then
            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);
            mock.Verify(x => x.DestroyAsync(), Times.Once);

            conn.Should().NotBeNull();
            conn.Should().NotBe(idleTooLongId);
        }

        [Fact]
        public async Task ShouldReuseIdleNotTooLongConn()
        {
            // Given
            var mock = new Mock<IPooledConnection>();
            mock.Setup(x => x.IdleTimer).Returns(new StopwatchBasedTimer());
            mock.Setup(x => x.LifetimeTimer).Returns(new StopwatchBasedTimer());
            mock.Setup(x => x.AuthorizationStatus).Returns(AuthorizationStatus.FreshlyAuthenticated);
            mock.Setup(x => x.IsOpen).Returns(true);
            var timerMock = new Mock<ITimer>();
            timerMock.Setup(x => x.ElapsedMilliseconds).Returns(10);
            mock.Setup(x => x.IdleTimer).Returns(timerMock.Object);
            var idleTooLongId = "Molly";
            mock.Setup(x => x.ToString()).Returns(idleTooLongId);
            mock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_0);

            var conns = new BlockingCollection<IPooledConnection>();
            conns.Add(mock.Object);
            var enableIdleTooLongTest = TimeSpan.FromMilliseconds(100);
            var context = new DriverContext(
                new Uri("bolt://localhost:7687"),
                AuthTokenManagers.None,
                new Config
                {
                    MaxIdleConnectionPoolSize = 2,
                    ConnectionIdleTimeout = enableIdleTooLongTest,
                    MaxConnectionLifetime = Config.InfiniteInterval
                });

            var pool = new ConnectionPool(
                ReusableConnectionFactory,
                conns,
                driverContext: context);

            pool.NumberOfIdleConnections.Should().Be(1);
            pool.NumberOfInUseConnections.Should().Be(0);

            // When
            var conn = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);

            // Then
            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);

            conn.Should().Be(mock.Object);
            conn.ToString().Should().Be(idleTooLongId);
        }

        [Fact]
        public async Task ShouldGetTokenFromAuthManager()
        {
            var mockConnection = new Mock<IPooledConnection>();
            mockConnection.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            mockConnection.Setup(x => x.IsOpen).Returns(true);
            var timerMock = new Mock<ITimer>();
            timerMock.Setup(x => x.ElapsedMilliseconds).Returns(1000);
            mockConnection.Setup(x => x.IdleTimer).Returns(timerMock.Object);
            var idleTooLongId = "Molly";
            mockConnection.Setup(x => x.ToString()).Returns(idleTooLongId);

            var conns = new BlockingCollection<IPooledConnection>();
            conns.Add(mockConnection.Object);
            var enableIdleTooLongTest = TimeSpan.FromMilliseconds(100);

            var mockAuthMgr = new Mock<IAuthTokenManager>();
            var context = new DriverContext(
                new Uri("bolt://localhost:7687"),
                mockAuthMgr.Object,
                new Config
                {
                    MaxIdleConnectionPoolSize = 2,
                    ConnectionIdleTimeout = enableIdleTooLongTest
                });

            var pool = new ConnectionPool(
                ReusableConnectionFactory,
                conns,
                driverContext: context);

            await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);

            mockAuthMgr.Verify(x => x.GetTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(500)]
        public async Task ShouldAcquireNewWhenBeingUsedConcurrentlyBy(int numberOfThreads)
        {
            var mockConns = new Queue<Mock<IPooledConnection>>();
            var conns = new BlockingCollection<IPooledConnection>();
            for (var i = 0; i < numberOfThreads; i++)
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IdleTimer).Returns(new StopwatchBasedTimer());
                mock.Setup(x => x.LifetimeTimer).Returns(new StopwatchBasedTimer());
                mock.Setup(x => x.AuthorizationStatus).Returns(AuthorizationStatus.FreshlyAuthenticated);
                mock.Setup(x => x.Version).Returns(BoltProtocolVersion.V4_4);
                mock.Setup(x => x.IsOpen).Returns(true);
                mock.Setup(x => x.ToString()).Returns(i.ToString());
                conns.Add(mock.Object);
                mockConns.Enqueue(mock);
            }

            var pool = NewConnectionPoolWithConnectionTimeoutCheckDisabled(conns);
            pool.NumberOfIdleConnections.Should().Be(numberOfThreads);
            pool.NumberOfInUseConnections.Should().Be(0);

            async Task<string> AcquireConnection(int i)
            {
                var conn = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
                return conn.ToString();
            }

            var tasks = await Task.WhenAll(Enumerable.Range(0, numberOfThreads).Select(AcquireConnection));
            var receivedIds = tasks.ToList();
            receivedIds.Count.Should().Be(numberOfThreads);
            receivedIds.Should().OnlyHaveUniqueItems();
            var ids = Enumerable.Range(0, numberOfThreads).Select(x => x.ToString());
            receivedIds.Should().BeEquivalentTo(ids);

            foreach (var mock in mockConns)
            {
                mock.Verify(x => x.IsOpen, Times.Once);
            }
        }

        // thread-safe test
        // concurrent call of Acquire and Dispose
        [Fact]
        public async Task ShouldThrowExceptionWhenAcquireCalledAfterClose()
        {
            var pool = NewConnectionPool();

            await pool.CloseAsync();
            var exception =
                await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));

            exception.Should().BeOfType<ObjectDisposedException>();
            exception.Message.Should().StartWith("Failed to acquire a new connection");
        }

        // thread-safe test
        // concurrent call of Acquire and Dispose
        [Fact]
        public async Task ShouldCloseAcquiredConnectionIfPoolDisposeStarted()
        {
            // Given
            var idleConnections = new BlockingCollection<IPooledConnection>();
            var healthyMock = new Mock<IPooledConnection>();
            healthyMock.Setup(x => x.IdleTimer).Returns(new StopwatchBasedTimer());
            healthyMock.Setup(x => x.LifetimeTimer).Returns(new StopwatchBasedTimer());
            healthyMock.Setup(x => x.AuthorizationStatus).Returns(AuthorizationStatus.FreshlyAuthenticated);
            var pool = NewConnectionPoolWithConnectionTimeoutCheckDisabled(idleConnections);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);

            // This is to simulate Acquire called first,
            // but before Acquire put a new conn into inUseConn, Dispose get called.
            // Note: Once dispose get called, it is forbidden to put anything into queue.
            healthyMock.Setup(x => x.IsOpen)
                .Returns(true)
                .Callback(() => pool.CloseAsync().Wait()); // Simulate Dispose get called at this time

            idleConnections.Add(healthyMock.Object);
            pool.NumberOfIdleConnections.Should().Be(1);
            // When
            var exception =
                await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);
            healthyMock.Verify(x => x.IsOpen, Times.Once);
            healthyMock.Verify(x => x.DestroyAsync(), Times.Once);
            exception.Should().BeOfType<ObjectDisposedException>();
            exception.Message.Should().StartWith("Failed to acquire a new connection");
        }

        [Fact]
        public async void ShouldTimeoutAfterAcquireAsyncTimeoutIfPoolIsFull()
        {
            var context = new DriverContext(
                new Uri("bolt://localhost:7687"),
                AuthTokenManagers.None,
                new Config
                {
                    ConnectionAcquisitionTimeout = TimeSpan.FromSeconds(10),
                    MaxConnectionPoolSize = 5,
                    MaxIdleConnectionPoolSize = 0
                });

            var pool = NewConnectionPool(driverContext: context);

            for (var i = 0; i < context.Config.MaxConnectionPoolSize; i++)
            {
                await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var exception =
                await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));

            stopWatch.Stop();
            stopWatch.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(10, 0.1);
            exception.Should()
                .BeOfType<ClientException>()
                .Which.Message.Should()
                .StartWith("Failed to obtain a connection from pool");
        }

        [Fact]
        public async void ShouldTimeoutAfterAcquireAsyncTimeoutWhenConnectionIsNotValidated()
        {
            var pool = NewConnectionPool(
                isConnectionValid: false,
                driverContext: new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config { ConnectionAcquisitionTimeout = TimeSpan.FromSeconds(5) }));

            var exception = await Record.ExceptionAsync(
                () =>
                    pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));

            exception.Should()
                .BeOfType<ClientException>()
                .Which
                .Message.Should()
                .StartWith("Failed to obtain a connection from pool within");
        }

        [Theory]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public async Task ShouldManageModePropertyAsync(AccessMode mode)
        {
            var connection = new Mock<IPooledConnection>();

            var idleConnections = new BlockingCollection<IPooledConnection> { connection.Object };

            var pool = NewConnectionPool(
                idleConnections,
                driverContext: new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config()));

            var acquired = await pool.AcquireAsync(mode, null, null, Bookmarks.Empty);
            connection.Verify(x => x.Configure(null, mode));

            await pool.ReleaseAsync((IPooledConnection)acquired);
            connection.Verify(x => x.Configure(null, null));
        }
    }

    public class ReleaseMethod
    {
        [Fact]
        public async Task ShouldReturnToPoolWhenConnectionIsReusableAndPoolIsNotFull()
        {
            var conn = new Mock<IPooledConnection>().Object;

            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            inUseConns.TryAdd(conn);
            var pool = NewConnectionPool(inUseConnections: inUseConns);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);

            await pool.ReleaseAsync(conn);

            pool.NumberOfIdleConnections.Should().Be(1);
            pool.NumberOfInUseConnections.Should().Be(0);
        }

        [Fact]
        public async Task ShouldCloseConnectionWhenConnectionIsClosed()
        {
            var mock = new Mock<IPooledConnection>();
            mock.Setup(x => x.IsOpen).Returns(false);

            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            inUseConns.TryAdd(mock.Object);
            var pool = new ConnectionPool(
                null,
                null,
                inUseConns,
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config()));

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);

            await pool.ReleaseAsync(mock.Object);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);
            mock.Verify(x => x.DestroyAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldCloseConnectionWhenConnectionIsOpenButNotResetable()
        {
            var mock = new Mock<IPooledConnection>();
            mock.Setup(x => x.IsOpen).Returns(true);
            mock.Setup(x => x.ClearConnectionAsync()).Returns(Task.FromException(new ClientException()));

            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            inUseConns.TryAdd(mock.Object);
            var pool = new ConnectionPool(
                null,
                null,
                inUseConns,
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config()));

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);

            await pool.ReleaseAsync(mock.Object);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);
            mock.Verify(x => x.DestroyAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldCloseConnectionWhenConnectionIsNotValid()
        {
            var mock = new Mock<IPooledConnection>();

            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            inUseConns.TryAdd(mock.Object);
            var pool = NewConnectionPool(inUseConnections: inUseConns, isConnectionValid: false);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);

            await pool.ReleaseAsync(mock.Object);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);
            mock.Verify(x => x.DestroyAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldCloseTheConnectionWhenConnectionIsReusableButThePoolIsFull()
        {
            var mock = new Mock<IPooledConnection>();

            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            inUseConns.TryAdd(mock.Object);

            var availableConns = new BlockingCollection<IPooledConnection>();
            var pooledConnMock = new Mock<IPooledConnection>();
            var config = new Config { MaxConnectionPoolSize = 10 };
            for (var i = 0; i < config.MaxIdleConnectionPoolSize; i++)
            {
                availableConns.Add(pooledConnMock.Object);
            }

            var pool = NewConnectionPool(
                availableConns,
                inUseConns,
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    config));

            pool.NumberOfIdleConnections.Should().Be(10);
            pool.NumberOfInUseConnections.Should().Be(1);

            await pool.ReleaseAsync(mock.Object);

            pool.NumberOfIdleConnections.Should().Be(10);
            pool.NumberOfInUseConnections.Should().Be(0);
            mock.Verify(x => x.DestroyAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldStartTimerBeforeReturnToPoolWhenIdleDetectionEnabled()
        {
            // Given
            var mock = new Mock<IPooledConnection>();
            mock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            mock.Setup(x => x.IsOpen).Returns(true);
            var timerMock = new Mock<ITimer>();
            mock.Setup(x => x.IdleTimer).Returns(timerMock.Object);

            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            inUseConns.TryAdd(mock.Object);
            var enableIdleTooLongTest = TimeSpan.FromMilliseconds(100);

            var pool = new ConnectionPool(
                null,
                null,
                inUseConns,
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config
                    {
                        MaxIdleConnectionPoolSize = 2,
                        ConnectionIdleTimeout = enableIdleTooLongTest
                    }));

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);

            //When
            await pool.ReleaseAsync(mock.Object);

            // Then
            pool.NumberOfIdleConnections.Should().Be(1);
            pool.NumberOfInUseConnections.Should().Be(0);

            timerMock.Verify(x => x.Start(), Times.Once);
        }

        [Fact]
        public async Task ShouldNotStartTimerBeforeReturnToPoolWhenIdleDetectionDisabled()
        {
            // Given
            var mock = new Mock<IPooledConnection>();
            mock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            mock.Setup(x => x.IsOpen).Returns(true);
            var timerMock = new Mock<ITimer>();
            mock.Setup(x => x.IdleTimer).Returns(timerMock.Object);

            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            inUseConns.TryAdd(mock.Object);
            // default pool setting have timer disabled
            var pool = new ConnectionPool(
                null,
                null,
                inUseConns,
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config()));

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);

            //When
            await pool.ReleaseAsync(mock.Object);

            // Then
            pool.NumberOfIdleConnections.Should().Be(1);
            pool.NumberOfInUseConnections.Should().Be(0);

            timerMock.Verify(x => x.Start(), Times.Never);
        }

        // thread safe test
        // Concurrent call of Release and Dispose
        [Fact]
        public async Task ShouldCloseConnectionIfPoolDisposeStarted()
        {
            // Given
            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            var pool = new ConnectionPool(
                new MockedConnectionFactory(),
                inUseConnections: inUseConns,
                driverContext: new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config()));

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);

            var mock = new Mock<IPooledConnection>();
            mock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            inUseConns.TryAdd(mock.Object);
            pool.NumberOfInUseConnections.Should().Be(1);

            // When
            // this is to simulate Release called first,
            // but before Release put a new conn into availConns, Dispose get called.
            // Note: Once dispose get called, it is forbidden to put anything into queue.
            mock.Setup(x => x.IsOpen)
                .Returns(true)
                .Callback(() => pool.CloseAsync().Wait()); // Simulate Dispose get called at this time

            await pool.ReleaseAsync(mock.Object);

            // Then
            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);
            mock.Verify(x => x.DestroyAsync(), Times.Once);
        }
    }

    public class CloseMethod
    {
        [Fact]
        public async Task ShouldReleaseAll()
        {
            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            var mock = new Mock<IPooledConnection>();
            inUseConns.TryAdd(mock.Object);

            var idleConns = new BlockingCollection<IPooledConnection>();
            var mock1 = new Mock<IPooledConnection>();
            idleConns.Add(mock1.Object);

            var pool = NewConnectionPool(idleConns, inUseConns);
            pool.NumberOfIdleConnections.Should().Be(1);
            pool.NumberOfInUseConnections.Should().Be(1);

            await pool.CloseAsync();

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);
            mock.Verify(x => x.DestroyAsync(), Times.Once);
            mock1.Verify(x => x.DestroyAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldLogInUseAndAvailableConnectionIds()
        {
            var mockLogger = LoggingHelper.GetTraceEnabledLogger();

            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            var mock = new Mock<IPooledConnection>();
            inUseConns.TryAdd(mock.Object);

            var availableConns = new BlockingCollection<IPooledConnection>();
            var mock1 = new Mock<IPooledConnection>();
            availableConns.Add(mock1.Object);

            var pool = new ConnectionPool(
                null,
                availableConns,
                inUseConns,
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config
                    {
                        Logger = mockLogger.Object
                    }),
                new TestConnectionValidator());

            await pool.CloseAsync();

            mockLogger.Verify(
                x => x.Info(It.Is<string>(actual => actual.Contains("Disposing In Use"))),
                Times.Once);

            mockLogger.Verify(
                x => x.Debug(It.Is<string>(actual => actual.Contains("Disposing Available"))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldReturnDirectlyWhenConnectionReleaseCalledAfterPoolDispose()
        {
            // Given
            var mock = new Mock<IPooledConnection>();
            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            inUseConns.TryAdd(mock.Object);
            var pool = NewConnectionPool(inUseConnections: inUseConns);

            // When
            await pool.CloseAsync();
            await pool.ReleaseAsync(mock.Object);

            // Then
            mock.Verify(x => x.DestroyAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldNotThrowExceptionWhenDisposedTwice()
        {
            // Given
            var mock = new Mock<IPooledConnection>();
            var inUseConns = new ConcurrentHashSet<IPooledConnection>();
            inUseConns.TryAdd(mock.Object);
            var pool = NewConnectionPool(inUseConnections: inUseConns);

            // When
            await pool.CloseAsync();
            await pool.CloseAsync();

            // Then
            mock.Verify(x => x.DestroyAsync(), Times.Once);
            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);
        }
    }

    public class NumberOfInUseConnections
    {
        [Fact]
        public void ShouldReturnZeroAfterCreation()
        {
            var uri = new Uri("bolt://localhost:7687");
            var context = new DriverContext(
                uri,
                AuthTokenManagers.None,
                new Config
                {
                    MaxIdleConnectionPoolSize = 1,
                    MaxConnectionPoolSize = 1,
                    ConnectionAcquisitionTimeout = Config.InfiniteInterval,
                    ConnectionIdleTimeout = Config.InfiniteInterval,
                    MaxConnectionLifetime = Config.InfiniteInterval
                });

            var connFactory = new MockedConnectionFactory();

            var pool = new ConnectionPool(connFactory, null, null, context);

            pool.NumberOfInUseConnections.Should().Be(0);
        }

        [Fact]
        public void ShouldReturnCorrectCountWhenOnlyInUseConnectionsPresent()
        {
            // pool has no idle connections
            var idleConnections = new BlockingCollection<IPooledConnection>();

            // pool has 3 in-use connections
            var inUseConnections = new ConcurrentHashSet<IPooledConnection>();
            inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);
            inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);
            inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);

            var pool = NewConnectionPool(idleConnections, inUseConnections);

            pool.NumberOfInUseConnections.Should().Be(3);
        }

        [Fact]
        public void ShouldReturnZeroWhenOnlyIdleConnectionsPresent()
        {
            // pool has 2 idle connections
            var idleConnections = new BlockingCollection<IPooledConnection>();
            idleConnections.TryAdd(new Mock<IPooledConnection>().Object);
            idleConnections.TryAdd(new Mock<IPooledConnection>().Object);

            // pool has no in-use connections
            var inUseConnections = new ConcurrentHashSet<IPooledConnection>();

            var pool = NewConnectionPool(idleConnections, inUseConnections);

            pool.NumberOfInUseConnections.Should().Be(0);
        }

        [Fact]
        public void ShouldReturnCorrectCountWhenBothIdleAndInUseConnectionsPresent()
        {
            // pool has 3 idle connections
            var idleConnections = new BlockingCollection<IPooledConnection>();
            idleConnections.TryAdd(new Mock<IPooledConnection>().Object);
            idleConnections.TryAdd(new Mock<IPooledConnection>().Object);
            idleConnections.TryAdd(new Mock<IPooledConnection>().Object);

            // pool has 2 in-use connections
            var inUseConnections = new ConcurrentHashSet<IPooledConnection>();
            inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);
            inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);

            var pool = NewConnectionPool(idleConnections, inUseConnections);

            pool.NumberOfInUseConnections.Should().Be(2);
        }
    }

    public class PoolSize
    {
        private static ConnectionPool CreatePool(IConnection conn, int maxIdlePoolSize, int maxPoolSize)
        {
            var connFactory = new MockedConnectionFactory(conn);

            var pool = new ConnectionPool(
                connFactory,
                driverContext: new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config
                    {
                        MaxIdleConnectionPoolSize = maxIdlePoolSize,
                        MaxConnectionPoolSize = maxPoolSize,
                        ConnectionAcquisitionTimeout = Config.InfiniteInterval,
                        ConnectionIdleTimeout = Config.InfiniteInterval,
                        MaxConnectionLifetime = Config.InfiniteInterval
                    }));

            return pool;
        }

        [Fact]
        public async Task ShouldReportCorrectPoolSizeWhenIdleConnectionsAreNotAllowed()
        {
            var connectionMock = new Mock<IConnection>();
            connectionMock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            connectionMock.Setup(x => x.IsOpen).Returns(true);

            var pool = CreatePool(connectionMock.Object, 0, 5);

            pool.PoolSize.Should().Be(0);

            var conn1 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.PoolSize.Should().Be(1);

            var conn2 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            var conn3 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            var conn4 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.PoolSize.Should().Be(4);

            await conn1.CloseAsync();
            pool.PoolSize.Should().Be(3);

            var conn5 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.PoolSize.Should().Be(4);

            await conn5.CloseAsync();
            await conn4.CloseAsync();
            await conn3.CloseAsync();
            await conn2.CloseAsync();

            pool.PoolSize.Should().Be(0);

            connectionMock.Verify(x => x.IsOpen, Times.Exactly(5 * 2)); // On Acquire and Release
        }

        [Fact]
        public async Task ShouldReportCorrectPoolSize()
        {
            var protocol = new Mock<IBoltProtocol>();
            var connectionMock = new Mock<IConnection>();
            connectionMock.Setup(x => x.IsOpen).Returns(true);
            connectionMock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            connectionMock.Setup(x => x.BoltProtocol).Returns(protocol.Object);

            var pool = CreatePool(connectionMock.Object, 5, 5);

            pool.PoolSize.Should().Be(0);

            var conn1 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.PoolSize.Should().Be(1);

            var conn2 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            var conn3 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            var conn4 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.PoolSize.Should().Be(4);

            await conn1.CloseAsync();
            pool.PoolSize.Should().Be(4);

            var conn5 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.PoolSize.Should().Be(4);

            await conn5.CloseAsync();
            await conn4.CloseAsync();
            await conn3.CloseAsync();
            await conn2.CloseAsync();

            pool.PoolSize.Should().Be(4);

            connectionMock.Verify(x => x.IsOpen, Times.Exactly(5 * 2)); // On Acquire and Release
        }

        [Fact]
        public async Task ShouldReportPoolSizeCorrectOnConcurrentRequests()
        {
            var connectionMock = new Mock<IConnection>();
            connectionMock.Setup(x => x.IsOpen).Returns(true);
            connectionMock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            var pool = CreatePool(connectionMock.Object, 5, 5);

            var rnd = new Random(Guid.NewGuid().GetHashCode());
            var acquireCounter = 0;
            var releaseCounter = 0;
            var stopMarker = 0;
            var waitedTime = 0;

            var acquireTasks = Enumerable.Range(0, 100)
                .Select(
                    _ => Task.Run(
                        async () =>
                        {
                            var conn = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
                            Interlocked.Increment(ref acquireCounter);

                            var wait = rnd.Next(1000);
                            Interlocked.Add(ref waitedTime, wait);
                            Thread.Sleep(wait);

                            await conn.CloseAsync();
                            Interlocked.Increment(ref releaseCounter);
                        }));

            var reportedSizes = new ConcurrentQueue<int>();
            var reportTask = Task.Run(
                () =>
                {
                    while (stopMarker == 0)
                    {
                        reportedSizes.Enqueue(pool.PoolSize);

                        Thread.Sleep(50);
                    }
                });

            await Task.WhenAll(acquireTasks);

            Interlocked.CompareExchange(ref stopMarker, 1, 0);

            reportTask.Wait();
            reportedSizes.Should().NotBeEmpty();
            reportedSizes.Should().NotContain(v => v < 0);
            reportedSizes.Should().NotContain(v => v > 5);
        }

        [Fact]
        public async void ShouldReportCorrectPoolSizeWhenIdleConnectionsAreNotAllowedAsync()
        {
            var connectionMock = new Mock<IConnection>();
            connectionMock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            connectionMock.Setup(x => x.IsOpen).Returns(true);

            var pool = CreatePool(connectionMock.Object, 0, 5);

            pool.PoolSize.Should().Be(0);

            var conn1 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.PoolSize.Should().Be(1);

            var conn2 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            var conn3 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            var conn4 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.PoolSize.Should().Be(4);

            await conn1.CloseAsync();
            pool.PoolSize.Should().Be(3);

            var conn5 = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.PoolSize.Should().Be(4);

            await conn5.CloseAsync();
            await conn4.CloseAsync();
            await conn3.CloseAsync();
            await conn2.CloseAsync();

            pool.PoolSize.Should().Be(0);

            connectionMock.Verify(x => x.IsOpen, Times.Exactly(5 * 2)); // On Acquire and Release
        }

        [Fact]
        public async void ShouldReportPoolSizeCorrectOnConcurrentRequestsAsync()
        {
            var connectionMock = new Mock<IConnection>();
            connectionMock.Setup(x => x.IsOpen).Returns(true);
            connectionMock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);
            var pool = CreatePool(connectionMock.Object, 5, 5);

            var rnd = new Random(Guid.NewGuid().GetHashCode());
            var acquireCounter = 0;
            var releaseCounter = 0;
            var stopMarker = 0;
            var waitedTime = 0;

            var acquireTasks = Enumerable.Range(0, 100)
                .Select(
                    _ => Task.Run(
                        async () =>
                        {
                            var conn = await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
                            Interlocked.Increment(ref acquireCounter);

                            var wait = rnd.Next(1000);
                            Interlocked.Add(ref waitedTime, wait);
                            await Task.Delay(wait);

                            await conn.CloseAsync();
                            Interlocked.Increment(ref releaseCounter);
                        }));

            var reportedSizes = new ConcurrentQueue<int>();
            var reportTask = Task.Run(
                () =>
                {
                    while (stopMarker == 0)
                    {
                        reportedSizes.Enqueue(pool.PoolSize);

                        Thread.Sleep(50);
                    }
                });

            var tasks = acquireTasks as Task[] ?? acquireTasks.ToArray();

            await Task.WhenAll(tasks).ContinueWith(_ => Interlocked.CompareExchange(ref stopMarker, 1, 0));

            await reportTask;

            reportedSizes.Should().NotBeEmpty();
            reportedSizes.Should().NotContain(v => v < 0);
            reportedSizes.Should().NotContain(v => v > 5);
        }
    }

    public class PoolState
    {
        // active
        [Fact]
        public async Task FromActiveViaAcquireToActive()
        {
            var pool = NewConnectionPool();
            await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);
            pool.Status.Should().Be(ConnectionPoolStatus.Active);
        }

        [Fact]
        public async Task FromActiveViaReleaseToActive()
        {
            var idleQueue = new BlockingCollection<IPooledConnection>();
            var inUseConnections = new ConcurrentHashSet<IPooledConnection>();
            var conn = new Mock<IPooledConnection>().Object;
            inUseConnections.TryAdd(conn);
            var pool = NewConnectionPool(idleQueue, inUseConnections);

            await pool.ReleaseAsync(conn);

            idleQueue.Count.Should().Be(1);
            inUseConnections.Count.Should().Be(0);
            pool.Status.Should().Be(ConnectionPoolStatus.Active);
        }

        [Fact]
        public async Task FromActiveViaCloseToClosed()
        {
            var pool = NewConnectionPool();
            await pool.CloseAsync();
            pool.Status.Should().Be(ConnectionPoolStatus.Closed);
        }

        [Fact]
        public void FromActiveViaActivateToActive()
        {
            var pool = NewConnectionPool();
            pool.Activate();
            pool.Status.Should().Be(ConnectionPoolStatus.Active);
        }

        [Fact]
        public async Task FromActiveViaDeactivateToInactive()
        {
            var pool = NewConnectionPool();
            await pool.DeactivateAsync();
            pool.Status.Should().Be(ConnectionPoolStatus.Inactive);
        }

        // inactive
        [Fact]
        public async Task FromInactiveViaAcquireThrowsError()
        {
            var pool = NewConnectionPool();
            pool.Status = ConnectionPoolStatus.Inactive;

            var exception =
                await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));

            exception.Should().BeOfType<ServiceUnavailableException>();
            pool.Status.Should().Be(ConnectionPoolStatus.Inactive);
        }

        [Fact]
        public async Task FromInactiveViaReleaseToInactive()
        {
            var idleQueue = new BlockingCollection<IPooledConnection>();
            var inUseConnections = new ConcurrentHashSet<IPooledConnection>();

            var conn = new Mock<IPooledConnection>().Object;
            inUseConnections.TryAdd(conn);

            var pool = NewConnectionPool(idleQueue, inUseConnections);
            pool.Status = ConnectionPoolStatus.Inactive;

            await pool.ReleaseAsync(conn);

            inUseConnections.Count.Should().Be(0);
            idleQueue.Count.Should().Be(0);
            pool.Status.Should().Be(ConnectionPoolStatus.Inactive);
        }

        [Fact]
        public async Task FromInactiveViaCloseToClosed()
        {
            var pool = NewConnectionPool();
            pool.Status = ConnectionPoolStatus.Inactive;

            await pool.CloseAsync();

            pool.Status.Should().Be(ConnectionPoolStatus.Closed);
        }

        [Fact]
        public void FromInactiveViaActivateToActive()
        {
            var pool = NewConnectionPool();
            pool.Status = ConnectionPoolStatus.Inactive;

            pool.Activate();

            pool.Status.Should().Be(ConnectionPoolStatus.Active);
        }

        [Fact]
        public async Task FromInactiveViaDeactivateToInactive()
        {
            var pool = NewConnectionPool();
            pool.Status = ConnectionPoolStatus.Inactive;

            await pool.DeactivateAsync();

            pool.Status.Should().Be(ConnectionPoolStatus.Inactive);
        }

        //closed
        [Fact]
        public async Task FromClosedViaAcquireThrowsError()
        {
            var pool = NewConnectionPool();
            pool.Status = ConnectionPoolStatus.Closed;

            var exception =
                await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));

            exception.Should().BeOfType<ObjectDisposedException>();
            pool.Status.Should().Be(ConnectionPoolStatus.Closed);
        }

        [Fact]
        public async Task FromClosedViaReleaseToClosed()
        {
            var idleQueue = new BlockingCollection<IPooledConnection>();
            var inUseConnections = new ConcurrentHashSet<IPooledConnection>();

            var conn = new Mock<IPooledConnection>().Object;
            inUseConnections.TryAdd(conn);

            var pool = NewConnectionPool(idleQueue, inUseConnections);
            pool.Status = ConnectionPoolStatus.Closed;

            await pool.ReleaseAsync(conn);

            inUseConnections.Count.Should().Be(1);
            idleQueue.Count.Should().Be(0);
            pool.Status.Should().Be(ConnectionPoolStatus.Closed);
        }

        [Fact]
        public async Task FromClosedViaCloseToClosed()
        {
            var pool = NewConnectionPool();
            pool.Status = ConnectionPoolStatus.Closed;

            await pool.CloseAsync();

            pool.Status.Should().Be(ConnectionPoolStatus.Closed);
        }

        [Fact]
        public void FromClosedViaActivateToClosed()
        {
            var pool = NewConnectionPool();
            pool.Status = ConnectionPoolStatus.Closed;

            pool.Activate();

            pool.Status.Should().Be(ConnectionPoolStatus.Closed);
        }

        [Fact]
        public async Task FromClosedViaDeactivateToClosed()
        {
            var pool = NewConnectionPool();
            pool.Status = ConnectionPoolStatus.Closed;

            await pool.DeactivateAsync();

            pool.Status.Should().Be(ConnectionPoolStatus.Closed);
        }
    }

    public class DeactivateMethod
    {
        private static List<Mock<IPooledConnection>> FillIdleConnections(
            BlockingCollection<IPooledConnection> idleConnections,
            int count)
        {
            var idleMocks = new List<Mock<IPooledConnection>>();
            for (var i = 0; i < count; i++)
            {
                var connMock = new Mock<IPooledConnection>();
                idleMocks.Add(connMock);
                idleConnections.Add(connMock.Object);
            }

            return idleMocks;
        }

        private static List<Mock<IPooledConnection>> FillInUseConnections(
            ConcurrentHashSet<IPooledConnection> inUseConnections,
            int count)
        {
            var inUseMocks = new List<Mock<IPooledConnection>>();
            for (var i = 0; i < count; i++)
            {
                var connMock = new Mock<IPooledConnection>();
                inUseMocks.Add(connMock);
                inUseConnections.TryAdd(connMock.Object);
            }

            return inUseMocks;
        }

        private static void VerifyDestroyAsyncCalledOnce(List<Mock<IPooledConnection>> mocks)
        {
            foreach (var conn in mocks)
            {
                conn.Verify(x => x.DestroyAsync(), Times.Once);
            }
        }

        [Fact]
        public async Task ShouldCloseAllIdleConnectionsAsync()
        {
            // Given
            var idleConnections = new BlockingCollection<IPooledConnection>();

            var idleMocks = FillIdleConnections(idleConnections, 10);

            var pool = NewConnectionPool(idleConnections);

            // When
            await pool.DeactivateAsync();

            // Then
            idleConnections.Count.Should().Be(0);
            VerifyDestroyAsyncCalledOnce(idleMocks);
        }

        // concurrent test
        // concurrently close and deactivate
        [Fact]
        public async Task DeactivateAndThenCloseShouldCloseAllConnections()
        {
            var idleConnections = new BlockingCollection<IPooledConnection>();
            var idleMocks = FillIdleConnections(idleConnections, 5);

            var inUseConnections = new ConcurrentHashSet<IPooledConnection>();
            var inUseMocks = FillInUseConnections(inUseConnections, 10);
            var pool = NewConnectionPool(idleConnections, inUseConnections);

            // When
            await pool.DeactivateAsync();
            // Then
            idleConnections.Count.Should().Be(0);
            inUseConnections.Count.Should().Be(10);
            VerifyDestroyAsyncCalledOnce(idleMocks);
            // refill the idle connections
            var newIdleMocks = FillIdleConnections(idleConnections, 5);
            idleConnections.Count.Should().Be(5);

            // When
            await pool.CloseAsync();
            // Then
            idleConnections.Count.Should().Be(0);
            inUseConnections.Count.Should().Be(0);

            VerifyDestroyAsyncCalledOnce(newIdleMocks);
            VerifyDestroyAsyncCalledOnce(inUseMocks);
        }

        // concurrent tests
        // ConcurrentlyAcquireAndDeactivate
        [Fact]
        public async Task ReturnConnectionIfAcquiredValidConnectionBeforeInactivation()
        {
            // Given
            var idleConnections = new BlockingCollection<IPooledConnection>();
            var openConnMock = new Mock<IPooledConnection>();
            openConnMock.Setup(x => x.LifetimeTimer).Returns(new StopwatchBasedTimer());
            openConnMock.Setup(x => x.IdleTimer).Returns(new StopwatchBasedTimer());
            var pool = NewConnectionPoolWithConnectionTimeoutCheckDisabled(idleConnections);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);

            // This is to simulate Acquire called first,
            // but before Acquire put a new conn into inUseConn, Deactivate get called.
            openConnMock.Setup(x => x.IsOpen)
                .Returns(true)
                .Callback(() => pool.DeactivateAsync().Wait());

            openConnMock.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);

            idleConnections.Add(openConnMock.Object);
            pool.NumberOfIdleConnections.Should().Be(1);
            // When
            await pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(1);
            openConnMock.Verify(x => x.IsOpen, Times.Once);
        }

        [Fact]
        public async Task ErrorIfAcquiredInvalidConnectionBeforeInactivation()
        {
            // Given
            var idleConnections = new BlockingCollection<IPooledConnection>();
            var closedConnMock = new Mock<IPooledConnection>();

            closedConnMock.Setup(x => x.IdleTimer).Returns(new StopwatchBasedTimer());
            var pool = NewConnectionPoolWithConnectionTimeoutCheckDisabled(idleConnections);

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);

            // This is to simulate Acquire called first,
            // but before Acquire put a new conn into inUseConn, Deactivate get called.
            // However here, this connection is not healthy and will be destroyed directly
            closedConnMock.Setup(x => x.IsOpen)
                .Returns(false)
                .Callback(() => pool.DeactivateAsync().Wait());

            idleConnections.Add(closedConnMock.Object);
            pool.NumberOfIdleConnections.Should().Be(1);
            // When
            var exception =
                await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read, null, null, Bookmarks.Empty));

            pool.NumberOfIdleConnections.Should().Be(0);
            pool.NumberOfInUseConnections.Should().Be(0);
            closedConnMock.Verify(x => x.IsOpen, Times.Once);
            closedConnMock.Verify(x => x.DestroyAsync(), Times.Once);
            exception.Should().BeOfType<ServiceUnavailableException>();
            exception.Message.Should().StartWith("Failed to acquire a connection");
        }

        // concurrent test
        // ConcurrentlyReleaseAndDeactivate
        [Fact]
        public async Task ShouldCloseConnectionReleasedDuringDeactivation()
        {
            // Given
            var idleConnections = new BlockingCollection<IPooledConnection>();
            var idleMocks = new List<Mock<IPooledConnection>>();
            idleMocks.AddRange(FillIdleConnections(idleConnections, 5));

            var specialConn = new Mock<IPooledConnection>();
            var releasedConn = new Mock<IPooledConnection>();
            specialConn.Setup(x => x.DestroyAsync())
                .Returns(Task.CompletedTask)
                .Callback(() => { idleConnections.Add(releasedConn.Object); });

            idleConnections.Add(specialConn.Object);
            idleMocks.Add(specialConn);
            idleMocks.Add(releasedConn);
            idleMocks.Count.Should().Be(5 + 2);

            var pool = NewConnectionPool(idleConnections);

            // When
            await pool.DeactivateAsync();

            // Then
            idleConnections.Count.Should().Be(0);
            VerifyDestroyAsyncCalledOnce(idleMocks);
        }

        // concurrent test
        // ConcurrentlyActivateAndDeactivate
        [Fact]
        public async Task ShouldCloseAllIdleConnectionsRegardlessActivateCalled()
        {
            // Given
            var idleConnections = new BlockingCollection<IPooledConnection>();
            var idleMocks = new List<Mock<IPooledConnection>>();
            idleMocks.AddRange(FillIdleConnections(idleConnections, 5));

            var specialConn = new Mock<IPooledConnection>();
            var pool = NewConnectionPool(idleConnections);
            specialConn.Setup(x => x.DestroyAsync())
                .Returns(Task.CompletedTask)
                .Callback(() => { pool.Activate(); });

            idleConnections.Add(specialConn.Object);
            idleMocks.Add(specialConn);

            idleMocks.AddRange(FillIdleConnections(idleConnections, 5));
            idleMocks.Count.Should().Be(5 + 1 + 5);

            // When
            await pool.DeactivateAsync();

            // Then
            idleConnections.Count.Should().Be(0);
            VerifyDestroyAsyncCalledOnce(idleMocks);
        }
    }

    private class TestConnectionValidator : IConnectionValidator
    {
        private readonly bool _isValid;

        public TestConnectionValidator(bool isValid = true)
        {
            _isValid = isValid;
        }

        public Task<bool> OnReleaseAsync(IPooledConnection connection)
        {
            return Task.FromResult(_isValid);
        }

        public AcquireStatus GetConnectionLifetimeStatus(IPooledConnection connection)
        {
            return _isValid ? AcquireStatus.Healthy : AcquireStatus.Unhealthy;
        }
    }

    private class MockedConnectionFactory : IPooledConnectionFactory
    {
        private readonly IConnection _connection;

        public MockedConnectionFactory(IConnection conn = null)
        {
            _connection = conn ?? new Mock<IConnection>().Object;
        }

        public IPooledConnection Create(
            Uri uri,
            IConnectionReleaseManager releaseManager,
            IAuthToken authToken)
        {
            var conn = new PooledConnection(_connection, releaseManager);
            conn.AuthorizationStatus = AuthorizationStatus.FreshlyAuthenticated;
            return conn;
        }
    }
}
