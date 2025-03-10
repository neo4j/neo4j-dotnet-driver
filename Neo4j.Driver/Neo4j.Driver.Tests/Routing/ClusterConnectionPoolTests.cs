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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Routing;
using Xunit;

namespace Neo4j.Driver.Tests.Routing;

public class ClusterConnectionPoolTests
{
    private static Uri ServerUri { get; } = new("neo4j://1234:5678");

    public class Constructor
    {
        [Fact]
        public void ShouldEnsureInitialRouter()
        {
            var uri = new Uri("bolt://123:456");
            var uris = new HashSet<Uri> { uri };
            var connFactory = new Mock<IPooledConnectionFactory>().Object;
            var driverContext = new DriverContext(
                uri,
                AuthTokenManagers.None,
                new Config());

            var pool = new ClusterConnectionPool(
                uris,
                connFactory,
                driverContext);

            pool.ToString().Should().Contain("bolt://123:456/");

            pool.ToString().Should().Contain("_idleConnections: {[]}, _inUseConnections: {[]}");
        }
    }

    public class AcquireMethod
    {
        [Fact]
        public async Task ShouldNotCreateNewConnectionPoolIfUriDoesNotExist()
        {
            // Given
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            var pool = new ClusterConnectionPool(new MockedPoolFactory(), connectionPoolDict);

            connectionPoolDict.Count.Should().Be(0);

            // When
            var connection = await pool.AcquireAsync(
                ServerUri,
                AccessMode.Write,
                null,
                null,
                Bookmarks.Empty,
                false);

            // Then
            connection.Should().BeNull();
            connectionPoolDict.Count.Should().Be(0);
        }

        [Fact]
        public async Task ShouldReturnExistingConnectionPoolIfUriAlreadyExist()
        {
            // Given
            var mockedConnectionPool = new Mock<IConnectionPool>();
            var mockedConnection = new Mock<IPooledConnection>();
            mockedConnection.Setup(c => c.InitAsync(It.IsAny<SessionConfig>(), CancellationToken.None))
                .Returns(Task.FromException(new InvalidOperationException("An exception")));

            mockedConnectionPool.Setup(
                    x =>
                        x.AcquireAsync(It.IsAny<AccessMode>(), It.IsAny<string>(), null, It.IsAny<Bookmarks>(), false))
                .ReturnsAsync(mockedConnection.Object);

            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);

            var pool = new ClusterConnectionPool(null, connectionPoolDict);

            connectionPoolDict.Count.Should().Be(1);
            connectionPoolDict.Keys.Single().Should().Be(ServerUri);
            connectionPoolDict[ServerUri].Should().Be(mockedConnectionPool.Object);

            // When
            var connection = await pool.AcquireAsync(ServerUri, AccessMode.Write, null, null, Bookmarks.Empty, false);

            // Then
            connection.Should().NotBeNull();
            var exception =
                await Record.ExceptionAsync(() => connection.InitAsync());

            mockedConnection.Verify(
                c => c.InitAsync(It.IsAny<SessionConfig>(), CancellationToken.None),
                Times.Once);

            exception.Should().BeOfType<InvalidOperationException>();
            exception.Message.Should().Be("An exception");
        }

        [Theory]
        [InlineData("neo4j://localhost:7687", "neo4j://127.0.0.1:7687", false)]
        [InlineData("neo4j://127.0.0.1:7687", "neo4j://127.0.0.1:7687", true)]
        [InlineData("neo4j://localhost:7687", "neo4j://localhost:7687", true)]
        [InlineData("neo4j://LOCALHOST:7687", "neo4j://localhost:7687", true)]
        public async Task AddressMatchTest(string first, string second, bool expectedResult)
        {
            // Given
            var mockedConnectionPool = new Mock<IConnectionPool>();
            var mockedConnection = new Mock<IConnection>();
            mockedConnectionPool.Setup(
                    x =>
                        x.AcquireAsync(
                            It.IsAny<AccessMode>(),
                            It.IsAny<string>(),
                            It.IsAny<SessionConfig>(),
                            It.IsAny<Bookmarks>(),
                            false))
                .ReturnsAsync(mockedConnection.Object);

            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            connectionPoolDict.GetOrAdd(new Uri(first), mockedConnectionPool.Object);

            var pool = new ClusterConnectionPool(null, connectionPoolDict);
            var connection = await pool.AcquireAsync(
                new Uri(second),
                AccessMode.Write,
                null,
                null,
                Bookmarks.Empty,
                false);

            if (expectedResult)
            {
                connection.Should().NotBeNull();
            }
            else
            {
                connection.Should().BeNull();
            }
        }
    }

    public class UpdateMethod
    {
        [Fact]
        public async Task ShouldAddNewConnectionPoolIfDoesNotExist()
        {
            // Given
            var mockedConnectionPool = new Mock<IConnectionPool>();
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            var pool = new ClusterConnectionPool(
                new MockedPoolFactory(mockedConnectionPool.Object),
                connectionPoolDict);

            // When
            await pool.UpdateAsync(new[] { ServerUri }, new Uri[0]);

            // Then
            connectionPoolDict.Count.Should().Be(1);
            connectionPoolDict.ContainsKey(ServerUri).Should().BeTrue();
            connectionPoolDict[ServerUri].Should().Be(mockedConnectionPool.Object);
        }

        [Fact]
        public async Task ShouldRemoveNewlyCreatedPoolIfCloseAlreadyCalled()
        {
            // Given
            var mockedConnectionPool = new Mock<IConnectionPool>();
            var mockedConnectionPoolDict = new Mock<ConcurrentDictionary<Uri, IConnectionPool>>();
            var pool = new ClusterConnectionPool(
                new MockedPoolFactory(mockedConnectionPool.Object),
                mockedConnectionPoolDict.Object);

            // When
            await pool.CloseAsync();
            var exception = await Record.ExceptionAsync(() => pool.UpdateAsync(new[] { ServerUri }, new Uri[0]));

            // Then
            mockedConnectionPool.Verify(x => x.DisposeAsync());

            exception.Should().BeOfType<ObjectDisposedException>();
            exception.Message.Should().Contain("Failed to create connections with server");
        }

        [Fact]
        public async Task ShouldRemoveServerPoolIfNotPresentInNewServers()
        {
            // Given
            var mockedConnectionPool = new Mock<IConnectionPool>();
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);
            mockedConnectionPool.Setup(x => x.NumberOfInUseConnections)
                .Returns(0); // no need to explicitly config this

            var pool = new ClusterConnectionPool(
                new MockedPoolFactory(mockedConnectionPool.Object),
                connectionPoolDict);

            // When
            await pool.UpdateAsync(new Uri[0], new[] { ServerUri });

            // Then
            mockedConnectionPool.Verify(x => x.DeactivateAsync(), Times.Once); // first deactivate then remove
            connectionPoolDict.Count.Should().Be(0);
        }

        [Fact]
        public async Task ShouldDeactivateServerPoolIfNotPresentInNewServersButHasInUseConnections()
        {
            // Given
            var mockedConnectionPool = new Mock<IConnectionPool>();
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);
            mockedConnectionPool.Setup(x => x.NumberOfInUseConnections).Returns(10); // non-zero number
            var pool = new ClusterConnectionPool(
                new MockedPoolFactory(mockedConnectionPool.Object),
                connectionPoolDict);

            // When
            await pool.UpdateAsync(new Uri[0], new[] { ServerUri });

            // Then
            mockedConnectionPool.Verify(x => x.DeactivateAsync(), Times.Once);
            connectionPoolDict.Count.Should().Be(1);
        }
    }

    public class AddMethod
    {
        [Fact]
        public async Task ShouldActivateIfExist()
        {
            // Given
            var mockedConnectionPool = new Mock<IConnectionPool>();
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);

            var pool = new ClusterConnectionPool(new MockedPoolFactory(), connectionPoolDict);

            // When
            await pool.AddAsync(new[] { ServerUri });

            // Then
            mockedConnectionPool.Verify(x => x.Activate(), Times.Once);
            connectionPoolDict.Count.Should().Be(1);
            connectionPoolDict.ContainsKey(ServerUri).Should().BeTrue();
        }

        [Fact]
        public async Task ShouldAddIfNotFound()
        {
            // Given
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            var fakePoolMock = new Mock<IConnectionPool>();

            var pool = new ClusterConnectionPool(new MockedPoolFactory(fakePoolMock.Object), connectionPoolDict);

            // When
            await pool.AddAsync(new[] { ServerUri });

            // Then
            connectionPoolDict.Count.Should().Be(1);
            connectionPoolDict.ContainsKey(ServerUri).Should().BeTrue();
            connectionPoolDict[ServerUri].Should().Be(fakePoolMock.Object);
        }
    }

    public class DeactivateMethod
    {
        [Fact]
        public async Task ShouldDeactivateIfExist()
        {
            // Given
            var mockedConnectionPool = new Mock<IConnectionPool>();
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);

            var pool = new ClusterConnectionPool(null, connectionPoolDict);

            // When
            await pool.DeactivateAsync(ServerUri);

            // Then
            mockedConnectionPool.Verify(x => x.DeactivateAsync(), Times.Once);
            connectionPoolDict.Count.Should().Be(1);
            connectionPoolDict.ContainsKey(ServerUri).Should().BeTrue();
        }

        [Fact]
        public async Task ShouldDeactivateNothingIfNotFound()
        {
            // Given
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();

            var pool = new ClusterConnectionPool(null, connectionPoolDict);

            // When
            await pool.DeactivateAsync(ServerUri);

            // Then
            connectionPoolDict.Count.Should().Be(0);
            connectionPoolDict.ContainsKey(ServerUri).Should().BeFalse();
        }
    }

    public class CloseMethod
    {
        [Fact]
        public async Task ShouldRemoveAllAfterClose()
        {
            // Given
            var mockedConnectionPool = new Mock<IConnectionPool>();
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);

            var pool = new ClusterConnectionPool(null, connectionPoolDict);

            await pool.DisposeAsync();

            // Then
            mockedConnectionPool.Verify(x => x.DisposeAsync(), Times.Once);
            connectionPoolDict.Count.Should().Be(0);
            connectionPoolDict.ContainsKey(ServerUri).Should().BeFalse();
        }
    }

    public class NumberOfInUseConnectionsMethod
    {
        [Fact]
        public void ShouldReturnZeroForMissingAddress()
        {
            var missingAddress = new Uri("localhost:1");
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            var pool = new ClusterConnectionPool(null, connectionPoolDict);

            var numberOfInUseConnections = pool.NumberOfInUseConnections(missingAddress);

            numberOfInUseConnections.Should().Be(0);
        }

        [Fact]
        public void ShouldReturnCorrectCountForPresentAddress()
        {
            var presentAddress = new Uri("localhost:1");
            var mockedConnectionPool = new Mock<IConnectionPool>();
            mockedConnectionPool.Setup(x => x.NumberOfInUseConnections).Returns(42);
            var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
            connectionPoolDict.TryAdd(presentAddress, mockedConnectionPool.Object);
            var pool = new ClusterConnectionPool(null, connectionPoolDict);

            var numberOfInUseConnections = pool.NumberOfInUseConnections(presentAddress);

            numberOfInUseConnections.Should().Be(42);
        }
    }

    private class MockedPoolFactory : IConnectionPoolFactory
    {
        private readonly IConnectionPool _pool;

        public MockedPoolFactory(IConnectionPool pool = null)
        {
            _pool = pool ?? new Mock<IConnectionPool>().Object;
        }

        public IConnectionPool Create(Uri uri)
        {
            return _pool;
        }
    }
}
