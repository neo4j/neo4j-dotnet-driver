// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Tests
{
    public class ConnectionPoolTests
    {
        internal static ConnectionPool NewConnectionPoolWithNoConnectionTimeoutValidation(
            IConnection connection,
            BlockingCollection<IPooledConnection> availableConnections = null,
            ConcurrentSet<IPooledConnection> inUseConnections = null)
        {
            var testConfigWithIdleTimeoutAndLifetimeCheckDisabled = new Config
            {
                MaxConnectionLifetime = Config.InfiniteInterval,
                ConnectionIdleTimeout = Config.InfiniteInterval
            };
            return new ConnectionPool(connection, availableConnections, inUseConnections,
                poolSettings: new ConnectionPoolSettings(testConfigWithIdleTimeoutAndLifetimeCheckDisabled));
        }
        public class AcquireMethod
        {
            private readonly ITestOutputHelper _output;

            private IConnection MockedConnection
            {
                get
                {
                    var mock = new Mock<IPooledConnection>();
                    mock.Setup(x => x.IsOpen).Returns(true);
                    mock.Setup(x => x.IdleTimer).Returns(MockedTimer);
                    mock.Setup(x => x.LifetimeTimer).Returns(MockedTimer);
                    return mock.Object;
                }
            }

            private ITimer MockedTimer
            {
                get {
                    var mock = new Mock<ITimer>();
                    mock.Setup(t => t.ElapsedMilliseconds).Returns(0);
                    return mock.Object;
                }
            }

            public AcquireMethod(ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public void ShouldCallConnInit()
            {
                // Given
                var mock = new Mock<IConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                var connectionPool = new ConnectionPool(mock.Object);
                // When
                connectionPool.Acquire();

                //Then
                mock.Verify(x => x.Init(), Times.Once);
            }

            [Fact]
            public void ShouldBlockWhenMaxPoolSizeReached()
            {
                var connectionPoolSettings = new ConnectionPoolSettings(new Config {MaxConnectionPoolSize = 2});
                var pool = new ConnectionPool(MockedConnection, poolSettings: connectionPoolSettings);
                var conn = pool.Acquire();
                pool.Acquire();
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(2);

                var timer = new Stopwatch();
                var blockingAcquire = new Task(()=>{ pool.Acquire(); });
                var releaseConn = new Task(()=>{ conn.Close(); });

                timer.Start();
                blockingAcquire.Start();
                Task.Delay(1000).Wait(); // delay a bit here
                releaseConn.Start();

                releaseConn.Wait();
                blockingAcquire.Wait();
                timer.Stop();

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(2);
                timer.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(1000);
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
                var pool = new ConnectionPool(MockedConnection, poolSettings: connectionPoolSettings);
                var conn = pool.Acquire();
                pool.Acquire();
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(2);

                var exception = Record.Exception(() => pool.Acquire());
                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().Contain("Failed to obtain a connection from pool within");
            }

            [Fact]
            public void ShouldNotExceedIdleLimit()
            {
                var connectionPoolSettings = new ConnectionPoolSettings(new Config {MaxIdleConnectionPoolSize = 2});
                var pool = new ConnectionPool(MockedConnection, poolSettings: connectionPoolSettings);

                var conns = new List<IConnection>();
                for (var i = 0; i < 4; i++)
                {
                    conns.Add(pool.Acquire());
                    pool.NumberOfAvailableConnections.Should().Be(0);
                }

                foreach (var conn in conns)
                {
                    conn.Close();
                    pool.NumberOfAvailableConnections.Should().BeLessOrEqualTo(2);
                }

                pool.NumberOfAvailableConnections.Should().Be(2);
            }

            [Fact]
            public void ShouldAcquireFromPoolIfAvailable()
            {
                var connectionPoolSettings = new ConnectionPoolSettings(new Config {MaxIdleConnectionPoolSize = 2});
                var pool = new ConnectionPool(MockedConnection, poolSettings: connectionPoolSettings);

                for (var i = 0; i < 4; i++)
                {
                    var conn = pool.Acquire();
                    pool.NumberOfAvailableConnections.Should().Be(0);
                    conn.Close();
                    pool.NumberOfAvailableConnections.Should().Be(1);
                }

                pool.NumberOfAvailableConnections.Should().Be(1);
            }

            [Fact]
            public void ShouldCreateNewWhenQueueIsEmpty()
            {
                var pool = new ConnectionPool(MockedConnection);

                pool.Acquire();
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
            }

            [Fact]
            public void ShouldCloseConnectionIfFailedToCreate()
            {
                var mockedConnection = new Mock<IConnection>();
                mockedConnection.Setup(x => x.Init()).Throws(new NotImplementedException());

                var pool = new ConnectionPool(mockedConnection.Object);

                Record.Exception(() => pool.Acquire());
                mockedConnection.Verify(x => x.Destroy(), Times.Once);
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
            }

            [Fact]
            public void ShouldCreateNewWhenQueueOnlyContainsClosedConnections()
            {
                var conns = new BlockingCollection<IPooledConnection>();
                var closedId = Guid.NewGuid();
                var closedMock = new Mock<IPooledConnection>();
                closedMock.Setup(x => x.IsOpen).Returns(false);
                closedMock.Setup(x => x.Id).Returns(closedId);

                conns.Add(closedMock.Object);
                var pool = new ConnectionPool(MockedConnection, conns);

                pool.NumberOfAvailableConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                var conn = pool.Acquire();

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
                closedMock.Verify(x => x.IsOpen, Times.Once);
                closedMock.Verify(x => x.Destroy(), Times.Once);

                conn.Should().NotBeNull();
                conn.Id.Should().NotBe(closedId);
            }

            [Fact]
            public void ShouldReuseWhenOpenConnectionInQueue()
            {
                var conns = new BlockingCollection<IPooledConnection>();
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                mock.Setup(x => x.IdleTimer).Returns(MockedTimer);
                mock.Setup(x => x.LifetimeTimer).Returns(MockedTimer);

                conns.Add(mock.Object);
                var pool = new ConnectionPool(MockedConnection, conns);

                pool.NumberOfAvailableConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                var conn = pool.Acquire();

                pool.NumberOfAvailableConnections.Should().Be(0);
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
                var unhealthyMock = new Mock<IPooledConnection>();
                unhealthyMock.Setup(x => x.IsOpen).Returns(false);

                conns.Add(unhealthyMock.Object);
                conns.Add(healthyMock.Object);
                var pool = NewConnectionPoolWithNoConnectionTimeoutValidation(MockedConnection, conns);

                pool.NumberOfAvailableConnections.Should().Be(2);
                pool.NumberOfInUseConnections.Should().Be(0);

                var conn = pool.Acquire();

                pool.NumberOfAvailableConnections.Should().Be(0);
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
                var idleTooLongId = Guid.NewGuid();
                mock.Setup(x => x.Id).Returns(idleTooLongId);

                var conns = new BlockingCollection<IPooledConnection>();
                conns.Add(mock.Object);
                var enableIdleTooLongTest = TimeSpan.FromMilliseconds(100);
                var poolSettings = new ConnectionPoolSettings(
                    new Config {MaxIdleConnectionPoolSize = 2, ConnectionIdleTimeout = enableIdleTooLongTest});
                var pool = new ConnectionPool(MockedConnection, conns, poolSettings: poolSettings);

                pool.NumberOfAvailableConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                // When
                var conn = pool.Acquire();

                // Then
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
                mock.Verify(x => x.Destroy(), Times.Once);

                conn.Should().NotBeNull();
                conn.Id.Should().NotBe(idleTooLongId);
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
                var idleTooLongId = Guid.NewGuid();
                mock.Setup(x => x.Id).Returns(idleTooLongId);

                var conns = new BlockingCollection<IPooledConnection>();
                conns.Add(mock.Object);
                var enableIdleTooLongTest = TimeSpan.FromMilliseconds(100);
                var poolSettings = new ConnectionPoolSettings(
                    new Config {
                        MaxIdleConnectionPoolSize = 2,
                        ConnectionIdleTimeout = enableIdleTooLongTest,
                        MaxConnectionLifetime = Config.InfiniteInterval, // disable life time check
                    });
                var pool = new ConnectionPool(MockedConnection, conns, poolSettings: poolSettings);

                pool.NumberOfAvailableConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                // When
                var conn = pool.Acquire();

                // Then
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                conn.Should().Be(mock.Object);
                conn.Id.Should().Be(idleTooLongId);
            }

            [Theory]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(5)]
            [InlineData(10)]
            [InlineData(500)]
            public void ShouldAcquireNewWhenBeingUsedConcurrentlyBy(int numberOfThreads)
            {
                var ids = new List<Guid>();
                for (var i = 0; i < numberOfThreads; i++)
                {
                    ids.Add(Guid.NewGuid());
                }

                var mockConns = new Queue<Mock<IPooledConnection>>();
                var conns = new BlockingCollection<IPooledConnection>();
                for (var i = 0; i < numberOfThreads; i++)
                {
                    var mock = new Mock<IPooledConnection>();
                    mock.Setup(x => x.IsOpen).Returns(true);
                    mock.Setup(x => x.Id).Returns(ids[i]);
                    conns.Add(mock.Object);
                    mockConns.Enqueue(mock);
                }

                var pool = NewConnectionPoolWithNoConnectionTimeoutValidation(MockedConnection, conns);

                pool.NumberOfAvailableConnections.Should().Be(numberOfThreads);
                pool.NumberOfInUseConnections.Should().Be(0);

                var receivedIds = new List<Guid>();

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
                                    receivedIds.Add(((IPooledConnection) conn).Id);
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
                var pool = new ConnectionPool(MockedConnection);

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
                var conns = new BlockingCollection<IPooledConnection>();
                var healthyMock = new Mock<IPooledConnection>();
                var pool = NewConnectionPoolWithNoConnectionTimeoutValidation(MockedConnection, conns);

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);

                // This is to simulate Acquire called first,
                // but before Acquire put a new conn into inUseConn, Dispose get called.
                // Note: Once dispose get called, it is forbiden to put anything into queue.
                healthyMock.Setup(x => x.IsOpen).Returns(true)
                    .Callback(() => pool.DisposeCalled = true); // Simulte Dispose get called at this time
                conns.Add(healthyMock.Object);
                pool.NumberOfAvailableConnections.Should().Be(1);
                // When
                var exception = Record.Exception(() => pool.Acquire());

                pool.NumberOfAvailableConnections.Should().Be(0);
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

                var pool = new ConnectionPool(MockedConnection, null, null, null, new ConnectionPoolSettings(config), null);

                for (var i = 0; i < config.MaxConnectionPoolSize; i++)
                {
                    pool.Acquire();
                }

                var stopWatch = new Stopwatch(); stopWatch.Start();

                var exception = Record.Exception(() => pool.Acquire());

                stopWatch.Elapsed.Seconds.Should().BeGreaterOrEqualTo(10);
                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("Failed to obtain a connection from pool within");
            }

            [Fact]
            public void ShouldTimeoutAfterAcquireTimeoutWhenConnectionIsNotValidated()
            {
                Config config = Config.Builder.WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(5))
                    .WithConnectionTimeout(Config.InfiniteInterval)
                    .ToConfig();

                var notValidConnection = new Mock<IPooledConnection>();
                notValidConnection.Setup(x => x.IsOpen).Returns(false);

                var pool = new ConnectionPool(notValidConnection.Object, null, null, null, new ConnectionPoolSettings(config), null);

                var exception = Record.Exception(() => pool.Acquire());

                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("Failed to obtain a connection from pool within");
            }

            [Fact]
            public async void ShouldTimeoutAfterAcquireAsyncTimeoutIfPoolIsFull()
            {
                Config config = Config.Builder.WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(10))
                    .WithMaxConnectionPoolSize(5).WithMaxIdleConnectionPoolSize(0).ToConfig();

                var pool = new ConnectionPool(MockedConnection, null, null, null, new ConnectionPoolSettings(config), null);

                for (var i = 0; i < config.MaxConnectionPoolSize; i++)
                {
                    pool.Acquire();
                }

                var stopWatch = new Stopwatch(); stopWatch.Start();

                var exception = await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read));

                stopWatch.Elapsed.Seconds.Should().BeGreaterOrEqualTo(10);
                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("Failed to obtain a connection from pool within");
            }

            [Fact]
            public async void ShouldTimeoutAfterAcquireAsyncTimeoutWhenConnectionIsNotValidated()
            {
                Config config = Config.Builder.WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(5))
                    .ToConfig();

                var notValidConnection = new Mock<IPooledConnection>();
                notValidConnection.Setup(x => x.IsOpen).Returns(false);

                var pool = new ConnectionPool(notValidConnection.Object, null, null, null, new ConnectionPoolSettings(config), null);

                var exception = await Record.ExceptionAsync(() => pool.AcquireAsync(AccessMode.Read));

                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("Failed to obtain a connection from pool within");
            }

        }

        public class ReleaseMethod
        {
            [Fact]
            public void ShouldReturnToPoolWhenConnectionIsReusableAndPoolIsNotFull()
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);

                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                var pool = new ConnectionPool(null, null, inUseConns);

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(mock.Object);

                pool.NumberOfAvailableConnections.Should().Be(1);
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

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(mock.Object);

                pool.NumberOfAvailableConnections.Should().Be(0);
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

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(mock.Object);

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Destroy(), Times.Once);
            }

            [Fact]
            public void ShouldCloseTheConnectionWhenConnectionIsReusableButThePoolIsFull()
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);

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

                var pool = new ConnectionPool(null, availableConns, inUseConns, poolSettings: poolSettings);

                pool.NumberOfAvailableConnections.Should().Be(10);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(mock.Object);

                pool.NumberOfAvailableConnections.Should().Be(10);
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

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                //When
                pool.Release(mock.Object);

                // Then
                pool.NumberOfAvailableConnections.Should().Be(1);
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

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                //When
                pool.Release(mock.Object);

                // Then
                pool.NumberOfAvailableConnections.Should().Be(1);
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

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);

                var mock = new Mock<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                pool.NumberOfInUseConnections.Should().Be(1);

                // When
                // this is to simulate Release called first,
                // but before Release put a new conn into availConns, Dispose get called.
                // Note: Once dispose get called, it is forbiden to put anything into queue.
                mock.Setup(x => x.IsOpen).Returns(true)
                    .Callback(() => pool.DisposeCalled = true); // Simulte Dispose get called at this time
                pool.Release(mock.Object);

                // Then
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Destroy(), Times.Once);
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldReleaseAll()
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);

                var availableConns = new BlockingCollection<IPooledConnection>();
                var mock1 = new Mock<IPooledConnection>();
                mock1.Setup(x => x.IsOpen).Returns(true);

                availableConns.Add(mock1.Object);

                var pool = new ConnectionPool(null, availableConns, inUseConns);
                pool.NumberOfAvailableConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Dispose();

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
            }

            [Fact]
            public void ShouldLogInUseAndAvailableConnectionIds()
            {
                var mockLogger = new Mock<ILogger>();
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);

                var availableConns = new BlockingCollection<IPooledConnection>();
                var mock1 = new Mock<IPooledConnection>();
                mock1.Setup(x => x.IsOpen).Returns(true);

                availableConns.Add(mock1.Object);

                var pool = new ConnectionPool(null, availableConns, inUseConns, mockLogger.Object);

                pool.Dispose();

                mockLogger.Verify(x => x.Info(It.Is<string>(actual => actual.StartsWith("Disposing In Use"))),
                    Times.Once);
                mockLogger.Verify(x => x.Debug(It.Is<string>(actual => actual.StartsWith("Disposing Available"))),
                    Times.Once);
            }

            [Fact]
            public void ShouldReturnDirectlyWhenConnectionReleaseCalledAfterPoolDispose()
            {
                // Given
                var mock = new Mock<IPooledConnection>();
                var inUseConns = new ConcurrentSet<IPooledConnection>();
                inUseConns.TryAdd(mock.Object);
                var pool = new ConnectionPool(null, null, inUseConns);

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
                var pool = new ConnectionPool(null, null, inUseConns);

                // When
                pool.Dispose();
                pool.Dispose();

                // Then
                mock.Verify(x => x.Destroy(), Times.Once);
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
            }
        }

        public class NumberOfInUseConnections
        {
            [Fact]
            public void ShouldReturnZeroAfterCreation()
            {
                var uri = new Uri("localhost:7687");
                var connectionSettings = new ConnectionSettings(AuthTokens.None, Config.DefaultConfig);
                var poolSettings = new ConnectionPoolSettings(1, 1, Config.InfiniteInterval, Config.InfiniteInterval, Config.InfiniteInterval);
                var bufferSettings = new BufferSettings(Config.DefaultConfig);
                var logger = new Mock<ILogger>().Object;

                var pool = new ConnectionPool(uri, connectionSettings, poolSettings, bufferSettings, logger);

                pool.NumberOfInUseConnections.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnCorrectCountWhenOnlyInUseConnectionsPresent()
            {
                var connectionMock = new Mock<IConnection>();
                // pool has no idle connections
                var availableConnections = new BlockingCollection<IPooledConnection>();

                // pool has 3 in-use connections
                var inUseConnections = new ConcurrentSet<IPooledConnection>();
                inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);
                inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);
                inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);

                var logger = new Mock<ILogger>().Object;

                var pool = new ConnectionPool(connectionMock.Object, availableConnections, inUseConnections, logger);

                pool.NumberOfInUseConnections.Should().Be(3);
            }

            [Fact]
            public void ShouldReturnZeroWhenOnlyIdleConnectionsPresent()
            {
                var connectionMock = new Mock<IConnection>();

                // pool has 2 idle connections
                var availableConnections = new BlockingCollection<IPooledConnection>();
                availableConnections.TryAdd(new Mock<IPooledConnection>().Object);
                availableConnections.TryAdd(new Mock<IPooledConnection>().Object);

                // pool has no in-use connections
                var inUseConnections = new ConcurrentSet<IPooledConnection>();
                var logger = new Mock<ILogger>().Object;

                var pool = new ConnectionPool(connectionMock.Object, availableConnections, inUseConnections, logger);

                pool.NumberOfInUseConnections.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnCorrectCountWhenBothIdleAndInUseConnectionsPresent()
            {
                var connectionMock = new Mock<IConnection>();

                // pool has 3 idle connections
                var availableConnections = new BlockingCollection<IPooledConnection>();
                availableConnections.TryAdd(new Mock<IPooledConnection>().Object);
                availableConnections.TryAdd(new Mock<IPooledConnection>().Object);
                availableConnections.TryAdd(new Mock<IPooledConnection>().Object);

                // pool has 2 in-use connections
                var inUseConnections = new ConcurrentSet<IPooledConnection>();
                inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);
                inUseConnections.TryAdd(new Mock<IPooledConnection>().Object);

                var logger = new Mock<ILogger>().Object;

                var pool = new ConnectionPool(connectionMock.Object, availableConnections, inUseConnections, logger);

                pool.NumberOfInUseConnections.Should().Be(2);
            }
        }

        public class PoolSize
        {

            private static ConnectionPool CreatePool(IConnection conn, int maxIdlePoolSize, int maxPoolSize)
            {
                var poolSettings = new ConnectionPoolSettings(maxIdlePoolSize, maxPoolSize, Config.InfiniteInterval, Config.InfiniteInterval, Config.InfiniteInterval);
                var bufferSettings = new BufferSettings(Config.DefaultConfig);

                var pool = new ConnectionPool(conn, null, null, null, poolSettings, bufferSettings);

                return pool;
            }

            [Fact]
            public void ShoulReportCorrectPoolSizeWhenIdleConnectionsAreNotAllowed()
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
            public void ShoulReportCorrectPoolSize()
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
            public void ShoulReportPoolSizeCorrectOnConcurrentRequests()
            {
                var connectionMock = new Mock<IConnection>();
                connectionMock.Setup(x => x.IsOpen).Returns(true);
                var pool = CreatePool(connectionMock.Object, 5, 5);

                var rnd = new Random(Guid.NewGuid().GetHashCode());
                var acquireCounter = 0;
                var releaseCounter = 0;
                var stopMarker = 0;
                var waitedTime = 0;

                var acquireTasks = Enumerable.Range(0, 100).Select(i => Task.Run(() => {
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
            public async void ShoulReportCorrectPoolSizeAsync()
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
            public async void ShoulReportCorrectPoolSizeWhenIdleConnectionsAreNotAllowedAsync()
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
            public async void ShoulReportPoolSizeCorrectOnConcurrentRequestsAsync()
            {
                var connectionMock = new Mock<IConnection>();
                connectionMock.Setup(x => x.IsOpen).Returns(true);
                var pool = CreatePool(connectionMock.Object, 5, 5);

                var rnd = new Random(Guid.NewGuid().GetHashCode());
                var acquireCounter = 0;
                var releaseCounter = 0;
                var stopMarker = 0;
                var waitedTime = 0;

                var acquireTasks = Enumerable.Range(0, 100).Select(i => Task.Run(async () => {
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

    }
}
