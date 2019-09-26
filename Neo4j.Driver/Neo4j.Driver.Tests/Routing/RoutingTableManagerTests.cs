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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver;
using Xunit;

namespace Neo4j.Driver.Tests.Routing
{
    public class RoutingTableManagerTests
    {
        internal static Uri InitialUri = new Uri("bolt+routing://neo4j.com:6060");

        internal static RoutingTableManager NewRoutingTableManager(
            IRoutingTable routingTable,
            IClusterConnectionPoolManager poolManager, IDiscovery discovery = null,
            IInitialServerAddressProvider addressProvider = null,
            IDriverLogger logger = null)
        {
            if (addressProvider == null)
            {
                addressProvider = new InitialServerAddressProvider(InitialUri, new PassThroughServerAddressResolver());
            }

            if (discovery == null)
            {
                discovery = Mock.Of<IDiscovery>();
            }

            return new RoutingTableManager(addressProvider, discovery, routingTable, poolManager, logger);
        }

        internal static Mock<IRoutingTable> NewMockedRoutingTable(AccessMode mode, Uri uri)
        {
            var mock = new Mock<IRoutingTable>();
            mock.Setup(m => m.IsStale(It.IsAny<AccessMode>())).Returns(false);
            var list = new List<Uri>();
            if (uri != null)
            {
                list.Add(uri);
            }

            switch (mode)
            {
                case AccessMode.Read:
                    mock.Setup(m => m.Readers).Returns(list);
                    break;
                case AccessMode.Write:
                    mock.Setup(m => m.Writers).Returns(list);
                    break;
                default:
                    throw new InvalidOperationException($"unknown access mode {mode}");
            }

            mock.Setup(m => m.Remove(It.IsAny<Uri>())).Callback<Uri>(u => list.Remove(u));
            return mock;
        }

        internal static IRoutingTable NewRoutingTable(
            IEnumerable<Uri> routers = null,
            IEnumerable<Uri> readers = null,
            IEnumerable<Uri> writers = null)
        {
            // assign default value of uri
            if (routers == null)
            {
                routers = new Uri[0];
            }

            if (readers == null)
            {
                readers = new Uri[0];
            }

            if (writers == null)
            {
                writers = new Uri[0];
            }

            return new RoutingTable(routers, readers, writers, 1000);
        }

        public class UpdateRoutingTableWithInitialUriFallbackMethod
        {
            [Fact]
            public async Task ShouldPrependInitialRouterIfWriterIsAbsent()
            {
                // Given
                var routers = new List<Uri>();
                var routingTableMock = new Mock<IRoutingTable>();
                routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(InitialUri));
                routingTableMock.Setup(x => x.Routers).Returns(routers);

                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(InitialUri));

                var manager = NewRoutingTableManager(routingTableMock.Object, poolManagerMock.Object);
                manager.IsReadingInAbsenceOfWriter = true;

                // When
                // should throw an exception as the initial routers should not be tried again
                var exception = await Record.ExceptionAsync(() =>
                    manager.UpdateRoutingTableWithInitialUriFallbackAsync());
                exception.Should().BeOfType<ServiceUnavailableException>();

                // Then
                poolManagerMock.Verify(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                routingTableMock.Verify(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()), Times.Once);
            }

            [Fact]
            public async Task ShouldAddInitialUriWhenNoAvailableRouters()
            {
                // Given
                var routers = new List<Uri>();
                var routingTableMock = new Mock<IRoutingTable>();
                routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                    .Callback<IEnumerable<Uri>>(r => { routers.AddRange(r); });
                routingTableMock.Setup(x => x.Routers).Returns(routers);

                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(InitialUri));
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(InitialUri))
                    .ReturnsAsync(Mock.Of<IConnection>());

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>()))
                    .ReturnsAsync(Mock.Of<IRoutingTable>());

                var manager = NewRoutingTableManager(routingTableMock.Object, poolManagerMock.Object, discovery.Object);

                // When
                await manager.UpdateRoutingTableWithInitialUriFallbackAsync();

                // Then
                poolManagerMock.Verify(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                routingTableMock.Verify(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()), Times.Once);
            }

            [Fact]
            public async Task ShouldNotTryInitialUriIfAlreadyTried()
            {
                // Given
                var a = new Uri("bolt+routing://123:456");
                var b = new Uri("bolt+routing://123:789");
                var s = a; // should not be retried
                var t = new Uri("bolt+routing://222:123"); // this should be retried

                var routers = new List<Uri>(new[] {a, b});
                var routingTableMock = new Mock<IRoutingTable>();
                routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                    .Callback<IEnumerable<Uri>>(r => { routers.AddRange(r); });
                routingTableMock.Setup(x => x.Routers).Returns(routers);

                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(t));
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(t)).ReturnsAsync(Mock.Of<IConnection>());

                var initialUriSet = new HashSet<Uri> {s, t};
                var mockProvider = new Mock<IInitialServerAddressProvider>();
                mockProvider.Setup(x => x.Get()).Returns(initialUriSet);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>()))
                    .ReturnsAsync(Mock.Of<IRoutingTable>());

                var manager =
                    NewRoutingTableManager(routingTableMock.Object, poolManagerMock.Object, discovery.Object,
                        mockProvider.Object);

                // When
                await manager.UpdateRoutingTableWithInitialUriFallbackAsync();

                // Then
                // verify the method is actually called
                poolManagerMock.Verify(x => x.AddConnectionPoolAsync(new[] {t}), Times.Once);
                routingTableMock.Verify(x => x.PrependRouters(new[] {t}), Times.Once);
            }
        }

        public class UpdateRoutingTableMethod
        {
            [Fact]
            public async Task ShouldForgetAndTryNextRouterWhenConnectionIsNull()
            {
                // Given
                var uriA = new Uri("bolt+routing://123:456");
                var uriB = new Uri("bolt+routing://123:789");

                // This ensures that uri and uri2 will return in order
                var routingTable = new RoutingTable(new List<Uri> {uriA, uriB});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync((ClusterConnection) null);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>()))
                    .Throws<NotSupportedException>();

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object);

                // When
                var newRoutingTable = await manager.UpdateRoutingTableAsync(null);

                // Then
                newRoutingTable.Should().BeNull();
                routingTable.All().Should().BeEmpty();
            }

            [Fact]
            public async Task ShouldForgetAndTryNextRouterWhenFailedWithConnectionError()
            {
                // Given
                var uriA = new Uri("bolt+routing://123:456");
                var uriB = new Uri("bolt+routing://123:789");
                var connA = new Mock<IConnection>().Object;
                var connB = new Mock<IConnection>().Object;

                // This ensures that uri and uri2 will return in order
                var routingTable = new RoutingTable(new List<Uri> {uriA, uriB});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.SetupSequence(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(connA).ReturnsAsync(connB);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>())).Callback((IConnection c) =>
                    throw new NotSupportedException($"Unknown uri: {c.Server.Address}"));
                discovery.Setup(x => x.DiscoverAsync(connA)).Callback((IConnection c) => routingTable.Remove(uriA))
                    .Throws(new SessionExpiredException("failed init"));
                discovery.Setup(x => x.DiscoverAsync(connB))
                    .ReturnsAsync(NewRoutingTable(new[] {uriA}, new[] {uriA}, new[] {uriA}));

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object);

                // When
                var newRoutingTable = await manager.UpdateRoutingTableAsync(null);

                // Then
                newRoutingTable.All().Should().ContainInOrder(uriA);
                routingTable.All().Should().ContainInOrder(uriB);
            }

            [Fact]
            public async Task ShouldLogServiceUnavailable()
            {
                var error = new ServiceUnavailableException("Procedure not found");
                var uri = new Uri("bolt+routing://123:456");
                var routingTable = new RoutingTable(new List<Uri> {uri});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(uri))
                    .Returns(Task.FromResult(new Mock<IConnection>().Object));

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>())).Throws(error);

                var logger = new Mock<IDriverLogger>();
                logger.Setup(x => x.Warn(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()));

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object,
                    logger: logger.Object);

                await manager.UpdateRoutingTableAsync();

                logger.Verify(x => x.Warn(error, It.IsAny<string>(), It.IsAny<object[]>()));
            }

            [Fact]
            public async Task ShouldPropagateSecurityException()
            {
                var error = new AuthenticationException("Procedure not found");
                var uri = new Uri("bolt+routing://123:456");
                var routingTable = new RoutingTable(new List<Uri> {uri});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(uri))
                    .ReturnsAsync(new Mock<IConnection>().Object);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>())).Throws(error);

                var logger = new Mock<IDriverLogger>();
                logger.Setup(x => x.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()));

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object,
                    logger: logger.Object);

                var exc = await Record.ExceptionAsync(() => manager.UpdateRoutingTableAsync());

                exc.Should().Be(error);
                logger.Verify(x => x.Error(error, It.IsAny<string>(), It.IsAny<object[]>()));
            }

            [Fact]
            public async Task ShouldTryNextRouterIfNoReader()
            {
                // Given
                var uriA = new Uri("bolt+routing://123:1");
                var uriB = new Uri("bolt+routing://123:2");
                var connA = new Mock<IConnection>().Object;
                var connB = new Mock<IConnection>().Object;

                var uriX = new Uri("bolt+routing://456:1");
                var uriY = new Uri("bolt+routing://789:2");

                var routingTable = new RoutingTable(new List<Uri> {uriA, uriB});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.SetupSequence(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(connA).ReturnsAsync(connB);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>())).Callback((IConnection c) =>
                    throw new NotSupportedException($"Unknown uri: {c.Server.Address}"));
                discovery.Setup(x => x.DiscoverAsync(connA))
                    .ReturnsAsync(NewRoutingTable(new[] {uriX}, new Uri[0], new[] {uriX}));
                discovery.Setup(x => x.DiscoverAsync(connB))
                    .ReturnsAsync(NewRoutingTable(new[] {uriY}, new[] {uriY}, new[] {uriY}));


                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object);


                // When
                var updateRoutingTable = await manager.UpdateRoutingTableAsync(null);

                // Then
                updateRoutingTable.All().Should().ContainInOrder(uriY);
                manager.IsReadingInAbsenceOfWriter.Should().BeFalse();
            }

            [Fact]
            public async Task ShouldAcceptRoutingTableIfNoWriter()
            {
                // Given
                var uriA = new Uri("bolt+routing://123:1");
                var connA = new Mock<IConnection>().Object;
                var uriX = new Uri("bolt+routing://456:1");

                var routingTable = new RoutingTable(new List<Uri> {uriA});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(connA);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>())).Callback((IConnection c) =>
                    throw new NotSupportedException($"Unknown uri: {c.Server?.Address}"));
                discovery.Setup(x => x.DiscoverAsync(connA))
                    .ReturnsAsync(NewRoutingTable(new[] {uriX}, new[] {uriX}));

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object);

                // When
                var updateRoutingTable = await manager.UpdateRoutingTableAsync(null);

                // Then
                updateRoutingTable.All().Should().ContainInOrder(uriX);
                manager.IsReadingInAbsenceOfWriter.Should().BeTrue();
            }

            [Fact]
            public async Task ShouldTryNextRouterOnDiscoveryError()
            {
                // Given
                var uriA = new Uri("bolt://server1");
                var uriB = new Uri("bolt://server2");
                var uriC = new Uri("bolt://server3");
                var uriD = new Uri("bolt://server4");
                var uriE = new Uri("bolt://server5");

                var connE = Mock.Of<IConnection>();

                var routingTable = Mock.Of<IRoutingTable>();

                var poolManager = new Mock<IClusterConnectionPoolManager>();
                poolManager.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(Mock.Of<IConnection>());
                poolManager.Setup(x => x.CreateClusterConnectionAsync(uriE)).ReturnsAsync(connE);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>()))
                    .Throws(new ServiceUnavailableException("something went wrong"));
                discovery.Setup(x => x.DiscoverAsync(connE)).ReturnsAsync(routingTable);

                var logger = new Mock<IDriverLogger>();
                logger.Setup(x =>
                    x.Warn(It.IsAny<ServiceUnavailableException>(), It.IsAny<string>(), It.IsAny<object[]>()));

                var manager =
                    NewRoutingTableManager(new RoutingTable(new[] {uriA, uriB, uriC, uriD, uriE}), poolManager.Object,
                        discovery.Object, logger: logger.Object);

                // When
                var result = await manager.UpdateRoutingTableAsync();

                // Then
                result.Should().Be(routingTable);
                discovery.Verify(x => x.DiscoverAsync(It.Is<IConnection>(c => c != connE)), Times.Exactly(4));
                discovery.Verify(x => x.DiscoverAsync(connE), Times.Once);
                logger.Verify(
                    x => x.Warn(It.IsAny<ServiceUnavailableException>(), It.IsAny<string>(), It.IsAny<object[]>()),
                    Times.Exactly(4));
            }

            [Fact]
            public async Task ShouldTryNextRouterOnConnectionError()
            {
                // Given
                var uriA = new Uri("bolt://server1");
                var uriB = new Uri("bolt://server2");
                var uriC = new Uri("bolt://server3");
                var uriD = new Uri("bolt://server4");
                var uriE = new Uri("bolt://server5");

                var connE = Mock.Of<IConnection>();

                var routingTable = Mock.Of<IRoutingTable>();

                var poolManager = new Mock<IClusterConnectionPoolManager>();
                poolManager.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .Throws(new ClientException("timed out"));
                poolManager.Setup(x => x.CreateClusterConnectionAsync(uriE)).ReturnsAsync(connE);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>()))
                    .Throws(new ServiceUnavailableException("something went wrong"));
                discovery.Setup(x => x.DiscoverAsync(connE)).ReturnsAsync(routingTable);

                var logger = new Mock<IDriverLogger>();
                logger.Setup(x =>
                    x.Warn(It.IsAny<ServiceUnavailableException>(), It.IsAny<string>(), It.IsAny<object[]>()));

                var manager =
                    NewRoutingTableManager(new RoutingTable(new[] {uriA, uriB, uriC, uriD, uriE}), poolManager.Object,
                        discovery.Object, logger: logger.Object);

                // When
                var result = await manager.UpdateRoutingTableAsync();

                // Then
                result.Should().Be(routingTable);
                discovery.Verify(x => x.DiscoverAsync(It.Is<IConnection>(c => c != connE)), Times.Never);
                discovery.Verify(x => x.DiscoverAsync(connE), Times.Once);
                logger.Verify(
                    x => x.Warn(It.IsAny<ClientException>(), It.IsAny<string>(), It.IsAny<object[]>()),
                    Times.Exactly(4));
            }
        }
    }
}