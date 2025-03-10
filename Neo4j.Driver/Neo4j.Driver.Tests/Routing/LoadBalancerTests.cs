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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Routing;
using Xunit;
using static Neo4j.Driver.Tests.Routing.RoutingTableManagerTests;

namespace Neo4j.Driver.Tests.Routing;

public class LoadBalancerTests
{
    public class ClusterErrorHandlerTests
    {
        public class OnConnectionErrorMethod
        {
            [Fact]
            public async Task ShouldRemoveFromLoadBalancer()
            {
                var clusterPoolMock = new Mock<IClusterConnectionPool>();
                var routingTableMock = new Mock<IRoutingTable>();
                var uri = new Uri("https://neo4j.com");
                var routingTableManagerMock = new Mock<IRoutingTableManager>();
                routingTableManagerMock
                    .Setup(x => x.EnsureRoutingTableForModeAsync(AccessMode.Read, null, null, Bookmarks.Empty))
                    .ReturnsAsync(routingTableMock.Object);

                var loadBalancer = new LoadBalancer(clusterPoolMock.Object, routingTableManagerMock.Object);

                await loadBalancer.OnConnectionErrorAsync(uri, null, new ClientException());

                clusterPoolMock.Verify(x => x.DeactivateAsync(uri), Times.Once);
                routingTableManagerMock.Verify(x => x.ForgetServer(uri, null), Times.Once);
                routingTableManagerMock.Verify(x => x.ForgetWriter(uri, null), Times.Never);
            }
        }

        public class OnWriteErrorMethod
        {
            [Fact]
            public void ShouldRemoveWriterFromRoutingTable()
            {
                var clusterPoolMock = new Mock<IClusterConnectionPool>();
                var routingTableMock = new Mock<IRoutingTable>();
                var uri = new Uri("https://neo4j.com");
                var routingTableManagerMock = new Mock<IRoutingTableManager>();
                routingTableManagerMock
                    .Setup(x => x.EnsureRoutingTableForModeAsync(AccessMode.Write, null, null, Bookmarks.Empty))
                    .ReturnsAsync(routingTableMock.Object);

                var loadBalancer = new LoadBalancer(clusterPoolMock.Object, routingTableManagerMock.Object);

                loadBalancer.OnWriteError(uri, null);
                clusterPoolMock.Verify(x => x.DeactivateAsync(uri), Times.Never);
                routingTableManagerMock.Verify(x => x.ForgetServer(uri, null), Times.Never);
                routingTableManagerMock.Verify(x => x.ForgetWriter(uri, null), Times.Once);
            }
        }
    }

    public class AcquireMethod
    {
        [Theory]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public async Task ShouldThrowSessionExpiredExceptionIfNoServerAvailable(AccessMode mode)
        {
            // Given
            var mock = new Mock<IRoutingTableManager>();
            mock.Setup(x => x.EnsureRoutingTableForModeAsync(mode, null, null, Bookmarks.Empty))
                .ReturnsAsync(NewMockedRoutingTable(mode, null, string.Empty).Object);

            var balancer = new LoadBalancer(null, mock.Object);

            // When
            var error = await Record.ExceptionAsync(() => balancer.AcquireAsync(mode, null, null, Bookmarks.Empty));

            // Then
            error.Should().BeOfType<SessionExpiredException>();
            error.Message.Should().Contain("Failed to connect to any");
        }

        [Theory]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public async Task ShouldReturnConnectionWithCorrectMode(AccessMode mode)
        {
            // Given
            var uri = new Uri("neo4j://123:456");
            var mock = new Mock<IRoutingTableManager>();
            var routingTableMock = NewMockedRoutingTable(mode, uri, string.Empty);
            mock.Setup(
                    x => x.EnsureRoutingTableForModeAsync(
                        mode,
                        It.IsAny<string>(),
                        It.IsAny<SessionConfig>(),
                        Bookmarks.Empty))
                .ReturnsAsync(routingTableMock.Object);

            var clusterPoolMock = new Mock<IClusterConnectionPool>();
            var mockedConn = new Mock<IConnection>();
            mockedConn.Setup(x => x.Server.Address).Returns(uri.ToString);
            mockedConn.Setup(x => x.Mode).Returns(mode);
            var conn = mockedConn.Object;
            clusterPoolMock
                .Setup(
                    x => x.AcquireAsync(
                        uri,
                        mode,
                        It.IsAny<string>(),
                        It.IsAny<SessionConfig>(),
                        Bookmarks.Empty,
                        false))
                .ReturnsAsync(conn);

            var balancer = new LoadBalancer(clusterPoolMock.Object, mock.Object);

            // When
            var acquiredConn = await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty);

            // Then
            acquiredConn.Server.Address.Should().Be(uri.ToString());
        }

        [Theory]
        [InlineData("OriginalDB", "AliasDB", "AliasDB")]
        [InlineData("OriginalDB", "OriginalDB", "OriginalDB")]
        [InlineData("", "AliasDB", "AliasDB")]
        [InlineData(null, "AliasDB", "AliasDB")]
        public async Task ShouldReturnConnectionWithDBFromRoutingTable(
            string dbName,
            string aliasDbName,
            string desiredResult)
        {
            var mode = AccessMode.Read;
            // Given
            var uri = new Uri("neo4j://123:456");
            var mockManager = new Mock<IRoutingTableManager>();
            var routingTableMock = NewMockedRoutingTable(mode, uri, aliasDbName);
            mockManager.Setup(x => x.EnsureRoutingTableForModeAsync(mode, dbName, null, Bookmarks.Empty))
                .ReturnsAsync(routingTableMock.Object);

            var clusterPoolMock = new Mock<IClusterConnectionPool>();
            var mockedConn = new Mock<IConnection>();
            mockedConn.Setup(x => x.Server.Address).Returns(uri.ToString);
            mockedConn.Setup(x => x.Mode).Returns(mode);
            mockedConn.Setup(x => x.Database).Returns(aliasDbName);
            clusterPoolMock.Setup(x => x.AcquireAsync(uri, mode, aliasDbName, null, Bookmarks.Empty, false))
                .ReturnsAsync(mockedConn.Object);

            var balancer = new LoadBalancer(clusterPoolMock.Object, mockManager.Object);

            // When
            var acquiredConn = await balancer.AcquireAsync(mode, dbName, null, Bookmarks.Empty);

            // Then
            acquiredConn.Database.Should().Be(desiredResult);
        }

        [Theory]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public void ShouldForgetServerWhenFailedToEstablishConn(AccessMode mode)
        {
            // Given
            var uri = new Uri("neo4j://123:456");
            var routingTableMock = NewMockedRoutingTable(mode, uri, string.Empty);
            var mock = new Mock<IRoutingTableManager>();
            mock.Setup(
                    x => x.EnsureRoutingTableForModeAsync(
                        mode,
                        It.IsAny<string>(),
                        It.IsAny<SessionConfig>(),
                        Bookmarks.Empty))
                .ReturnsAsync(routingTableMock.Object);

            mock.Setup(x => x.ForgetServer(It.IsAny<Uri>(), It.IsAny<string>()))
                .Callback((Uri u, string _) => routingTableMock.Object.Remove(u));

            mock.Setup(x => x.ForgetWriter(It.IsAny<Uri>(), It.IsAny<string>()))
                .Callback((Uri u, string _) => routingTableMock.Object.RemoveWriter(u));

            var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
            clusterConnPoolMock.Setup(
                    x => x.AcquireAsync(
                        uri,
                        mode,
                        It.IsAny<string>(),
                        It.IsAny<SessionConfig>(),
                        Bookmarks.Empty,
                        false))
                .Returns(Task.FromException<IConnection>(new ServiceUnavailableException("failed init")));

            var balancer = new LoadBalancer(clusterConnPoolMock.Object, mock.Object);

            // When & Then
            balancer.Awaiting(b => b.AcquireAsync(mode, It.IsAny<string>(), It.IsAny<SessionConfig>(), Bookmarks.Empty))
                .Should()
                .ThrowAsync<SessionExpiredException>()
                .WithMessage("Failed to connect to any*");

            // should be removed
            routingTableMock.Verify(m => m.Remove(uri), Times.Once);
            clusterConnPoolMock.Verify(m => m.DeactivateAsync(uri), Times.Once);
        }

        [Theory]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public async Task ShouldThrowErrorDirectlyIfSecurityError(AccessMode mode)
        {
            // Given
            var uri = new Uri("neo4j://123:456");
            var routingTableMock = NewMockedRoutingTable(mode, uri, string.Empty);
            var mock = new Mock<IRoutingTableManager>();
            mock.Setup(x => x.EnsureRoutingTableForModeAsync(mode, null, null, Bookmarks.Empty))
                .ReturnsAsync(routingTableMock.Object);

            var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
            clusterConnPoolMock.Setup(
                    x => x.AcquireAsync(
                        uri,
                        mode,
                        It.IsAny<string>(),
                        It.IsAny<SessionConfig>(),
                        Bookmarks.Empty,
                        false))
                .Returns(
                    Task.FromException<IConnection>(
                        new SecurityException("Failed to establish ssl connection with the server")));

            var balancer = new LoadBalancer(clusterConnPoolMock.Object, mock.Object);

            // When
            var error = await Record.ExceptionAsync(() => balancer.AcquireAsync(mode, null, null, Bookmarks.Empty));

            // Then
            error.Should().BeOfType<SecurityException>();
            error.Message.Should().Contain("ssl connection with the server");

            // while the server is not removed
            routingTableMock.Verify(m => m.Remove(uri), Times.Never);
            clusterConnPoolMock.Verify(m => m.DeactivateAsync(uri), Times.Never);
        }

        [Theory]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public void ShouldThrowErrorDirectlyIfProtocolError(AccessMode mode)
        {
            // Given
            var uri = new Uri("neo4j://123:456");
            var routingTableMock = NewMockedRoutingTable(mode, uri, string.Empty);
            var mock = new Mock<IRoutingTableManager>();
            mock.Setup(
                    x => x.EnsureRoutingTableForModeAsync(
                        mode,
                        It.IsAny<string>(),
                        It.IsAny<SessionConfig>(),
                        Bookmarks.Empty))
                .ReturnsAsync(routingTableMock.Object);

            var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
            clusterConnPoolMock
                .Setup(
                    x => x.AcquireAsync(
                        uri,
                        mode,
                        It.IsAny<string>(),
                        It.IsAny<SessionConfig>(),
                        Bookmarks.Empty,
                        false))
                .Returns(Task.FromException<IConnection>(new ProtocolException("do not understand struct 0x01")));

            var balancer = new LoadBalancer(clusterConnPoolMock.Object, mock.Object);

            // When
            balancer.Awaiting(b => b.AcquireAsync(mode, null, null, Bookmarks.Empty))
                .Should()
                .ThrowAsync<ProtocolException>()
                .WithMessage("*do not understand struct 0x01*");

            // while the server is not removed
            routingTableMock.Verify(m => m.Remove(uri), Times.Never);
            clusterConnPoolMock.Verify(m => m.DeactivateAsync(uri), Times.Never);
        }

        [Theory]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public async Task ShouldReturnConnectionAccordingToLoadBalancingStrategy(AccessMode mode)
        {
            var routingTable = NewRoutingTable(
                new List<Uri> { new("router:1"), new("router:2") },
                new List<Uri> { new("reader:1"), new("reader:2"), new("reader:3") },
                new List<Uri> { new("writer:1"), new("writer:2") });

            var routingTableManager = new Mock<IRoutingTableManager>();
            routingTableManager.Setup(x => x.EnsureRoutingTableForModeAsync(mode, null, null, Bookmarks.Empty))
                .ReturnsAsync(routingTable);

            var clusterPoolMock = new Mock<IClusterConnectionPool>();
            clusterPoolMock.Setup(
                    x => x.AcquireAsync(
                        It.IsAny<Uri>(),
                        mode,
                        It.IsAny<string>(),
                        It.IsAny<SessionConfig>(),
                        Bookmarks.Empty,
                        false))
                .ReturnsAsync(
                    (Uri uri, AccessMode m, string _, string _, Bookmarks _, bool _) => NewConnectionMock(uri, m));

            var balancer = new LoadBalancer(clusterPoolMock.Object, routingTableManager.Object);

            if (mode == AccessMode.Read)
            {
                (await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty)).Server.Address.Should()
                    .Be("reader:1");

                (await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty)).Server.Address.Should()
                    .Be("reader:2");

                (await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty)).Server.Address.Should()
                    .Be("reader:3");

                (await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty)).Server.Address.Should()
                    .Be("reader:1");

                (await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty)).Server.Address.Should()
                    .Be("reader:2");

                (await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty)).Server.Address.Should()
                    .Be("reader:3");
            }
            else if (mode == AccessMode.Write)
            {
                (await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty)).Server.Address.Should()
                    .Be("writer:1");

                (await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty)).Server.Address.Should()
                    .Be("writer:2");

                (await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty)).Server.Address.Should()
                    .Be("writer:1");

                (await balancer.AcquireAsync(mode, null, null, Bookmarks.Empty)).Server.Address.Should()
                    .Be("writer:2");
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private static IConnection NewConnectionMock(Uri uri, AccessMode mode)
        {
            var mockedConn = new Mock<IConnection>();
            mockedConn.Setup(x => x.Server.Address).Returns(uri.ToString);
            mockedConn.Setup(x => x.Mode).Returns(mode);
            return mockedConn.Object;
        }
    }
}
