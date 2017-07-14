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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
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
                public void ShouldRmoveFromLoadBalancer()
                {
                    var clusterPoolMock = new Mock<IClusterConnectionPool>();
                    var routingTableMock = new Mock<IRoutingTable>();
                    var uri = new Uri("https://neo4j.com");
                    var routingTableManagerMock = new Mock<IRoutingTableManager>();
                    routingTableManagerMock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);
                    var loadBalancer = new LoadBalancer(clusterPoolMock.Object, routingTableManagerMock.Object);

                    loadBalancer.OnConnectionError(uri, new ClientException());
                    clusterPoolMock.Verify(x=>x.Purge(uri),Times.Once);
                    routingTableMock.Verify(x=>x.Remove(uri),Times.Once);
                    routingTableMock.Verify(x=>x.RemoveWriter(uri),Times.Never);
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
                    clusterPoolMock.Verify(x => x.Purge(uri), Times.Never);
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
            public void ShouldThrowSessionExpiredExceptionIfNoServerAvailable(AccessMode mode)
            {
                // Given
                var mock = new Mock<IRoutingTableManager>();
                mock.Setup(x => x.RoutingTable).Returns(NewMockedRoutingTable(mode, null, false).Object);
                var balancer = new LoadBalancer(null, mock.Object);

                // When
                var error = Record.Exception(() => balancer.Acquire(mode));

                // Then
                error.Should().BeOfType<SessionExpiredException>();
                error.Message.Should().Contain("Failed to connect to any");
            }

            [Theory]
            [InlineData(AccessMode.Read)]
            [InlineData(AccessMode.Write)]
            public void ShouldReturnConnectionWithCorrectMode(AccessMode mode)
            {
                // Given
                var uri = new Uri("bolt+routing://123:456");
                var mock = new Mock<IRoutingTableManager>();
                var routingTableMock = NewMockedRoutingTable(mode, uri);
                mock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);

                var clusterPoolMock = new Mock<IClusterConnectionPool>();
                var mockedConn = new Mock<IConnection>();
                mockedConn.Setup(x => x.Server.Address).Returns(uri.ToString);
                var conn = mockedConn.Object;
                clusterPoolMock.Setup(x => x.TryAcquire(uri, out conn)).Returns(true);
                var balancer = new LoadBalancer(clusterPoolMock.Object, mock.Object);
                
                // When
                var acquiredConn = balancer.Acquire(mode);

                // Then
                acquiredConn.Server.Address.Should().Be(uri.ToString());
            }

            [Theory]
            [InlineData(AccessMode.Read)]
            [InlineData(AccessMode.Write)]
            public void ShouldForgetServerWhenFailedToEstablishConn(AccessMode mode)
            {
                // Given
                var uri = new Uri("bolt+routing://123:456");
                var routingTableMock = NewMockedRoutingTable(mode, uri);
                var mock = new Mock<IRoutingTableManager>();
                mock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);

                var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
                IConnection conn = null;
                clusterConnPoolMock.Setup(x => x.TryAcquire(uri, out conn))
                    .Callback(() => throw new ServiceUnavailableException("failed init"));

                var balancer = new LoadBalancer(clusterConnPoolMock.Object, mock.Object);

                // When
                var error = Record.Exception(() => balancer.Acquire(mode));

                // Then
                error.Should().BeOfType<SessionExpiredException>();
                error.Message.Should().Contain("Failed to connect to any");

                // should be removed
                routingTableMock.Verify(m => m.Remove(uri), Times.Once);
                clusterConnPoolMock.Verify(m => m.Purge(uri), Times.Once);
            }

            [Theory]
            [InlineData(AccessMode.Read)]
            [InlineData(AccessMode.Write)]
            public void ShouldThrowErrorDirectlyIfSecurityError(AccessMode mode)
            {
                // Given
                var uri = new Uri("bolt+routing://123:456");
                var routingTableMock = NewMockedRoutingTable(mode, uri);
                var mock = new Mock<IRoutingTableManager>();
                mock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);

                var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
                IConnection conn = null;
                clusterConnPoolMock.Setup(x => x.TryAcquire(uri, out conn))
                    .Callback(() => throw new SecurityException("Failed to establish ssl connection with the server"));

                var balancer = new LoadBalancer(clusterConnPoolMock.Object, mock.Object);

                // When
                var error = Record.Exception(() => balancer.Acquire(mode));

                // Then
                error.Should().BeOfType<SecurityException>();
                error.Message.Should().Contain("ssl connection with the server");

                // while the server is not removed
                routingTableMock.Verify(m => m.Remove(uri), Times.Never);
                clusterConnPoolMock.Verify(m => m.Purge(uri), Times.Never);
            }

            [Theory]
            [InlineData(AccessMode.Read)]
            [InlineData(AccessMode.Write)]
            public void ShouldThrowErrorDirectlyIfProtocolError(AccessMode mode)
            {
                // Given
                var uri = new Uri("bolt+routing://123:456");
                var routingTableMock = NewMockedRoutingTable(mode, uri);
                var mock = new Mock<IRoutingTableManager>();
                mock.Setup(x => x.RoutingTable).Returns(routingTableMock.Object);

                var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
                IConnection conn = null;
                clusterConnPoolMock.Setup(x => x.TryAcquire(uri, out conn)).Returns(false)
                    .Callback(() => throw new ProtocolException("do not understand struct 0x01"));

                var balancer = new LoadBalancer(clusterConnPoolMock.Object, mock.Object);

                // When
                var error = Record.Exception(() => balancer.Acquire(mode));

                // Then
                error.Should().BeOfType<ProtocolException>();
                error.Message.Should().Contain("do not understand struct 0x01");

                // while the server is not removed
                routingTableMock.Verify(m => m.Remove(uri), Times.Never);
                clusterConnPoolMock.Verify(m => m.Purge(uri), Times.Never);
            }
        }
    }
}
