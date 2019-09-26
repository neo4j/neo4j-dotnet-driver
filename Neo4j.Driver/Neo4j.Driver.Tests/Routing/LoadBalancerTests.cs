// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver;
using Neo4j.Driver.Internal;
using Xunit;
using static Neo4j.Driver.Tests.Routing.RoutingTableManagerTests;

namespace Neo4j.Driver.Tests.Routing
{
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
                    routingTableManagerMock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);
                    var loadBalancer = new LoadBalancer(clusterPoolMock.Object, routingTableManagerMock.Object);

                    await loadBalancer.OnConnectionErrorAsync(uri, new ClientException());

                    clusterPoolMock.Verify(x => x.DeactivateAsync(uri), Times.Once);
                    routingTableMock.Verify(x => x.Remove(uri), Times.Once);
                    routingTableMock.Verify(x => x.RemoveWriter(uri), Times.Never);
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
                    routingTableManagerMock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);
                    var loadBalancer = new LoadBalancer(clusterPoolMock.Object, routingTableManagerMock.Object);

                    loadBalancer.OnWriteError(uri);
                    clusterPoolMock.Verify(x => x.DeactivateAsync(uri), Times.Never);
                    routingTableMock.Verify(x => x.Remove(uri), Times.Never);
                    routingTableMock.Verify(x => x.RemoveWriter(uri), Times.Once);
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
                mock.Setup(x => x.RoutingTable).Returns(NewMockedRoutingTable(mode, null).Object);
                var balancer = new LoadBalancer(null, mock.Object);

                // When
                var error = await Record.ExceptionAsync(() => balancer.AcquireAsync(mode));

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
                var uri = new Uri("bolt+routing://123:456");
                var mock = new Mock<IRoutingTableManager>();
                var routingTableMock = NewMockedRoutingTable(mode, uri);
                mock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);

                var clusterPoolMock = new Mock<IClusterConnectionPool>();
                var mockedConn = new Mock<IConnection>();
                mockedConn.Setup(x => x.Server.Address).Returns(uri.ToString);
                mockedConn.Setup(x => x.Mode).Returns(mode);
                var conn = mockedConn.Object;
                clusterPoolMock.Setup(x => x.AcquireAsync(uri, mode)).ReturnsAsync(conn);
                var balancer = new LoadBalancer(clusterPoolMock.Object, mock.Object);

                // When
                var acquiredConn = await balancer.AcquireAsync(mode);

                // Then
                acquiredConn.Server.Address.Should().Be(uri.ToString());
            }

            [Theory]
            [InlineData(AccessMode.Read)]
            [InlineData(AccessMode.Write)]
            public async Task ShouldForgetServerWhenFailedToEstablishConn(AccessMode mode)
            {
                // Given
                var uri = new Uri("bolt+routing://123:456");
                var routingTableMock = NewMockedRoutingTable(mode, uri);
                var mock = new Mock<IRoutingTableManager>();
                mock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);

                var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
                clusterConnPoolMock.Setup(x => x.AcquireAsync(uri, mode))
                    .Returns(Task.FromException<IConnection>(new ServiceUnavailableException("failed init")));

                var balancer = new LoadBalancer(clusterConnPoolMock.Object, mock.Object);

                // When
                var error = await Record.ExceptionAsync(() => balancer.AcquireAsync(mode));

                // Then
                error.Should().BeOfType<SessionExpiredException>();
                error.Message.Should().Contain("Failed to connect to any");

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
                var uri = new Uri("bolt+routing://123:456");
                var routingTableMock = NewMockedRoutingTable(mode, uri);
                var mock = new Mock<IRoutingTableManager>();
                mock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);

                var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
                clusterConnPoolMock.Setup(x => x.AcquireAsync(uri, mode))
                    .Returns(Task.FromException<IConnection>(
                        new SecurityException("Failed to establish ssl connection with the server")));

                var balancer = new LoadBalancer(clusterConnPoolMock.Object, mock.Object);

                // When
                var error = await Record.ExceptionAsync(() => balancer.AcquireAsync(mode));

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
            public async Task ShouldThrowErrorDirectlyIfProtocolError(AccessMode mode)
            {
                // Given
                var uri = new Uri("bolt+routing://123:456");
                var routingTableMock = NewMockedRoutingTable(mode, uri);
                var mock = new Mock<IRoutingTableManager>();
                mock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);

                var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
                clusterConnPoolMock.Setup(x => x.AcquireAsync(uri, mode)).Returns(
                    Task.FromException<IConnection>(new ProtocolException("do not understand struct 0x01")));

                var balancer = new LoadBalancer(clusterConnPoolMock.Object, mock.Object);

                // When
                var error = await Record.ExceptionAsync(() => balancer.AcquireAsync(mode));

                // Then
                error.Should().BeOfType<ProtocolException>();
                error.Message.Should().Contain("do not understand struct 0x01");

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
                    new List<Uri> {new Uri("router:1"), new Uri("router:2")},
                    new List<Uri> {new Uri("reader:1"), new Uri("reader:2"), new Uri("reader:3")},
                    new List<Uri> {new Uri("writer:1"), new Uri("writer:2")});

                var routingTableManager = new Mock<IRoutingTableManager>();
                routingTableManager.Setup(x => x.RoutingTable).Returns(routingTable);

                var clusterPoolMock = new Mock<IClusterConnectionPool>();
                clusterPoolMock.Setup(x => x.AcquireAsync(It.IsAny<Uri>(), mode))
                    .ReturnsAsync((Uri uri, AccessMode m) => NewConnectionMock(uri, m));

                var balancer = new LoadBalancer(clusterPoolMock.Object, routingTableManager.Object);

                if (mode == AccessMode.Read)
                {
                    (await balancer.AcquireAsync(mode)).Server.Address.Should().Be("reader:1");
                    (await balancer.AcquireAsync(mode)).Server.Address.Should().Be("reader:2");
                    (await balancer.AcquireAsync(mode)).Server.Address.Should().Be("reader:3");

                    (await balancer.AcquireAsync(mode)).Server.Address.Should().Be("reader:1");
                    (await balancer.AcquireAsync(mode)).Server.Address.Should().Be("reader:2");
                    (await balancer.AcquireAsync(mode)).Server.Address.Should().Be("reader:3");
                }
                else if (mode == AccessMode.Write)
                {
                    (await balancer.AcquireAsync(mode)).Server.Address.Should().Be("writer:1");
                    (await balancer.AcquireAsync(mode)).Server.Address.Should().Be("writer:2");

                    (await balancer.AcquireAsync(mode)).Server.Address.Should().Be("writer:1");
                    (await balancer.AcquireAsync(mode)).Server.Address.Should().Be("writer:2");
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
}