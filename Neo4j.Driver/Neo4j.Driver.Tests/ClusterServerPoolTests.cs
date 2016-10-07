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
    }
}
