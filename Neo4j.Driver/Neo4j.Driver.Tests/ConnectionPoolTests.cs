// Copyright (c) 2002-2016 "Neo Technology,"
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
using System.Collections.Generic;
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
        public class AcquireMethod
        {
            private readonly ITestOutputHelper _output;
            private IConnection MockedConnection
            {
                get
                {
                    var mock = new Mock<IConnection>();
                    mock.Setup(x => x.IsOpen).Returns(true);
                    return mock.Object;
                }
            }

            public AcquireMethod(ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public void ShouldAddExternalConnectionHandlerIfNotNull()
            {
                // Given
                var mock = new Mock<IConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);

                var mockedHandler = new Mock<IConnectionErrorHandler>();
                var connectionPool = new ConnectionPool(mock.Object, exteralErrorHandler:mockedHandler.Object);
                // When
                connectionPool.Acquire();

                //Then
                mock.Verify(x=>x.AddConnectionErrorHander(mockedHandler.Object), Times.Once);
                mock.Verify(x => x.Init(), Times.Once);
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
            public void ShouldNotThrowExceptionWhenIdlePoolSizeReached()
            {
                var connectionPoolSettings = new ConnectionPoolSettings(2);
                var pool = new ConnectionPool(MockedConnection, settings:connectionPoolSettings);
                pool.Acquire();
                pool.Acquire();
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(2);

                var ex = Record.Exception(() => pool.Acquire());
                ex.Should().BeNull();
            }

            [Fact]
            public void ShouldNotExceedIdleLimit()
            {
                var connectionPoolSettings = new ConnectionPoolSettings(2);
                var pool = new ConnectionPool(MockedConnection, settings: connectionPoolSettings);

                var conns = new List<IPooledConnection>();
                for (var i = 0; i < 4; i++)
                {
                    conns.Add(pool.Acquire());
                    pool.NumberOfAvailableConnections.Should().Be(0);
                }

                foreach (var conn in conns)
                {
                    conn.Dispose();
                    pool.NumberOfAvailableConnections.Should().BeLessOrEqualTo(2);
                }

                pool.NumberOfAvailableConnections.Should().Be(2);
            }

            [Fact]
            public void ShouldAcquireFromPoolIfAvailable()
            {
                var connectionPoolSettings = new ConnectionPoolSettings(2);
                var pool = new ConnectionPool(MockedConnection, settings:connectionPoolSettings);

                for (var i = 0; i < 4; i++)
                {
                    var conn = pool.Acquire();
                    pool.NumberOfAvailableConnections.Should().Be(0);
                    conn.Dispose();
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
            public void ShouldCreateNewWhenQueueOnlyContainsUnhealthyConnections()
            {
                var conns = new Queue<IPooledConnection>();
                var unhealthyId = Guid.NewGuid();
                var unhealthyMock = new Mock<IPooledConnection>();
                unhealthyMock.Setup(x => x.IsOpen).Returns(false);
                unhealthyMock.Setup(x => x.Id).Returns(unhealthyId);

                conns.Enqueue(unhealthyMock.Object);
                var pool = new ConnectionPool(MockedConnection, conns);

                pool.NumberOfAvailableConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);

                var conn = pool.Acquire();

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
                unhealthyMock.Verify(x => x.IsOpen, Times.Once);
                unhealthyMock.Verify(x => x.Close(), Times.Once);

                conn.Should().NotBeNull();
                conn.Id.Should().NotBe(unhealthyId);
            }

            [Fact]
            public void ShouldReuseOldWhenReusableConnectionInQueue()
            {
                var conns = new Queue<IPooledConnection>();
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);

                conns.Enqueue(mock.Object);
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
            public void ShouldReuseReusableWhenReusableConnectionInQueue()
            {
                var conns = new Queue<IPooledConnection>();
                var healthyMock = new Mock<IPooledConnection>();
                healthyMock.Setup(x => x.IsOpen).Returns(true);
                var unhealthyMock = new Mock<IPooledConnection>();
                unhealthyMock.Setup(x => x.IsOpen).Returns(false);

                conns.Enqueue(unhealthyMock.Object);
                conns.Enqueue(healthyMock.Object);
                var pool = new ConnectionPool(MockedConnection, conns);

                pool.NumberOfAvailableConnections.Should().Be(2);
                pool.NumberOfInUseConnections.Should().Be(0);

                var conn = pool.Acquire();

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);
                unhealthyMock.Verify(x => x.Close(), Times.Once);
                healthyMock.Verify(x => x.Close(), Times.Never);
                conn.Should().Be(healthyMock.Object);
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
                var conns = new Queue<IPooledConnection>();
                for (var i = 0; i < numberOfThreads; i++)
                {
                    var mock = new Mock<IPooledConnection>();
                    mock.Setup(x => x.IsOpen).Returns(true);
                    mock.Setup(x => x.Id).Returns(ids[i]);
                    conns.Enqueue(mock.Object);
                    mockConns.Enqueue(mock);
                }

                var pool = new ConnectionPool(MockedConnection, conns);

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
                                    receivedIds.Add(conn.Id);
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

            [Fact]
            public void ShouldThrowExceptionWhenAcquireCalledAfterDispose()
            {
                var pool = new ConnectionPool(MockedConnection);

                pool.Dispose();
                var exception = Record.Exception(() => pool.Acquire());
                exception.Should().BeOfType<ObjectDisposedException>();
                exception.Message.Should().Contain("Cannot acquire a new connection from the connection pool");
            }

            // thread-safe test
            // concurrent call of Acquire and Dispose
            [Fact]
            public void ShouldCloseAcquiredConnectionIfPoolDisposeStarted()
            {
                // Given
                var conns = new Queue<IPooledConnection>();
                var healthyMock = new Mock<IPooledConnection>();
                var pool = new ConnectionPool(MockedConnection, conns);

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);

                // This is to simulate Acquire called first,
                // but before Acquire put a new conn into inUseConn, Dispose get called.
                // Note: Once dispose get called, it is forbiden to put anything into queue.
                healthyMock.Setup(x => x.IsOpen).Returns(true)
                    .Callback(() => pool.DisposeCalled = true); // Simulte Dispose get called at this time
                conns.Enqueue(healthyMock.Object);
                pool.NumberOfAvailableConnections.Should().Be(1);
                // When
                var exception = Record.Exception(() => pool.Acquire());

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                healthyMock.Verify(x => x.IsOpen, Times.Once);
                healthyMock.Verify(x => x.Close(), Times.Once);
                exception.Should().BeOfType<ObjectDisposedException>();
                exception.Message.Should().Contain("Cannot acquire a new connection from the connection pool");
            }
        }

        public class ReleaseMethod
        {
            [Fact]
            public void ShouldReturnToPoolWhenConnectionIsReusableAndPoolIsNotFull()
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                var id = new Guid();

                var inUseconns = new Dictionary<Guid, IPooledConnection>();
                inUseconns.Add(id, mock.Object);
                var pool = new ConnectionPool(null, null, inUseconns);

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(id);

                pool.NumberOfAvailableConnections.Should().Be(1);
                pool.NumberOfInUseConnections.Should().Be(0);
            }

            [Fact]
            public void ShouldCloseConnectionWhenConnectionIsUnhealthy()
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(false);
                var id = new Guid();

                var inUseConns = new Dictionary<Guid, IPooledConnection>();
                inUseConns.Add(id, mock.Object);
                var pool = new ConnectionPool(null, null, inUseConns);

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(id);

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Close(), Times.Once);
            }

            [Fact]
            public void ShouldCloseConnectionWhenConnectionIsOpenButNotResetable()
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                mock.Setup(x => x.ClearConnection()).Throws<ClientException>();
                var id = new Guid();

                var inUseConns = new Dictionary<Guid, IPooledConnection>();
                inUseConns.Add(id, mock.Object);
                var pool = new ConnectionPool(null, null, inUseConns);

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(id);

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Close(), Times.Once);
            }

            [Fact]
            public void ShouldCloseTheConnectionIfSessionIsReusableButThePoolIsFull()
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                var id = new Guid();

                var inUseConns = new Dictionary<Guid, IPooledConnection>();
                inUseConns.Add(id, mock.Object);

                var availableConns = new Queue<IPooledConnection>();
                var pooledConnMock = new Mock<IPooledConnection>();
                for (int i = 0; i < Config.DefaultConfig.MaxIdleSessionPoolSize; i++)
                {
                    availableConns.Enqueue(pooledConnMock.Object);
                }

                var pool = new ConnectionPool(null, availableConns, inUseConns);

                pool.NumberOfAvailableConnections.Should().Be(10);
                pool.NumberOfInUseConnections.Should().Be(1);

                pool.Release(id);

                pool.NumberOfAvailableConnections.Should().Be(10);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Close(), Times.Once);
            }

            // thread safe test
            // Concurrent call of Release and Dispose
            [Fact]
            public void ShouldCloseConnectionIfPoolDisposeStarted()
            {
                // Given
                var inUseConns = new Dictionary<Guid, IPooledConnection>();
                var pool = new ConnectionPool(null, null, inUseConns);

                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);

                var mock = new Mock<IPooledConnection>();
                var id = new Guid();
                inUseConns.Add(id, mock.Object);
                pool.NumberOfInUseConnections.Should().Be(1);

                // When
                // this is to simulate Release called first,
                // but before Release put a new conn into availConns, Dispose get called.
                // Note: Once dispose get called, it is forbiden to put anything into queue.
                mock.Setup(x => x.IsOpen).Returns(true)
                    .Callback(() => pool.DisposeCalled = true); // Simulte Dispose get called at this time
                pool.Release(id);

                // Then
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
                mock.Verify(x => x.Close(), Times.Once);
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldReleaseAll()
            {
                var mock = new Mock<IPooledConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);
                var id = Guid.NewGuid();
                var inUseConns = new Dictionary<Guid, IPooledConnection>();
                inUseConns.Add(id, mock.Object);

                var availableConns = new Queue<IPooledConnection>();
                var mock1 = new Mock<IPooledConnection>();
                mock1.Setup(x => x.IsOpen).Returns(true);

                availableConns.Enqueue(mock1.Object);

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
                var id = Guid.NewGuid();
                var inUseConns = new Dictionary<Guid, IPooledConnection>();
                inUseConns.Add(id, mock.Object);

                var availableConns = new Queue<IPooledConnection>();
                var mock1 = new Mock<IPooledConnection>();
                mock1.Setup(x => x.IsOpen).Returns(true);

                availableConns.Enqueue(mock1.Object);

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
                var id = Guid.NewGuid();
                var inUseConns = new Dictionary<Guid, IPooledConnection> {{id, mock.Object}};
                var pool = new ConnectionPool(null, null, inUseConns);

                // When
                pool.Dispose();
                pool.Release(id);

                // Then
                mock.Verify(x => x.Close(), Times.Once);
            }

            [Fact]
            public void ShouldNotThrowExceptionWhenDisposedTwice()
            {
                // Given
                var mock = new Mock<IPooledConnection>();
                var id = Guid.NewGuid();
                var inUseConns = new Dictionary<Guid, IPooledConnection> { { id, mock.Object } };
                var pool = new ConnectionPool(null, null, inUseConns);

                // When
                pool.Dispose();
                pool.Dispose();

                // Then
                mock.Verify(x => x.Close(), Times.Once);
                pool.NumberOfAvailableConnections.Should().Be(0);
                pool.NumberOfInUseConnections.Should().Be(0);
            }
        }
    }
}