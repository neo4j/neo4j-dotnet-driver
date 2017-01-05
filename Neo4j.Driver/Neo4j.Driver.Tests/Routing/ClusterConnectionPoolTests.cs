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
using System.Linq;
using FluentAssertions;
using Moq;
using Xunit;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Routing;


namespace Neo4j.Driver.Tests
{
    public class ClusterConnectionPoolTests
    {
        private static Uri ServerUri { get; } = new Uri("bolt+routing://1234:5678");

        public class TryAcquireMethod
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
                IPooledConnection connection;
                var acquired = pool.TryAcquire(ServerUri, out connection);

                // Then
                acquired.Should().BeFalse();
                connectionPoolDict.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnExisitingConnectionPoolIfUriAlreadyExist()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var mockedConnection = new Mock<IPooledConnection>();
                mockedConnectionPool.Setup(x => x.Acquire()).Returns(mockedConnection.Object);

                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.Keys.Single().Should().Be(ServerUri);
                connectionPoolDict[ServerUri].Should().Be(mockedConnectionPool.Object);

                // When
                IPooledConnection connection;
                var acquired = pool.TryAcquire(ServerUri, out connection);

                // Then
                acquired.Should().BeTrue();
                connection.Should().Be(mockedConnection.Object);
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
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(new Uri(first), mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);
                IPooledConnection ignored;
                pool.TryAcquire(new Uri(second), out ignored).Should().Be(expectedResult);
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
                pool.Update(new[] { ServerUri });

                // Then
                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.ContainsKey(ServerUri).Should().BeTrue();
                connectionPoolDict[ServerUri].Should().Be(mockedConnectionPool.Object);
            }

            [Fact]
            public void ShouldRemoveNewlyCreatedPoolnIfDisposeAlreadyCalled()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var mockedConnectionPoolDict = new Mock<ConcurrentDictionary<Uri, IConnectionPool>>();
                var pool = new ClusterConnectionPool(mockedConnectionPool.Object, mockedConnectionPoolDict.Object);

                // When
                pool.Dispose();
                var exception = Record.Exception(() => pool.Update(new[] {ServerUri}));

                // Then
                mockedConnectionPool.Verify(x => x.Dispose());

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
                var pool = new ClusterConnectionPool(mockedConnectionPool.Object, connectionPoolDict);

                // When
                pool.Update(new Uri[0]);

                // Then
                connectionPoolDict.Count.Should().Be(0);
            }
        }

        public class PurgeMethod
        {
            [Fact]
            public void ShouldRemovedIfExist()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(ServerUri, mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                pool.Purge(ServerUri);
                
                // Then
                mockedConnectionPool.Verify(x=>x.Dispose(), Times.Once);
                connectionPoolDict.Count.Should().Be(0);
                connectionPoolDict.ContainsKey(ServerUri).Should().BeFalse();
            }

            [Fact]
            public void ShouldRemoveNothingIfNotFound()
            {
                // Given
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                pool.Purge(ServerUri);

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
                mockedConnectionPool.Verify(x => x.Dispose(), Times.Once);
                connectionPoolDict.Count.Should().Be(0);
                connectionPoolDict.ContainsKey(ServerUri).Should().BeFalse();

            }
        }
    }
}
