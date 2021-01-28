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
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Tests.TestUtil;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Tests
{
    public class ConnectionPoolTests
    {
        public class AcquireMethod
        {
            private readonly ITestOutputHelper _output;

            public AcquireMethod(ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public void ShouldCallConnInit()
            {
                // Given
                var mock = new Mock<IConnection>();
                var connFactory = new MockedConnectionFactory(mock.Object);
                var connectionPool = new ConnectionPool(connFactory, validator: new TestConnectionValidator());

                // When
                connectionPool.Acquire();

                //Then
                mock.Verify(x => x.Init(), Times.Once);
            }

            [Fact]
            public void ShouldBlockWhenMaxPoolSizeReached()
            {
                var connectionPoolSettings = new ConnectionPoolSettings(new Config {MaxConnectionPoolSize = 2});
                var pool = NewConnectionPool(poolSettings: connectionPoolSettings);
                var conn = pool.Acquire();
                pool.Acquire();
                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(2);

                var timer = new Stopwatch();
                var blockingAcquire = new Task(() => { pool.Acquire(); });
                var releaseConn = new Task(() => { conn.Close(); });

                timer.Start();
                blockingAcquire.Start();
                Task.Delay(1000).Wait(); // delay a bit here
                releaseConn.Start();

                releaseConn.Wait();
                blockingAcquire.Wait();
                timer.Stop();

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(2);
                timer.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(1, 0.1);
            }

            [Fact]
            public void ShouldThrowClientExceptionWhenFailedToAcquireWithinTimeout()
            {
                var connectionPoolSettings = new ConnectionPoolSettings(
                    new Config
                    {
                        MaxConnectionPoolSize = 2,
                        ConnectionAcquisitionTimeout = TimeSpan.FromMilliseconds(0)
                    });
                var pool = NewConnectionPool(poolSettings: connectionPoolSettings);
                pool.Acquire();
                pool.Acquire();
                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(2);

                var exception = Record.Exception(() => pool.Acquire());
                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().Contain("Failed to obtain a connection from pool within");
            }

            [Fact]
            public void ShouldNotExceedIdleLimit()
            {
                var connectionPoolSettings = new ConnectionPoolSettings(new Config {MaxIdleConnectionPoolSize = 2});
                var pool = NewConnectionPool(poolSettings: connectionPoolSettings);

                var conns = new List<IConnection>();
                for (var i = 0; i < 4; i++)
                {
                    conns.Add(pool.Acquire());
                    pool.NumberOfIdleConnections.Should().Be(0);
                }

                foreach (var conn in conns)
                {
                    conn.Close();
                    pool.NumberOfIdleConnections.Should().BeLessOrEqualTo(2);
                }

                pool.NumberOfIdleConnections.Should().Be(2);
            }

            [Fact]
            public void ShouldAcquireFromPoolIfAvailable()
            {
                var connectionPoolSettings = new ConnectionPoolSettings(new Config {MaxIdleConnectionPoolSize = 2});
                var pool = NewConnectionPool(poolSettings: connectionPoolSettings);

                for (var i = 0; i < 4; i++)
                {
                    var conn = pool.Acquire();
                    pool.NumberOfInUseConnections.Should().Be(1);
                    pool.NumberOfIdleConnections.Should().Be(0);
                    conn.Close();
                    pool.NumberOfIdleConnections.Should().Be(1);
                    pool.NumberOfInUseConnections.Should().Be(0);
                }

                pool.NumberOfIdleConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);
            }

            [Fact]
            public void ShouldCreateNewWhenQueueIsEmpty()
            {
                var pool = NewConnectionPool();

                pool.Acquire();
                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
            }

            [Fact]
            public void ShouldCloseConnectionIfFailedToCreate()
            {
                var connMock = new Mock<IPooledConnection>();
                connMock.Setup(x => x.Init()).Throws(new NotImplementedException());

                var connFactory = new MockedConnectionFactory(connMock.Object);
                var pool = new ConnectionPool(connFactory);

                Record.Exception(() => pool.Acquire());
                connMock.Verify(x => x.Destroy(), Times.Once);
                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
            }

            [Fact]
            public void ShouldCreateNewWhenQueueOnlyContainsClosedConnections()
            {
                var conns = new BlockingCollection<IPooledConnection>();
                var closedMock = new Mock<IPooledConnection>();
                closedMock.Setup(x => x.IsOpen).Returns(false);

                conns.Add(closedMock.Object);
                var pool = new ConnectionPool(ReusableConnectionFactory, conns);

                pool.NumberOfIdleConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                var conn = pool.Acquire();

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
                closedMock.Verify(x => x.IsOpen, Times.Once);
                closedMock.Verify(x => x.Destroy(), Times.Once);

                conn.Should().NotBeNull();
                conn.Should().NotBe(closedMock.Object);
            }

            [Fact]
            public void ShouldReuseWhenOpenConnectionInQueue()
            {
                var conns = new BlockingCollection<IPooledConnection>();
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                mock.Setup(x => x.LifetimeTimer).Returns(MockedTimer);

                conns.Add(mock.Object);
                var pool = new ConnectionPool(new MockedConnectionFactory(), conns);

                pool.NumberOfIdleConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                var conn = pool.Acquire();

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
                mock.Verify(x => x.IsOpen, Times.Once);
                conn.Should().Be(mock.Object);
            }

            [Fact]
            public void ShouldReuseOpenConnectionWhenOpenAndClosedConnectionsInQueue()
            {
                var conns = new BlockingCollection<IPooledConnection>();
                var healthyMock = new Mock<IPooledConnection>();
                healthyMock.Setup(x => x.IsOpen).Returns(true);
                healthyMock.Setup(x => x.LifetimeTimer).Returns(MockedTimer);
                var unhealthyMock = new Mock<IPooledConnection>();
                unhealthyMock.Setup(x => x.IsOpen).Returns(false);

                conns.Add(unhealthyMock.Object);
                conns.Add(healthyMock.Object);
                var pool = new ConnectionPool(new MockedConnectionFactory(), conns);

                pool.NumberOfIdleConnections.Should().Be(2);
                pool.NumberOfInUseConnections.Should().Be(0);

                var conn = pool.Acquire();

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
                unhealthyMock.Verify(x => x.Destroy(), Times.Once);
                healthyMock.Verify(x => x.Destroy(), Times.Never);
                conn.Should().Be(healthyMock.Object);
            }

            [Fact]
            public void ShouldCloseIdleTooLongConn()
            {
                // Given
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                var timerMock = new Mock<ITimer>();
                timerMock.Setup(x => x.ElapsedMilliseconds).Returns(1000);
                mock.Setup(x => x.IdleTimer).Returns(timerMock.Object);
                var idleTooLongId = "Molly";
                mock.Setup(x => x.ToString()).Returns(idleTooLongId);

                var conns = new BlockingCollection<IPooledConnection>();
                conns.Add(mock.Object);
                var enableIdleTooLongTest = TimeSpan.FromMilliseconds(100);
                var poolSettings = new ConnectionPoolSettings(
                    new Config {MaxIdleConnectionPoolSize = 2, ConnectionIdleTimeout = enableIdleTooLongTest});
                var pool = new ConnectionPool(ReusableConnectionFactory, conns, poolSettings: poolSettings);

                pool.NumberOfIdleConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                // When
                var conn = pool.Acquire();

                // Then
                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
                mock.Verify(x => x.Destroy(), Times.Once);

                conn.Should().NotBeNull();
                conn.Should().NotBe(idleTooLongId);
            }

            [Fact]
            public void ShouldReuseIdleNotTooLongConn()
            {
                // Given
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                var timerMock = new Mock<ITimer>();
                timerMock.Setup(x => x.ElapsedMilliseconds).Returns(10);
                mock.Setup(x => x.IdleTimer).Returns(timerMock.Object);
                var idleTooLongId = "Molly";
                mock.Setup(x => x.ToString()).Returns(idleTooLongId);

                var conns = new BlockingCollection<IPooledConnection>();
                conns.Add(mock.Object);
                var enableIdleTooLongTest = TimeSpan.FromMilliseconds(100);
                var poolSettings = new ConnectionPoolSettings(
                    new Config
                    {
                        MaxIdleConnectionPoolSize = 2,
                        ConnectionIdleTimeout = enableIdleTooLongTest,
                        MaxConnectionLifetime = Config.InfiniteInterval, // disable life time check
                    });
                var pool = new ConnectionPool(ReusableConnectionFactory, conns, poolSettings: poolSettings);

                pool.NumberOfIdleConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                // When
                var conn = pool.Acquire();

                // Then
                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                conn.Should().Be(mock.Object);
                conn.ToString().Should().Be(idleTooLongId);
            }

            [Theory]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(5)]
            [InlineData(10)]
            [InlineData(500)]
            public void ShouldAcquireNewWhenBeingUsedConcurrentlyBy(int numberOfThreads)
            {
                var ids = new List<string>();
                for (var i = 0; i < numberOfThreads; i++)
                {
                    ids.Add($"{i}");
                }

                var mockConns = new Queue<Mock<IPooledConnection>>();
                var conns = new BlockingCollection<IPooledConnection>();
                for (var i = 0; i < numberOfThreads; i++)
                {
                    var mock = new Mock<IPooledConnection>();
                    mock.Setup(x => x.IsOpen).Returns(true);
                    mock.Setup(x => x.ToString()).Returns(ids[i]);
                    conns.Add(mock.Object);
                    mockConns.Enqueue(mock);
                }

                var pool = NewConnectionPoolWithConnectionTimeoutCheckDisabled(conns);

                pool.NumberOfIdleConnections.Should().Be(numberOfThreads);
                pool.NumberOfInUseConnections.Should().Be(0);

                var receivedIds = new List<string>();

                var tasks = new Task[numberOfThreads];
                for (var i = 0; i < numberOfThreads; i++)
                {
                    var localI = i;
                    tasks[localI] =
                        Task.Run(() =>
                        {
                            try
                            {
                                Task.Delay(500);
                                var conn = pool.Acquire();
                                lock (receivedIds)
                                    receivedIds.Add(conn.ToString());
                            }
                            catch (Exception ex)
                            {
                                _output.WriteLine($"Task[{localI}] died: {ex}");
                            }
                        });
                }

                Task.WaitAll(tasks);

                receivedIds.Count.Should().Be(numberOfThreads);
                foreach (var receivedId in receivedIds)
                {
                    receivedIds.Should().ContainSingle(x => x == receivedId);
                    ids.Should().Contain(receivedId);
                }

                foreach (var mock in mockConns)
                {
                    mock.Verify(x => x.IsOpen, Times.Once);
                }
            }

            // thread-safe test
            // concurrent call of Acquire and Dispose
            [Fact]
            public void ShouldThrowExceptionWhenAcquireCalledAfterDispose()
            {
                var pool = NewConnectionPool();

                pool.Dispose();
                var exception = Record.Exception(() => pool.Acquire());
                exception.Should().BeOfType<ObjectDisposedException>();
                exception.Message.Should().StartWith("Failed to acquire a new connection");
            }

            // thread-safe test
            // concurrent call of Acquire and Dispose
            [Fact]
            public void ShouldCloseAcquiredConnectionIfPoolDisposeStarted()
            {
                // Given
                var idleConnections = new BlockingCollection<IPooledConnection>();
                var healthyMock = new Mock<IPooledConnection>();
                var pool = NewConnectionPoolWithConnectionTimeoutCheckDisabled(idleConnections);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);

                // This is to simulate Acquire called first,
                // but before Acquire put a new conn into inUseConn, Dispose get called.
                // Note: Once dispose get called, it is forbidden to put anything into queue.
                healthyMock.Setup(x => x.IsOpen).Returns(true)
                    .Callback(() => pool.Close()); // Simulate Dispose get called at this time
                idleConnections.Add(healthyMock.Object);
                pool.NumberOfIdleConnections.Should().Be(1);
                // When
                var exception = Record.Exception(() => pool.Acquire());

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                healthyMock.Verify(x => x.IsOpen, Times.Once);
                healthyMock.Verify(x => x.Destroy(), Times.Once);
                exception.Should().BeOfType<ObjectDisposedException>();
                exception.Message.Should().StartWith("Failed to acquire a new connection");
            }

            [Fact]
            public void ShouldTimeoutAfterAcquireTimeoutIfPoolIsFull()
            {
                Config config = Config.Builder.WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(10))
                    .WithMaxConnectionPoolSize(5).WithMaxIdleConnectionPoolSize(0).ToConfig();

                var pool = NewConnectionPool(poolSettings: new ConnectionPoolSettings(config));

                for (var i = 0; i < config.MaxConnectionPoolSize; i++)
                {
                    pool.Acquire();
                }

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var exception = Record.Exception(() => pool.Acquire());

                stopWatch.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(10, 0.1);
                exception.Message.Should().StartWith("Failed to obtain a connection from pool within");
                exception.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldTimeoutAfterAcquireTimeoutWhenConnectionIsNotValidated()
            {
                Config config = Config.Builder.WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(5))
                    .ToConfig();

                var closedConnectionMock = new Mock<IPooledConnection>();
                closedConnectionMock.Setup(x => x.IsOpen).Returns(false);

                var pool = NewConnectionPool(poolSettings: new ConnectionPoolSettings(config),
                    isConnectionValid: false);

                var exception = Record.Exception(() => pool.Acquire());

                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("Failed to obtain a connection from pool within");
            }

            [Fact]
            public async void ShouldTimeoutAfterAcquireAsyncTimeoutIfPoolIsFull()
            {
                Config config = Config.Builder.WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(10))
                    .WithMaxConnectionPoolSize(5).WithMaxIdleConnectionPoolSize(0).ToConfig();

                var pool = NewConnectionPool(poolSettings: new ConnectionPoolSettings(config));

                for (var i = 0; i < config.MaxConnectionPoolSize; i++)
                {
                    pool.Acquire();
                }

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var exception = await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read));

                stopWatch.Stop();
                stopWatch.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(10, 0.1);
                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("Failed to obtain a connection from pool within");
            }

            [Fact]
            public async void ShouldTimeoutAfterAcquireAsyncTimeoutWhenConnectionIsNotValidated()
            {
                Config config = Config.Builder.WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(5))
                    .ToConfig();


                var pool = NewConnectionPool(poolSettings: new ConnectionPoolSettings(config),
                    isConnectionValid: false);

                var exception = await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read));

                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("Failed to obtain a connection from pool within");
            }

            [Theory]
            [InlineData(AccessMode.Read)]
            [InlineData(AccessMode.Write)]
            public void ShouldManageModeProperty(AccessMode mode)
            {
                var connection = new Mock<IPooledConnection>().SetupProperty(x => x.Mode);
                var idleConnections = new BlockingCollection<IPooledConnection>() { connection.Object };

                var pool = NewConnectionPool(idleConnections);

                var acquired = pool.Acquire(mode);
                acquired.Mode.Should().Be(mode);

                pool.Release((IPooledConnection)acquired);
                acquired.Mode.Should().BeNull();

                connection.VerifySet(x => x.Mode = mode);
                connection.VerifySet(x => x.Mode = null);
            }

            [Theory]
            [InlineData(AccessMode.Read)]
            [InlineData(AccessMode.Write)]
            public async Task ShouldManageModePropertyAsync(AccessMode mode)
            {
                var connection = new Mock<IPooledConnection>().SetupProperty(x => x.Mode);
                var idleConnections = new BlockingCollection<IPooledConnection>() { connection.Object };

                var pool = NewConnectionPool(idleConnections);

                var acquired = await pool.AcquireAsync(mode);
                acquired.Mode.Should().Be(mode);

                await pool.ReleaseAsync((IPooledConnection)acquired);
                acquired.Mode.Should().BeNull();

                connection.VerifySet(x => x.Mode = mode);
                connection.VerifySet(x => x.Mode = null);
            }
        }

        public class ReleaseMethod
        {
            [Fact]
            public void ShouldReturnToPoolWhenConnectionIsReusableAndPoolIsNotFull()
            {
                var conn = new Mock<IPooledConnection>().Object;

                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(conn);
                var pool = NewConnectionPool(inUseConnections: inUseConns);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(conn);

                pool.NumberOfIdleConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);
            }

            [Fact]
            public void ShouldCloseConnectionWhenConnectionIsClosed()
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(false);

                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                var pool = new ConnectionPool(null, null, inUseConns);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(mock.Object);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Destroy(), Times.Once);
            }

            [Fact]
            public void ShouldCloseConnectionWhenConnectionIsOpenButNotResetable()
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                mock.Setup(x => x.ClearConnection()).Throws<ClientException>();

                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                var pool = new ConnectionPool(null, null, inUseConns);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(mock.Object);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Destroy(), Times.Once);
            }

            [Fact]
            public void ShouldCloseConnectionWhenConnectionIsNotValid()
            {
                var mock = new Mock<IPooledConnection>();

                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                var pool = NewConnectionPool(inUseConnections: inUseConns, isConnectionValid: false);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(mock.Object);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Destroy(), Times.Once);
            }

            [Fact]
            public void ShouldCloseTheConnectionWhenConnectionIsReusableButThePoolIsFull()
            {
                var mock = new Mock<IPooledConnection>();

                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);

                var availableConns = new BlockingCollection<IPooledConnection>();
                var pooledConnMock = new Mock<IPooledConnection>();
                var poolSettings = new ConnectionPoolSettings(
                    new Config {MaxConnectionPoolSize = 10});

                for (int i = 0; i < poolSettings.MaxIdleConnectionPoolSize; i++)
                {
                    availableConns.Add(pooledConnMock.Object);
                }

                var pool = NewConnectionPool(availableConns, inUseConns, poolSettings);

                pool.NumberOfIdleConnections.Should().Be(10);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(mock.Object);

                pool.NumberOfIdleConnections.Should().Be(10);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Destroy(), Times.Once);
            }

            [Fact]
            public void ShouldStartTimerBeforeReturnToPoolWhenIdleDetectionEnabled()
            {
                // Given
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                var timerMock = new Mock<ITimer>();
                mock.Setup(x => x.IdleTimer).Returns(timerMock.Object);

                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                var enableIdleTooLongTest = TimeSpan.FromMilliseconds(100);
                var poolSettings = new ConnectionPoolSettings(
                    new Config {MaxIdleConnectionPoolSize = 2, ConnectionIdleTimeout = enableIdleTooLongTest});
                ;
                var pool = new ConnectionPool(null, null, inUseConns, poolSettings: poolSettings);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                //When
                pool.Release(mock.Object);

                // Then
                pool.NumberOfIdleConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                timerMock.Verify(x => x.Start(), Times.Once);
            }

            [Fact]
            public void ShouldNotStartTimerBeforeReturnToPoolWhenIdleDetectionDisabled()
            {
                // Given
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                var timerMock = new Mock<ITimer>();
                mock.Setup(x => x.IdleTimer).Returns(timerMock.Object);

                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                // default pool setting have timer disabled
                var pool = new ConnectionPool(null, null, inUseConns);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                //When
                pool.Release(mock.Object);

                // Then
                pool.NumberOfIdleConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                timerMock.Verify(x => x.Start(), Times.Never);
            }

            // thread safe test
            // Concurrent call of Release and Dispose
            [Fact]
            public void ShouldCloseConnectionIfPoolDisposeStarted()
            {
                // Given
                var inUseConns = new ConcurrentSet<IPooledConnection>();
                var pool = new ConnectionPool(null, null, inUseConns);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);

                var mock = new Mock<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                pool.NumberOfInUseConnections.Should().Be(1);

                // When
                // this is to simulate Release called first,
                // but before Release put a new conn into availConns, Dispose get called.
                // Note: Once dispose get called, it is forbidden to put anything into queue.
                mock.Setup(x => x.IsOpen).Returns(true)
                    .Callback(() => pool.Close()); // Simulate Dispose get called at this time
                pool.Release(mock.Object);

                // Then
                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Destroy(), Times.Once);
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldReleaseAll()
            {
                var inUseConns = new ConcurrentSet<IPooledConnection>();
                var mock = new Mock<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);

                var idleConns = new BlockingCollection<IPooledConnection>();
                var mock1 = new Mock<IPooledConnection>();
                idleConns.Add(mock1.Object);

                var pool = NewConnectionPool(idleConns, inUseConns);
                pool.NumberOfIdleConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Dispose();

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Destroy(), Times.Once);
                mock1.Verify(x => x.Destroy(), Times.Once);
            }

            [Fact]
            public void ShouldLogInUseAndAvailableConnectionIds()
            {
                var mockLogger = LoggingHelper.GetTraceEnabledLogger();

                var inUseConns = new ConcurrentSet<IPooledConnection>();
                var mock = new Mock<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);

                var availableConns = new BlockingCollection<IPooledConnection>();
                var mock1 = new Mock<IPooledConnection>();
                availableConns.Add(mock1.Object);

                var pool = new ConnectionPool(null, availableConns, inUseConns,
                    validator: new TestConnectionValidator(), logger: mockLogger.Object);

                pool.Dispose();

                mockLogger.Verify(x => x.Info(It.Is<string>(actual => actual.Contains("Disposing In Use"))),
                    Times.Once);
                mockLogger.Verify(x => x.Debug(It.Is<string>(actual => actual.Contains("Disposing Available"))),
                    Times.Once);
            }

            [Fact]
            public void ShouldReturnDirectlyWhenConnectionReleaseCalledAfterPoolDispose()
            {
                // Given
                var mock = new Mock<IPooledConnection>();
                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                var pool = NewConnectionPool(inUseConnections: inUseConns);

                // When
                pool.Dispose();
                pool.Release(mock.Object);

                // Then
                mock.Verify(x => x.Destroy(), Times.Once);
            }

            [Fact]
            public void ShouldNotThrowExceptionWhenDisposedTwice()
            {
                // Given
                var mock = new Mock<IPooledConnection>();
                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                var pool = NewConnectionPool(inUseConnections: inUseConns);

                // When
                pool.Dispose();
                pool.Dispose();

                // Then
                mock.Verify(x => x.Destroy(), Times.Once);
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
                var poolSettings = new ConnectionPoolSettings(1, 1, Config.InfiniteInterval, Config.InfiniteInterval,
                    Config.InfiniteInterval);
                var logger = new Mock<IDriverLogger>().Object;
                var connFactory = new MockedConnectionFactory();

                var pool = new ConnectionPool(uri, connFactory, poolSettings, logger);

                pool.NumberOfInUseConnections.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnCorrectCountWhenOnlyInUseConnectionsPresent()
            {
                // pool has no idle connections
                var idleConnections = new BlockingCollection<IPooledConnection>();

                // pool has 3 in-use connections
                var inUseConnections = new ConcurrentSet<IPooledConnection>();
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
                var inUseConnections = new ConcurrentSet<IPooledConnection>();
                var logger = new Mock<ILogger>().Object;

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
                var inUseConnections = new ConcurrentSet<IPooledConnection>();
                inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);
                inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);

                var logger = new Mock<ILogger>().Object;

                var pool = NewConnectionPool(idleConnections, inUseConnections);

                pool.NumberOfInUseConnections.Should().Be(2);
            }
        }

        public class PoolSize
        {
            private static ConnectionPool CreatePool(IConnection conn, int maxIdlePoolSize, int maxPoolSize)
            {
                var poolSettings = new ConnectionPoolSettings(maxIdlePoolSize, maxPoolSize, Config.InfiniteInterval,
                    Config.InfiniteInterval, Config.InfiniteInterval);
                var connFactory = new MockedConnectionFactory(conn);

                var pool = new ConnectionPool(connFactory, poolSettings: poolSettings);

                return pool;
            }

            [Fact]
            public void ShouldReportCorrectPoolSizeWhenIdleConnectionsAreNotAllowed()
            {
                var connectionMock = new Mock<IConnection>();
                connectionMock.Setup(x => x.IsOpen).Returns(true);

                var pool = CreatePool(connectionMock.Object, 0, 5);

                pool.PoolSize.Should().Be(0);

                var conn1 = pool.Acquire();
                pool.PoolSize.Should().Be(1);

                var conn2 = pool.Acquire();
                var conn3 = pool.Acquire();
                var conn4 = pool.Acquire();
                pool.PoolSize.Should().Be(4);

                conn1.Close();
                pool.PoolSize.Should().Be(3);

                var conn5 = pool.Acquire();
                pool.PoolSize.Should().Be(4);

                conn5.Close();
                conn4.Close();
                conn3.Close();
                conn2.Close();

                pool.PoolSize.Should().Be(0);

                connectionMock.Verify(x => x.IsOpen, Times.Exactly(5 * 2)); // On Acquire and Release
            }

            [Fact]
            public void ShouldReportCorrectPoolSize()
            {
                var connectionMock = new Mock<IConnection>();
                connectionMock.Setup(x => x.IsOpen).Returns(true);

                var pool = CreatePool(connectionMock.Object, 5, 5);

                pool.PoolSize.Should().Be(0);

                var conn1 = pool.Acquire();
                pool.PoolSize.Should().Be(1);

                var conn2 = pool.Acquire();
                var conn3 = pool.Acquire();
                var conn4 = pool.Acquire();
                pool.PoolSize.Should().Be(4);

                conn1.Close();
                pool.PoolSize.Should().Be(4);

                var conn5 = pool.Acquire();
                pool.PoolSize.Should().Be(4);

                conn5.Close();
                conn4.Close();
                conn3.Close();
                conn2.Close();

                pool.PoolSize.Should().Be(4);

                connectionMock.Verify(x => x.IsOpen, Times.Exactly(5 * 2)); // On Acquire and Release
            }

            [Fact]
            public void ShouldReportPoolSizeCorrectOnConcurrentRequests()
            {
                var connectionMock = new Mock<IConnection>();
                connectionMock.Setup(x => x.IsOpen).Returns(true);
                var pool = CreatePool(connectionMock.Object, 5, 5);

                var rnd = new Random(Guid.NewGuid().GetHashCode());
                var acquireCounter = 0;
                var releaseCounter = 0;
                var stopMarker = 0;
                var waitedTime = 0;

                var acquireTasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
                {
                    var conn = pool.Acquire();
                    Interlocked.Increment(ref acquireCounter);

                    var wait = rnd.Next(1000);
                    Interlocked.Add(ref waitedTime, wait);
                    Thread.Sleep(wait);

                    conn.Close();
                    Interlocked.Increment(ref releaseCounter);
                }));

                var reportedSizes = new ConcurrentQueue<int>();
                var reportTask = Task.Run(() =>
                {
                    while (stopMarker == 0)
                    {
                        reportedSizes.Enqueue(pool.PoolSize);

                        Thread.Sleep(50);
                    }
                });

                var tasks = acquireTasks as Task[] ?? acquireTasks.ToArray();

                Task.WhenAll(tasks).ContinueWith(t => Interlocked.CompareExchange(ref stopMarker, 1, 0)).Wait();

                reportTask.Wait();
                reportedSizes.Should().NotBeEmpty();
                reportedSizes.Should().NotContain(v => v < 0);
                reportedSizes.Should().NotContain(v => v > 5);
            }

            [Fact]
            public async void ShouldReportCorrectPoolSizeAsync()
            {
                var connectionMock = new Mock<IConnection>();
                connectionMock.Setup(x => x.IsOpen).Returns(true);

                var pool = CreatePool(connectionMock.Object, 5, 5);

                pool.PoolSize.Should().Be(0);

                var conn1 = await pool.AcquireAsync(AccessMode.Read);
                pool.PoolSize.Should().Be(1);

                var conn2 = await pool.AcquireAsync(AccessMode.Read);
                var conn3 = await pool.AcquireAsync(AccessMode.Read);
                var conn4 = await pool.AcquireAsync(AccessMode.Read);
                pool.PoolSize.Should().Be(4);

                await conn1.CloseAsync();
                pool.PoolSize.Should().Be(4);

                var conn5 = await pool.AcquireAsync(AccessMode.Read);
                pool.PoolSize.Should().Be(4);

                await conn5.CloseAsync();
                await conn4.CloseAsync();
                await conn3.CloseAsync();
                await conn2.CloseAsync();

                pool.PoolSize.Should().Be(4);

                connectionMock.Verify(x => x.IsOpen, Times.Exactly(5 * 2)); // On Acquire and Release
            }

            [Fact]
            public async void ShouldReportCorrectPoolSizeWhenIdleConnectionsAreNotAllowedAsync()
            {
                var connectionMock = new Mock<IConnection>();
                connectionMock.Setup(x => x.IsOpen).Returns(true);

                var pool = CreatePool(connectionMock.Object, 0, 5);

                pool.PoolSize.Should().Be(0);

                var conn1 = await pool.AcquireAsync(AccessMode.Read);
                pool.PoolSize.Should().Be(1);

                var conn2 = await pool.AcquireAsync(AccessMode.Read);
                var conn3 = await pool.AcquireAsync(AccessMode.Read);
                var conn4 = await pool.AcquireAsync(AccessMode.Read);
                pool.PoolSize.Should().Be(4);

                await conn1.CloseAsync();
                pool.PoolSize.Should().Be(3);

                var conn5 = await pool.AcquireAsync(AccessMode.Read);
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
                var pool = CreatePool(connectionMock.Object, 5, 5);

                var rnd = new Random(Guid.NewGuid().GetHashCode());
                var acquireCounter = 0;
                var releaseCounter = 0;
                var stopMarker = 0;
                var waitedTime = 0;

                var acquireTasks = Enumerable.Range(0, 100).Select(i => Task.Run(async () =>
                {
                    var conn = await pool.AcquireAsync(AccessMode.Read);
                    Interlocked.Increment(ref acquireCounter);

                    var wait = rnd.Next(1000);
                    Interlocked.Add(ref waitedTime, wait);
                    await Task.Delay(wait);

                    await conn.CloseAsync();
                    Interlocked.Increment(ref releaseCounter);
                }));

                var reportedSizes = new ConcurrentQueue<int>();
                var reportTask = Task.Run(() =>
                {
                    while (stopMarker == 0)
                    {
                        reportedSizes.Enqueue(pool.PoolSize);

                        Thread.Sleep(50);
                    }
                });

                var tasks = acquireTasks as Task[] ?? acquireTasks.ToArray();

                await Task.WhenAll(tasks).ContinueWith(t => Interlocked.CompareExchange(ref stopMarker, 1, 0));

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
            public void FromActiveViaAcquireToActive()
            {
                var pool = NewConnectionPool();
                pool.Acquire();
                pool.Status.Should().Be(ConnectionPoolStatus.Active);
            }

            [Fact]
            public void FromActiveViaReleaseToActive()
            {
                var idleQueue = new BlockingCollection<IPooledConnection>();
                var inUseConnections = new ConcurrentSet<IPooledConnection>();
                var conn = new Mock<IPooledConnection>().Object;
                inUseConnections.TryAdd(conn);
                var pool = NewConnectionPool(idleQueue, inUseConnections);

                pool.Release(conn);

                idleQueue.Count.Should().Be(1);
                inUseConnections.Count.Should().Be(0);
                pool.Status.Should().Be(ConnectionPoolStatus.Active);
            }

            [Fact]
            public void FromActiveViaDisposeToClosed()
            {
                var pool = NewConnectionPool();
                pool.Dispose();
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
            public void FromActiveViaDeactivateToInactive()
            {
                var pool = NewConnectionPool();
                pool.Deactivate();
                pool.Status.Should().Be(ConnectionPoolStatus.Inactive);
            }

            // inactive
            [Fact]
            public void FromInactiveViaAcquireThrowsError()
            {
                var pool = NewConnectionPool();
                pool.Status = ConnectionPoolStatus.Inactive;

                var exception = Record.Exception(() => pool.Acquire());

                exception.Should().BeOfType<ClientException>();
                pool.Status.Should().Be(ConnectionPoolStatus.Inactive);
            }

            [Fact]
            public void FromInactiveViaReleaseToInactive()
            {
                var idleQueue = new BlockingCollection<IPooledConnection>();
                var inUseConnections = new ConcurrentSet<IPooledConnection>();

                var conn = new Mock<IPooledConnection>().Object;
                inUseConnections.TryAdd(conn);

                var pool = NewConnectionPool(idleQueue, inUseConnections);
                pool.Status = ConnectionPoolStatus.Inactive;

                pool.Release(conn);

                inUseConnections.Count.Should().Be(0);
                idleQueue.Count.Should().Be(0);
                pool.Status.Should().Be(ConnectionPoolStatus.Inactive);
            }

            [Fact]
            public void FromInactiveViaDisposeToClosed()
            {
                var pool = NewConnectionPool();
                pool.Status = ConnectionPoolStatus.Inactive;

                pool.Dispose();

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
            public void FromInactiveViaDeactivateToInactive()
            {
                var pool = NewConnectionPool();
                pool.Status = ConnectionPoolStatus.Inactive;

                pool.Deactivate();

                pool.Status.Should().Be(ConnectionPoolStatus.Inactive);
            }

            //closed
            [Fact]
            public void FromClosedViaAcquireThrowsError()
            {
                var pool = NewConnectionPool();
                pool.Status = ConnectionPoolStatus.Closed;

                var exception = Record.Exception(() => pool.Acquire());

                exception.Should().BeOfType<ObjectDisposedException>();
                pool.Status.Should().Be(ConnectionPoolStatus.Closed);
            }

            [Fact]
            public void FromClosedViaReleaseToClosed()
            {
                var idleQueue = new BlockingCollection<IPooledConnection>();
                var inUseConnections = new ConcurrentSet<IPooledConnection>();

                var conn = new Mock<IPooledConnection>().Object;
                inUseConnections.TryAdd(conn);

                var pool = NewConnectionPool(idleQueue, inUseConnections);
                pool.Status = ConnectionPoolStatus.Closed;

                pool.Release(conn);

                inUseConnections.Count.Should().Be(1);
                idleQueue.Count.Should().Be(0);
                pool.Status.Should().Be(ConnectionPoolStatus.Closed);
            }

            [Fact]
            public void FromClosedViaDisposeToClosed()
            {
                var pool = NewConnectionPool();
                pool.Status = ConnectionPoolStatus.Closed;

                pool.Dispose();

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
            public void FromClosedViaDeactivateToClosed()
            {
                var pool = NewConnectionPool();
                pool.Status = ConnectionPoolStatus.Closed;

                pool.Deactivate();

                pool.Status.Should().Be(ConnectionPoolStatus.Closed);
            }
        }

        public class DeactivateMethod
        {
            private static List<Mock<IPooledConnection>> FillIdleConnections(
                BlockingCollection<IPooledConnection> idleConnections, int count)
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
                ConcurrentSet<IPooledConnection> inUseConnections, int count)
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

            private static void VerifyDestroyCalledOnce(List<Mock<IPooledConnection>> mocks)
            {
                foreach (var conn in mocks)
                {
                    conn.Verify(x => x.Destroy(), Times.Once);
                }
            }

            private static void VerifyDestroyAsyncCalledOnce(List<Mock<IPooledConnection>> mocks)
            {
                foreach (var conn in mocks)
                {
                    conn.Verify(x => x.DestroyAsync(), Times.Once);
                }
            }

            [Fact]
            public void ShouldCloseAllIdleConnections()
            {
                // Given
                var idleConnections = new BlockingCollection<IPooledConnection>();
                var idleMocks = FillIdleConnections(idleConnections, 10);
                var pool = NewConnectionPool(idleConnections);

                // When
                pool.Deactivate();

                // Then
                idleConnections.Count.Should().Be(0);
                VerifyDestroyCalledOnce(idleMocks);
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
            public void DeactivateAndThenCloseShouldCloseAllConnections()
            {
                var idleConnections = new BlockingCollection<IPooledConnection>();
                var idleMocks = FillIdleConnections(idleConnections, 5);

                var inUseConnections = new ConcurrentSet<IPooledConnection>();
                var inUseMocks = FillInUseConnections(inUseConnections, 10);
                var pool = NewConnectionPool(idleConnections, inUseConnections);

                // When
                pool.Deactivate();
                // Then
                idleConnections.Count.Should().Be(0);
                inUseConnections.Count.Should().Be(10);
                VerifyDestroyCalledOnce(idleMocks);
                // refill the idle connections
                var newIdleMocks = FillIdleConnections(idleConnections, 5);
                idleConnections.Count.Should().Be(5);

                // When
                pool.Close();
                // Then
                idleConnections.Count.Should().Be(0);
                inUseConnections.Count.Should().Be(0);

                VerifyDestroyCalledOnce(newIdleMocks);
                VerifyDestroyCalledOnce(inUseMocks);
            }

            // concurrent tests
            // ConcurrentlyAcquireAndDeactivate
            [Fact]
            public void ReturnConnectionIfAcquiredValidConnectionBeforeInactivation()
            {
                // Given
                var idleConnections = new BlockingCollection<IPooledConnection>();
                var openConnMock = new Mock<IPooledConnection>();
                var pool = NewConnectionPoolWithConnectionTimeoutCheckDisabled(idleConnections);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);

                // This is to simulate Acquire called first,
                // but before Acquire put a new conn into inUseConn, Deactivate get called.
                openConnMock.Setup(x => x.IsOpen).Returns(true)
                    .Callback(() => pool.Deactivate());
                idleConnections.Add(openConnMock.Object);
                pool.NumberOfIdleConnections.Should().Be(1);
                // When
                pool.Acquire();

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
                openConnMock.Verify(x => x.IsOpen, Times.Once);
            }

            [Fact]
            public void ErrorIfAcquiredInvalidConnectionBeforeInactivation()
            {
                // Given
                var idleConnections = new BlockingCollection<IPooledConnection>();
                var closedConnMock = new Mock<IPooledConnection>();
                var pool = NewConnectionPoolWithConnectionTimeoutCheckDisabled(idleConnections);

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);

                // This is to simulate Acquire called first,
                // but before Acquire put a new conn into inUseConn, Deactivate get called.
                // However here, this connection is not healthy and will be destroyed directly
                closedConnMock.Setup(x => x.IsOpen).Returns(false)
                    .Callback(() => pool.Deactivate());
                idleConnections.Add(closedConnMock.Object);
                pool.NumberOfIdleConnections.Should().Be(1);
                // When
                var exception = Record.Exception(() => pool.Acquire());

                pool.NumberOfIdleConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                closedConnMock.Verify(x => x.IsOpen, Times.Once);
                closedConnMock.Verify(x => x.Destroy(), Times.Once);
                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("Failed to acquire a connection");
            }

            // concurrent test
            // ConcurrentlyReleaseAndDeactivate
            [Fact]
            public void ShouldCloseConnectionReleasedDuringDeactivation()
            {
                // Given
                var idleConnections = new BlockingCollection<IPooledConnection>();
                var idleMocks = new List<Mock<IPooledConnection>>();
                idleMocks.AddRange(FillIdleConnections(idleConnections, 5));

                var specialConn = new Mock<IPooledConnection>();
                var releasedConn = new Mock<IPooledConnection>();
                specialConn.Setup(x => x.Destroy()).Callback(() => { idleConnections.Add(releasedConn.Object); });

                idleConnections.Add(specialConn.Object);
                idleMocks.Add(specialConn);
                idleMocks.Add(releasedConn);
                idleMocks.Count.Should().Be(5 + 2);

                var pool = NewConnectionPool(idleConnections);

                // When
                pool.Deactivate();

                // Then
                idleConnections.Count.Should().Be(0);
                VerifyDestroyCalledOnce(idleMocks);
            }

            // concurrent test
            // ConcurrentlyActivateAndDeactivate
            [Fact]
            public void ShouldCloseAllIdleConnectionsRegardlessActivateCalled()
            {
                // Given
                var idleConnections = new BlockingCollection<IPooledConnection>();
                var idleMocks = new List<Mock<IPooledConnection>>();
                idleMocks.AddRange(FillIdleConnections(idleConnections, 5));

                var specialConn = new Mock<IPooledConnection>();
                var pool = NewConnectionPool(idleConnections);
                specialConn.Setup(x => x.Destroy()).Callback(() => { pool.Activate(); });

                idleConnections.Add(specialConn.Object);
                idleMocks.Add(specialConn);

                idleMocks.AddRange(FillIdleConnections(idleConnections, 5));
                idleMocks.Count.Should().Be(5 + 1 + 5);

                // When
                pool.Deactivate();

                // Then
                idleConnections.Count.Should().Be(0);
                VerifyDestroyCalledOnce(idleMocks);
            }
        }

        private class TestConnectionValidator : IConnectionValidator
        {
            private readonly bool _isValid;

            public TestConnectionValidator(bool isValid = true)
            {
                _isValid = isValid;
            }

            public bool OnRelease(IPooledConnection connection)
            {
                return _isValid;
            }

            public Task<bool> OnReleaseAsync(IPooledConnection connection)
            {
                return Task.FromResult(_isValid);
            }

            public bool OnRequire(IPooledConnection connection)
            {
                return _isValid;
            }
        }

        private class MockedConnectionFactory : IPooledConnectionFactory
        {
            private readonly IConnection _connection;

            public MockedConnectionFactory(IConnection conn = null)
            {
                _connection = conn ?? new Mock<IConnection>().Object;
            }

            public IPooledConnection Create(Uri uri, IConnectionReleaseManager releaseManager,
                IConnectionListener listener)
            {
                return new PooledConnection(_connection, releaseManager, listener);
            }
        }

        private static ConnectionPool NewConnectionPool(
            BlockingCollection<IPooledConnection> idleConnections = null,
            ConcurrentSet<IPooledConnection> inUseConnections = null,
            ConnectionPoolSettings poolSettings = null,
            bool isConnectionValid = true)
        {
            return new ConnectionPool(new MockedConnectionFactory(), idleConnections, inUseConnections,
                poolSettings, new TestConnectionValidator(isConnectionValid));
        }

        private static ConnectionPool NewConnectionPoolWithConnectionTimeoutCheckDisabled(
            BlockingCollection<IPooledConnection> idleConnections,
            ConcurrentSet<IPooledConnection> inUseConnections = null)
        {
            return new ConnectionPool(new MockedConnectionFactory(), idleConnections, inUseConnections,
                validator: new ConnectionValidator(Config.InfiniteInterval, Config.InfiniteInterval));
        }

        private static IPooledConnectionFactory ReusableConnectionFactory
        {
            get
            {
                var mock = new Mock<IConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
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
    }
}