// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Linq;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Routing
{
    public class ClusterConnectionPoolTests
    {
        private static Uri ServerUri { get; } = new Uri("bolt+routing://1234:5678");

        public class Constructor
        {
            [Fact]
            public void ShouldEnsureInitialRouter()
            {
                var uris = new HashSet<Uri> {new Uri("bolt://123:456")};
                var config = Config.DefaultConfig;
                var connSettings = new ConnectionSettings(new Mock<IAuthToken>().Object, config);
                var poolSettings = new ConnectionPoolSettings(config);
                var bufferSettings = new BufferSettings(config);

                var pool = new ClusterConnectionPool(connSettings, poolSettings, bufferSettings, uris, null);

                pool.ToString().Should().Be(
                    "[{bolt://123:456/ : _idleConnections: {[]}, _inUseConnections: {[]}}]");
            }
        }

        public class AcquireMethod
        {
            [Fact]
            public void ShouldNotCreateNewConnectionPoolIfUriDoseNotExist()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                var pool = new ClusterConnectionPool(mockedConnectionPool.Object, connectionPoolDict);

                connectionPoolDict.Count.Should().Be(0);

                // When
                IConnection connection = pool.Acquire(ServerUri);

                // Then
                connection.Should().BeNull();
                connectionPoolDict.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnExisitingConnectionPoolIfUriAlreadyExist()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var mockedConnection = new Mock<IPooledConnection>();
                mockedConnection.Setup(c => c.Init()).Throws(new InvalidOperationException("An exception"));
                mockedConnectionPool.Setup(x => x.Acquire(It.IsAny<AccessMode>())).Returns(mockedConnection.Object);

                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.Keys.Single().Should().Be(ServerUri);
                connectionPoolDict[ServerUri].Should().Be(mockedConnectionPool.Object);

                // When
                IConnection connection = pool.Acquire(ServerUri);

                // Then
                connection.Should().NotBeNull();
                var exception = Record.Exception(() => connection.Init());
                mockedConnection.Verify(c => c.Init(), Times.Once);
                exception.Should().BeOfType<InvalidOperationException>();
                exception.Message.Should().Be("An exception");
            }

            [Theory]
            [InlineData("bolt+routing://localhost:7687", "bolt+routing://127.0.0.1:7687", false)]
            [InlineData("bolt+routing://127.0.0.1:7687", "bolt+routing://127.0.0.1:7687", true)]
            [InlineData("bolt+routing://localhost:7687", "bolt+routing://localhost:7687", true)]
            [InlineData("bolt+routing://LOCALHOST:7687", "bolt+routing://localhost:7687", true)]
            public void AddressMatchTest(string first, string second, bool expectedResult)
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var mockedConnection = new Mock<IConnection>();
                mockedConnectionPool.Setup(x => x.Acquire(It.IsAny<AccessMode>())).Returns(mockedConnection.Object);
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(new Uri(first), mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);
                IConnection connection = pool.Acquire(new Uri(second));

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
            public void ShouldAddNewConnectionPoolIfDoesNotExist()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                var pool = new ClusterConnectionPool(mockedConnectionPool.Object, connectionPoolDict);

                // When
                pool.Update(new[] {ServerUri}, new Uri[0]);

                // Then
                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.ContainsKey(ServerUri).Should().BeTrue();
                connectionPoolDict[ServerUri].Should().Be(mockedConnectionPool.Object);
            }

            [Fact]
            public void ShouldRemoveNewlyCreatedPoolIfDisposeAlreadyCalled()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var mockedConnectionPoolDict = new Mock<ConcurrentDictionary<Uri, IConnectionPool>>();
                var pool = new ClusterConnectionPool(mockedConnectionPool.Object, mockedConnectionPoolDict.Object);

                // When
                pool.Dispose();
                var exception = Record.Exception(() => pool.Update(new[] {ServerUri}, new Uri[0]));

                // Then
                mockedConnectionPool.Verify(x => x.Close());

                exception.Should().BeOfType<ObjectDisposedException>();
                exception.Message.Should().Contain("Failed to create connections with server");
            }

            [Fact]
            public void ShouldRemoveServerPoolIfNotPresentInNewServers()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);
                mockedConnectionPool.Setup(x => x.NumberOfInUseConnections).Returns(0); // no need to explicitly config this
                var pool = new ClusterConnectionPool(mockedConnectionPool.Object, connectionPoolDict);

                // When
                pool.Update(new Uri[0], new[] {ServerUri});

                // Then
                mockedConnectionPool.Verify(x => x.Deactivate(), Times.Once); // first deactivate then remove
                connectionPoolDict.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldDeactivateServerPoolIfNotPresentInNewServersButHasInUseConnections()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);
                mockedConnectionPool.Setup(x => x.NumberOfInUseConnections).Returns(10); // non-zero number
                var pool = new ClusterConnectionPool(mockedConnectionPool.Object, connectionPoolDict);

                // When
                pool.Update(new Uri[0], new[] {ServerUri});

                // Then
                mockedConnectionPool.Verify(x => x.Deactivate(), Times.Once);
                connectionPoolDict.Count.Should().Be(1);
            }
        }

        public class AddMethod
        {
            [Fact]
            public void ShouldDeactivateIfExist()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                pool.Add(new []{ServerUri});

                // Then
                mockedConnectionPool.Verify(x => x.Activate(), Times.Once);
                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.ContainsKey(ServerUri).Should().BeTrue();
            }

            [Fact]
            public void ShouldAddIfNotFound()
            {
                // Given
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                var fakePoolMock = new Mock<IConnectionPool>();

                var pool = new ClusterConnectionPool(fakePoolMock.Object, connectionPoolDict);

                // When
                pool.Add(new[] {ServerUri});

                // Then
                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.ContainsKey(ServerUri).Should().BeTrue();
                connectionPoolDict[ServerUri].Should().Be(fakePoolMock.Object);
            }
        }

        public class DeactivateMethod
        {
            [Fact]
            public void ShouldDeactivateIfExist()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                pool.Deactivate(ServerUri);

                // Then
                mockedConnectionPool.Verify(x => x.Deactivate(), Times.Once);
                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.ContainsKey(ServerUri).Should().BeTrue();
            }

            [Fact]
            public void ShouldDeactivateNothingIfNotFound()
            {
                // Given
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                pool.Deactivate(ServerUri);

                // Then
                connectionPoolDict.Count.Should().Be(0);
                connectionPoolDict.ContainsKey(ServerUri).Should().BeFalse();
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldRemoveAllAfterDispose()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                pool.Dispose();

                // Then
                mockedConnectionPool.Verify(x => x.Close(), Times.Once);
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
    }
}
