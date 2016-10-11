using System;
using System.Collections.Concurrent;
using System.Linq;
using FluentAssertions;
using Moq;
using Xunit;
using Neo4j.Driver.Internal;


namespace Neo4j.Driver.Tests
{
    public class ClusterServerPoolTests
    {
        private static string ServerUri { get; } = "bolt+routing://1234:5678";

        public class AcquireMethod
        {
            [Fact]
            public void ShouldCreateNewConnectionPoolIfUriDoseNotExist()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var mockedConnection = new Mock<IPooledConnection>();
                mockedConnectionPool.Setup(x => x.Acquire()).Returns(mockedConnection.Object);
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                var pool = new ClusterConnectionPool(mockedConnectionPool.Object, connectionPoolDict);

                connectionPoolDict.Count.Should().Be(0);

                // When
                var connection = pool.Acquire(new Uri(ServerUri));

                // Then
                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.Keys.Single().Should().Be(new Uri(ServerUri));
                connectionPoolDict[new Uri(ServerUri)].Should().Be(mockedConnectionPool.Object);
                connection.Should().Be(mockedConnection.Object);
            }

            [Fact]
            public void ShouldReturnExisitingConnectionPoolIfUriAlreadyExist()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var mockedConnection = new Mock<IPooledConnection>();
                mockedConnectionPool.Setup(x => x.Acquire()).Returns(mockedConnection.Object);

                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(new Uri(ServerUri), mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.Keys.Single().Should().Be(new Uri(ServerUri));
                connectionPoolDict[new Uri(ServerUri)].Should().Be(mockedConnectionPool.Object);

                // When
                var connection = pool.Acquire(new Uri(ServerUri));

                // Then
                connection.Should().Be(mockedConnection.Object);
            }

            [Fact]
            public void ShouldRemoveNewlyCreatedPoolAndThrowExceptionIfDisposeAlreadyCalled()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var mockedConnectionPoolDict = new Mock<ConcurrentDictionary<Uri, IConnectionPool>>();
                var pool = new ClusterConnectionPool(mockedConnectionPool.Object, mockedConnectionPoolDict.Object);

                // When
                pool.Dispose();
                var exception = Record.Exception(() => pool.Acquire(new Uri(ServerUri)));

                // Then
                mockedConnectionPool.Verify(x=>x.Dispose());

                exception.Should().BeOfType<InvalidOperationException>();
                exception.Message.Should().Contain("Failed to create connections with server");
            }
        }

        public class HasAddressMethod
        {
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
                pool.HasAddress(new Uri(second)).Should().Be(expectedResult);
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
                connectionPoolDict.GetOrAdd(new Uri(ServerUri), mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                pool.Purge(new Uri(ServerUri));
                
                // Then
                mockedConnectionPool.Verify(x=>x.Dispose(), Times.Once);
                connectionPoolDict.Count.Should().Be(0);
                connectionPoolDict.ContainsKey(new Uri(ServerUri)).Should().BeFalse();
            }

            [Fact]
            public void ShouldRemoveNothingIfNotFound()
            {
                // Given
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                pool.Purge(new Uri(ServerUri));

                // Then
                connectionPoolDict.Count.Should().Be(0);
                connectionPoolDict.ContainsKey(new Uri(ServerUri)).Should().BeFalse();
            }
        }

        public class ReleaseMethod
        {
            [Fact]
            public void ShouldReleaseIfExist()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(new Uri(ServerUri), mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                var id = new Guid();
                pool.Release(new Uri(ServerUri), id);

                // Then
                mockedConnectionPool.Verify(x => x.Release(id), Times.Once);
                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.ContainsKey(new Uri(ServerUri)).Should().BeTrue();
            }

            [Fact]
            public void ShouldNotReleaseIfNotFound()
            {
                // Given
                var mockedConnectionPool = new Mock<IConnectionPool>();
                var connectionPoolDict = new ConcurrentDictionary<Uri, IConnectionPool>();
                connectionPoolDict.GetOrAdd(new Uri(ServerUri), mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                var id = new Guid();
                pool.Release(new Uri("http://123"), id);

                // Then
                mockedConnectionPool.Verify(x => x.Release(id), Times.Never());
                connectionPoolDict.Count.Should().Be(1);
                connectionPoolDict.ContainsKey(new Uri(ServerUri)).Should().BeTrue();
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
                connectionPoolDict.GetOrAdd(new Uri(ServerUri), mockedConnectionPool.Object);

                var pool = new ClusterConnectionPool(null, connectionPoolDict);

                // When
                pool.Dispose();

                // Then
                mockedConnectionPool.Verify(x => x.Dispose(), Times.Once);
                connectionPoolDict.Count.Should().Be(0);
                connectionPoolDict.ContainsKey(new Uri(ServerUri)).Should().BeFalse();

            }
        }
    }
}
