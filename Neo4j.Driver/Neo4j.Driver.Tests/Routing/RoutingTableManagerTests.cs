﻿// Copyright (c) "Neo4j"
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
using System.Configuration;
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
    public static class RoutingTableManagerTests
    {
        private static readonly Uri server01 = new Uri("bolt://server-01");
        private static readonly Uri server02 = new Uri("bolt://server-02");
        private static readonly Uri server03 = new Uri("bolt://server-03");
        private static readonly Uri server04 = new Uri("bolt://server-04");
        private static readonly Uri server05 = new Uri("bolt://server-05");
        private static readonly Uri server06 = new Uri("bolt://server-06");
        private static readonly Uri server07 = new Uri("bolt://server-07");
        private static readonly Uri server08 = new Uri("bolt://server-08");
        private static readonly Uri server09 = new Uri("bolt://server-09");

        internal static Uri InitialUri = new Uri("neo4j://neo4j.com:6060");

        internal static RoutingTableManager NewRoutingTableManager(
            IRoutingTable routingTable,
            IClusterConnectionPoolManager poolManager, IDiscovery discovery = null,
            IInitialServerAddressProvider addressProvider = null,
            ILogger logger = null)
        {
            if (addressProvider == null)
            {
                addressProvider = new InitialServerAddressProvider(InitialUri, new PassThroughServerAddressResolver());
            }

            if (discovery == null)
            {
                discovery = Mock.Of<IDiscovery>();
            }

            return new RoutingTableManager(addressProvider, discovery, poolManager, logger, TimeSpan.Zero,
                routingTable);
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
            IEnumerable<Uri> writers = null,
            string database = null)
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

            return new RoutingTable(database, routers, readers, writers, 1000);
        }

        public class UpdateRoutingTableWithInitialUriFallbackMethod
        {
            [Fact]
            public void ShouldPrependInitialRouterIfWriterIsAbsent()
            {
                // Given
                var routers = new List<Uri>();
                var routingTableMock = new Mock<IRoutingTable>();
                routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(InitialUri));
                routingTableMock.Setup(x => x.Routers).Returns(routers);
                routingTableMock.Setup(x => x.Database).Returns("");
                routingTableMock.Setup(x => x.IsReadingInAbsenceOfWriter(AccessMode.Read)).Returns(true);

                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(InitialUri));

                var manager = NewRoutingTableManager(routingTableMock.Object, poolManagerMock.Object);

                // When
                // should throw an exception as the initial routers should not be tried again
                manager.Awaiting(m =>
                        m.UpdateRoutingTableAsync(AccessMode.Read, "", Bookmark.Empty)).Should()
                    .Throw<ServiceUnavailableException>();

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
                routingTableMock.Setup(x => x.Database).Returns("");

                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(InitialUri));
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(InitialUri))
                    .ReturnsAsync(Mock.Of<IConnection>());

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty))
                    .ReturnsAsync(Mock.Of<IRoutingTable>());

                var manager = NewRoutingTableManager(routingTableMock.Object, poolManagerMock.Object, discovery.Object);

                // When
                await manager.UpdateRoutingTableAsync(AccessMode.Read, "", Bookmark.Empty);

                // Then
                poolManagerMock.Verify(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                routingTableMock.Verify(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()), Times.Once);
            }

            [Fact]
            public async Task ShouldNotTryInitialUriIfAlreadyTried()
            {
                // Given
                var a = new Uri("neo4j://123:456");
                var b = new Uri("neo4j://123:789");
                var s = a; // should not be retried
                var t = new Uri("neo4j://222:123"); // this should be retried

                var routers = new List<Uri>(new[] {a, b});
                var routingTableMock = new Mock<IRoutingTable>();
                routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                    .Callback<IEnumerable<Uri>>(r => { routers.AddRange(r); });
                routingTableMock.Setup(x => x.Routers).Returns(routers);
                routingTableMock.Setup(x => x.Database).Returns("");

                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(t));
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(t)).ReturnsAsync(Mock.Of<IConnection>());

                var initialUriSet = new HashSet<Uri> {s, t};
                var mockProvider = new Mock<IInitialServerAddressProvider>();
                mockProvider.Setup(x => x.Get()).Returns(initialUriSet);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty))
                    .ReturnsAsync(Mock.Of<IRoutingTable>());

                var manager =
                    NewRoutingTableManager(routingTableMock.Object, poolManagerMock.Object, discovery.Object,
                        mockProvider.Object);

                // When
                await manager.UpdateRoutingTableAsync(AccessMode.Read, "", Bookmark.Empty);

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
                var uriA = new Uri("neo4j://123:456");
                var uriB = new Uri("neo4j://123:789");

                // This ensures that uri and uri2 will return in order
                var routingTable = new RoutingTable(null, new List<Uri> {uriA, uriB});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync((ClusterConnection) null);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty))
                    .Throws<NotSupportedException>();

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object);

                // When
                var newRoutingTable =
                    await manager.UpdateRoutingTableAsync(routingTable, AccessMode.Read, "", Bookmark.Empty);

                // Then
                newRoutingTable.Should().BeNull();
                routingTable.All().Should().BeEmpty();
            }

            [Fact]
            public async Task ShouldForgetAndTryNextRouterWhenFailedWithConnectionError()
            {
                // Given
                var uriA = new Uri("neo4j://123:456");
                var uriB = new Uri("neo4j://123:789");
                var connA = new Mock<IConnection>().Object;
                var connB = new Mock<IConnection>().Object;

                // This ensures that uri and uri2 will return in order
                var routingTable = new RoutingTable(null, new List<Uri> {uriA, uriB});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.SetupSequence(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(connA).ReturnsAsync(connB);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), null, Bookmark.Empty)).Callback(
                    (IConnection c, string database, Bookmark bookmark) =>
                        throw new NotSupportedException($"Unknown uri: {c.Server.Address}"));
                discovery.Setup(x => x.DiscoverAsync(connA, "", Bookmark.Empty))
                    .Callback((IConnection c, string database, Bookmark bookmark) => routingTable.Remove(uriA))
                    .Throws(new SessionExpiredException("failed init"));
                discovery.Setup(x => x.DiscoverAsync(connB, "", Bookmark.Empty))
                    .ReturnsAsync(NewRoutingTable(new[] {uriA}, new[] {uriA}, new[] {uriA}));

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object);

                // When
                var newRoutingTable =
                    await manager.UpdateRoutingTableAsync(routingTable, AccessMode.Read, "", Bookmark.Empty);

                // Then
                newRoutingTable.All().Should().ContainInOrder(uriA);
                routingTable.All().Should().ContainInOrder(uriB);
            }

            [Fact]
            public async Task ShouldLogServiceUnavailable()
            {
                var error = new ServiceUnavailableException("Procedure not found");
                var uri = new Uri("neo4j://123:456");
                var routingTable = new RoutingTable(null, new List<Uri> {uri});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(uri))
                    .Returns(Task.FromResult(new Mock<IConnection>().Object));

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty)).Throws(error);

                var logger = new Mock<ILogger>();
                logger.Setup(x => x.Warn(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()));

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object,
                    logger: logger.Object);

                await manager.UpdateRoutingTableAsync(routingTable, AccessMode.Read, "", Bookmark.Empty);

                logger.Verify(x => x.Warn(error, It.IsAny<string>(), It.IsAny<object[]>()));
            }

            [Fact]
            public async Task ShouldPropagateSecurityException()
            {
                var error = new AuthenticationException("Procedure not found");
                var uri = new Uri("neo4j://123:456");
                var routingTable = new RoutingTable(null, new List<Uri> {uri});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(uri))
                    .ReturnsAsync(new Mock<IConnection>().Object);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty)).Throws(error);

                var logger = new Mock<ILogger>();
                logger.Setup(x => x.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()));

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object,
                    logger: logger.Object);

                var exc = await Record.ExceptionAsync(() =>
                    manager.UpdateRoutingTableAsync(routingTable, AccessMode.Read, "", Bookmark.Empty));

                exc.Should().Be(error);
                logger.Verify(x => x.Error(error, It.IsAny<string>(), It.IsAny<object[]>()));
            }

            [Fact]
            public async Task ShouldTryNextRouterIfNoReader()
            {
                // Given
                var uriA = new Uri("neo4j://123:1");
                var uriB = new Uri("neo4j://123:2");
                var connA = new Mock<IConnection>().Object;
                var connB = new Mock<IConnection>().Object;

                var uriX = new Uri("neo4j://456:1");
                var uriY = new Uri("neo4j://789:2");

                var routingTable = new RoutingTable(null, new List<Uri> {uriA, uriB});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.SetupSequence(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(connA).ReturnsAsync(connB);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty)).Callback(
                    (IConnection c, string database, Bookmark bookmark) =>
                        throw new NotSupportedException($"Unknown uri: {c.Server.Address}"));
                discovery.Setup(x => x.DiscoverAsync(connA, "", Bookmark.Empty))
                    .ReturnsAsync(NewRoutingTable(new[] {uriX}, new Uri[0], new[] {uriX}));
                discovery.Setup(x => x.DiscoverAsync(connB, "", Bookmark.Empty))
                    .ReturnsAsync(NewRoutingTable(new[] {uriY}, new[] {uriY}, new[] {uriY}));


                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object);


                // When
                var updateRoutingTable =
                    await manager.UpdateRoutingTableAsync(routingTable, AccessMode.Read, "", Bookmark.Empty, null);

                // Then
                updateRoutingTable.All().Should().ContainInOrder(uriY);
                updateRoutingTable.IsReadingInAbsenceOfWriter(AccessMode.Read).Should().BeFalse();
            }

            [Fact]
            public async Task ShouldAcceptRoutingTableIfNoWriter()
            {
                // Given
                var uriA = new Uri("neo4j://123:1");
                var connA = new Mock<IConnection>().Object;
                var uriX = new Uri("neo4j://456:1");

                var routingTable = new RoutingTable(null, new List<Uri> {uriA});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(connA);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty)).Callback(
                    (IConnection c, string database, Bookmark bookmark) =>
                        throw new NotSupportedException($"Unknown uri: {c.Server?.Address}"));
                discovery.Setup(x => x.DiscoverAsync(connA, "", Bookmark.Empty))
                    .ReturnsAsync(NewRoutingTable(new[] {uriX}, new[] {uriX}));

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object, discovery.Object);

                // When
                var updateRoutingTable =
                    await manager.UpdateRoutingTableAsync(routingTable, AccessMode.Read, "", Bookmark.Empty, null);

                // Then
                updateRoutingTable.All().Should().ContainInOrder(uriX);
                updateRoutingTable.IsReadingInAbsenceOfWriter(AccessMode.Read).Should().BeTrue();
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
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty))
                    .Throws(new ServiceUnavailableException("something went wrong"));
                discovery.Setup(x => x.DiscoverAsync(connE, "", Bookmark.Empty)).ReturnsAsync(routingTable);

                var logger = new Mock<ILogger>();
                logger.Setup(x =>
                    x.Warn(It.IsAny<ServiceUnavailableException>(), It.IsAny<string>(), It.IsAny<object[]>()));

                var existingRoutingTable = new RoutingTable(null, new[] {uriA, uriB, uriC, uriD, uriE});
                var manager =
                    NewRoutingTableManager(existingRoutingTable,
                        poolManager.Object, discovery.Object, logger: logger.Object);

                // When
                var result =
                    await manager.UpdateRoutingTableAsync(existingRoutingTable, AccessMode.Read, "", Bookmark.Empty);

                // Then
                result.Should().Be(routingTable);
                discovery.Verify(x => x.DiscoverAsync(It.Is<IConnection>(c => c != connE), "", Bookmark.Empty),
                    Times.Exactly(4));
                discovery.Verify(x => x.DiscoverAsync(connE, "", Bookmark.Empty), Times.Once);
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
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty))
                    .Throws(new ServiceUnavailableException("something went wrong"));
                discovery.Setup(x => x.DiscoverAsync(connE, "", Bookmark.Empty)).ReturnsAsync(routingTable);

                var logger = new Mock<ILogger>();
                logger.Setup(x =>
                    x.Warn(It.IsAny<ServiceUnavailableException>(), It.IsAny<string>(), It.IsAny<object[]>()));

                var existingRoutingTable = new RoutingTable(null, new[] {uriA, uriB, uriC, uriD, uriE});
                var manager =
                    NewRoutingTableManager(existingRoutingTable, poolManager.Object, discovery.Object,
                        logger: logger.Object);

                // When
                var result =
                    await manager.UpdateRoutingTableAsync(existingRoutingTable, AccessMode.Read, "", Bookmark.Empty);

                // Then
                result.Should().Be(routingTable);
                discovery.Verify(x => x.DiscoverAsync(It.Is<IConnection>(c => c != connE), "", Bookmark.Empty),
                    Times.Never);
                discovery.Verify(x => x.DiscoverAsync(connE, "", Bookmark.Empty), Times.Once);
                logger.Verify(
                    x => x.Warn(It.IsAny<ClientException>(), It.IsAny<string>(), It.IsAny<object[]>()),
                    Times.Exactly(4));
            }
        }

        public class ForgetServer
        {
            [Fact]
            public void ShouldRemoveFromCorrectRoutingTable()
            {
                var defaultRoutingTable =
                    new RoutingTable(null, new[] {server01}, new[] {server02, server05}, new[] {server03}, 60);
                var fooRoutingTable =
                    new RoutingTable("foo", new[] {server04, server05}, new[] {server05}, new[] {server06}, 80);
                var barRoutingTable =
                    new RoutingTable("bar", new[] {server07}, new[] {server08}, new[] {server09, server05}, 100);

                var manager = new RoutingTableManager(Mock.Of<IInitialServerAddressProvider>(), Mock.Of<IDiscovery>(),
                    Mock.Of<IClusterConnectionPoolManager>(), Mock.Of<ILogger>(), TimeSpan.MaxValue,
                    defaultRoutingTable, fooRoutingTable, barRoutingTable);

                manager.ForgetServer(server05, "foo");

                manager.RoutingTableFor("foo").Routers.Should().BeEquivalentTo(server04);
                manager.RoutingTableFor("foo").Readers.Should().BeEmpty();
                manager.RoutingTableFor("foo").Writers.Should().BeEquivalentTo(server06);

                manager.RoutingTableFor("").Routers.Should().BeEquivalentTo(server01);
                manager.RoutingTableFor("").Readers.Should().BeEquivalentTo(server02, server05);
                manager.RoutingTableFor("").Writers.Should().BeEquivalentTo(server03);

                manager.RoutingTableFor("bar").Routers.Should().BeEquivalentTo(server07);
                manager.RoutingTableFor("bar").Readers.Should().BeEquivalentTo(server08);
                manager.RoutingTableFor("bar").Writers.Should().BeEquivalentTo(server09, server05);
            }

            [Fact]
            public void ShouldRemoveWriterFromCorrectRoutingTable()
            {
                var defaultRoutingTable =
                    new RoutingTable(null, new[] {server01}, new[] {server02, server05}, new[] {server03}, 60);
                var fooRoutingTable =
                    new RoutingTable("foo", new[] {server04, server06}, new[] {server05}, new[] {server06, server04},
                        80);
                var barRoutingTable =
                    new RoutingTable("bar", new[] {server07}, new[] {server08}, new[] {server09, server05}, 100);

                var manager = new RoutingTableManager(Mock.Of<IInitialServerAddressProvider>(), Mock.Of<IDiscovery>(),
                    Mock.Of<IClusterConnectionPoolManager>(), Mock.Of<ILogger>(), TimeSpan.MaxValue,
                    defaultRoutingTable, fooRoutingTable, barRoutingTable);

                manager.ForgetWriter(server06, "foo");

                manager.RoutingTableFor("foo").Routers.Should().BeEquivalentTo(server04, server06);
                manager.RoutingTableFor("foo").Readers.Should().BeEquivalentTo(server05);
                manager.RoutingTableFor("foo").Writers.Should().BeEquivalentTo(server04);

                manager.RoutingTableFor("").Routers.Should().BeEquivalentTo(server01);
                manager.RoutingTableFor("").Readers.Should().BeEquivalentTo(server02, server05);
                manager.RoutingTableFor("").Writers.Should().BeEquivalentTo(server03);

                manager.RoutingTableFor("bar").Routers.Should().BeEquivalentTo(server07);
                manager.RoutingTableFor("bar").Readers.Should().BeEquivalentTo(server08);
                manager.RoutingTableFor("bar").Writers.Should().BeEquivalentTo(server09, server05);
            }
        }

        public class MultiDatabase
        {
            [Theory]
            [InlineData(AccessMode.Read)]
            [InlineData(AccessMode.Write)]
            public async Task ShouldAllowMultipleRoutingTables(AccessMode mode)
            {
                var defaultRoutingTable =
                    new RoutingTable(null, new[] {server01}, new[] {server02}, new[] {server03}, 60);
                var fooRoutingTable =
                    new RoutingTable("foo", new[] {server04}, new[] {server05}, new[] {server06}, 80);
                var barRoutingTable =
                    new RoutingTable("bar", new[] {server07}, new[] {server08}, new[] {server09}, 100);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty))
                    .ReturnsAsync(defaultRoutingTable);
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "foo", Bookmark.Empty))
                    .ReturnsAsync(fooRoutingTable);
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "bar", Bookmark.Empty))
                    .ReturnsAsync(barRoutingTable);

                var poolManager = new Mock<IClusterConnectionPoolManager>();
                poolManager.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(Mock.Of<IConnection>);

                var initialAddressProvider = new Mock<IInitialServerAddressProvider>();
                initialAddressProvider.Setup(x => x.Get()).Returns(new HashSet<Uri> {server01, server04, server07});

                var manager = new RoutingTableManager(initialAddressProvider.Object, discovery.Object,
                    poolManager.Object, Mock.Of<ILogger>(), TimeSpan.MaxValue);

                // When
                var routingTable1 = await manager.EnsureRoutingTableForModeAsync(mode, null, Bookmark.Empty);
                var routingTable2 =
                    await manager.EnsureRoutingTableForModeAsync(mode, "foo", Bookmark.Empty);
                var routingTable3 =
                    await manager.EnsureRoutingTableForModeAsync(mode, "bar", Bookmark.Empty);

                routingTable1.Should().Be(defaultRoutingTable);
                routingTable2.Should().Be(fooRoutingTable);
                routingTable3.Should().Be(barRoutingTable);

                manager.RoutingTableFor(null).Should().Be(defaultRoutingTable);
                manager.RoutingTableFor("").Should().Be(defaultRoutingTable);
                manager.RoutingTableFor("foo").Should().Be(fooRoutingTable);
                manager.RoutingTableFor("bar").Should().Be(barRoutingTable);
            }

            [Fact]
            public async Task ShouldRemoveStaleEntriesOnUpdate()
            {
                var timer = new Mock<ITimer>();
                var fooRoutingTable =
                    new RoutingTable("foo", new[] {server04}, new[] {server05}, new[] {server06}, 1, timer.Object);
                var barRoutingTable =
                    new RoutingTable("bar", new[] {server07}, new[] {server08}, new[] {server09}, 4, timer.Object);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "foo", Bookmark.Empty))
                    .ReturnsAsync(fooRoutingTable);
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "bar", Bookmark.Empty))
                    .ReturnsAsync(barRoutingTable);

                var poolManager = new Mock<IClusterConnectionPoolManager>();
                poolManager.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(Mock.Of<IConnection>);

                var initialAddressProvider = new Mock<IInitialServerAddressProvider>();
                initialAddressProvider.Setup(x => x.Get()).Returns(new HashSet<Uri> {server01, server04, server07});

                var manager = new RoutingTableManager(initialAddressProvider.Object, discovery.Object,
                    poolManager.Object, Mock.Of<ILogger>(), TimeSpan.FromSeconds(1));

                // When
                var routingTable1 =
                    await manager.EnsureRoutingTableForModeAsync(AccessMode.Read, "foo", Bookmark.Empty);
                routingTable1.Should().Be(fooRoutingTable);

                // Fake the timer to make foo routing table to be recognized as stale
                timer.Setup(x => x.ElapsedMilliseconds).Returns(2 * 1000 + 1);

                // An update should trigger an implicit clean-up of stale entries
                var routingTable2 =
                    await manager.EnsureRoutingTableForModeAsync(AccessMode.Read, "bar", Bookmark.Empty);
                routingTable2.Should().Be(barRoutingTable);

                manager.RoutingTableFor("foo").Should().BeNull();
                manager.RoutingTableFor("bar").Should().Be(barRoutingTable);
            }

            [Fact]
            public async Task ShouldIsolateEntriesFromFailures()
            {
                var defaultRoutingTable =
                    new RoutingTable(null, new[] {server01}, new[] {server02}, new[] {server03}, 60);
                var fooRoutingTable =
                    new RoutingTable("foo", new[] {server04}, new[] {server05}, new[] {server06}, 80);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "", Bookmark.Empty))
                    .ReturnsAsync(defaultRoutingTable);
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "foo", Bookmark.Empty))
                    .ReturnsAsync(fooRoutingTable);
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "bar", Bookmark.Empty))
                    .ThrowsAsync(new FatalDiscoveryException("code", "message"));

                var poolManager = new Mock<IClusterConnectionPoolManager>();
                poolManager.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(Mock.Of<IConnection>());

                var initialAddressProvider = new Mock<IInitialServerAddressProvider>();
                initialAddressProvider.Setup(x => x.Get()).Returns(new HashSet<Uri> {server01, server04, server07});

                var manager = new RoutingTableManager(initialAddressProvider.Object, discovery.Object,
                    poolManager.Object, Mock.Of<ILogger>(), TimeSpan.MaxValue);

                // When
                var routingTable1 =
                    await manager.EnsureRoutingTableForModeAsync(AccessMode.Write, null, Bookmark.Empty);
                var routingTable2 =
                    await manager.EnsureRoutingTableForModeAsync(AccessMode.Write, "foo", Bookmark.Empty);
                routingTable1.Should().Be(defaultRoutingTable);
                routingTable2.Should().Be(fooRoutingTable);

                manager.Awaiting(m => m.EnsureRoutingTableForModeAsync(AccessMode.Write, "bar", Bookmark.Empty))
                    .Should().Throw<FatalDiscoveryException>();
                manager.RoutingTableFor("").Should().Be(defaultRoutingTable);
                manager.RoutingTableFor("foo").Should().Be(fooRoutingTable);
            }

            [Fact]
            public void ShouldThrowOnFatalDiscovery()
            {
                var error = new FatalDiscoveryException("code", "message");

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "bar", Bookmark.Empty))
                    .ThrowsAsync(error);

                var poolManager = new Mock<IClusterConnectionPoolManager>();
                poolManager.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(Mock.Of<IConnection>());

                var initialAddressProvider = new Mock<IInitialServerAddressProvider>();
                initialAddressProvider.Setup(x => x.Get()).Returns(new HashSet<Uri> {server01, server04, server07});

                var manager = new RoutingTableManager(initialAddressProvider.Object, discovery.Object,
                    poolManager.Object, Mock.Of<ILogger>(), TimeSpan.MaxValue);

                manager.Awaiting(m => m.EnsureRoutingTableForModeAsync(AccessMode.Write, "bar", Bookmark.Empty))
                    .Should().Throw<FatalDiscoveryException>().Which.Should().Be(error);
            }


            [Fact]
            public async Task ShouldPassBookmarkDownToDiscovery()
            {
                var bookmark = Bookmark.From("bookmark-1", "bookmark-2");
                var rt = new RoutingTable("foo", new[] {server01}, new[] {server02}, new[] {server03}, 10);

                var discovery = new Mock<IDiscovery>();
                discovery.Setup(x => x.DiscoverAsync(It.IsAny<IConnection>(), "foo", bookmark))
                    .ReturnsAsync(rt);

                var poolManager = new Mock<IClusterConnectionPoolManager>();
                poolManager.Setup(x => x.CreateClusterConnectionAsync(It.IsAny<Uri>()))
                    .ReturnsAsync(Mock.Of<IConnection>());

                var initialAddressProvider = new Mock<IInitialServerAddressProvider>();
                initialAddressProvider.Setup(x => x.Get()).Returns(new HashSet<Uri> {server01});

                var manager = new RoutingTableManager(initialAddressProvider.Object, discovery.Object,
                    poolManager.Object, Mock.Of<ILogger>(), TimeSpan.MaxValue);

                var routingTable = await manager.EnsureRoutingTableForModeAsync(AccessMode.Write, "foo", bookmark);

                routingTable.Should().Be(rt);
                discovery.Verify(x => x.DiscoverAsync(It.IsAny<IConnection>(), "foo", bookmark), Times.Once);
            }
        }
    }
}