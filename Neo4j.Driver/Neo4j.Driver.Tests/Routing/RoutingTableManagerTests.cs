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
            IClusterConnectionPoolManager poolManager, IInitialServerAddressProvider addressProvider = null)
        {
            if (addressProvider == null)
            {
                addressProvider = new InitialServerAddressProvider(InitialUri, new PassThroughServerAddressResolver());
            }

            return new RoutingTableManager(addressProvider, new Dictionary<string, string>(), routingTable, poolManager,
                new SyncExecutor(), null);
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
                var routingTableMock = new Mock<IRoutingTable>();
                routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(InitialUri));

                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()))
                    .Returns(TaskHelper.GetCompletedTask())
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(InitialUri));

                var manager = NewRoutingTableManager(routingTableMock.Object, poolManagerMock.Object);
                manager.IsReadingInAbsenceOfWriter = true;
                var routingTableReturnMock = new Mock<IRoutingTable>();

                // When
                // should throw an exception as the initial routers should not be tried again
                var exception = await Record.ExceptionAsync(() =>
                    manager.UpdateRoutingTableWithInitialUriFallbackAsync(c => Task.FromResult(c != null
                        ? null
                        : routingTableReturnMock.Object)));
                exception.Should().BeOfType<ServiceUnavailableException>();

                // Then
                poolManagerMock.Verify(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                routingTableMock.Verify(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()), Times.Once);
            }

            [Fact]
            public async Task ShouldAddInitialUriWhenNoAvailableRouters()
            {
                // Given
                var routingTableMock = new Mock<IRoutingTable>();
                routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(InitialUri));

                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()))
                    .Returns(TaskHelper.GetCompletedTask())
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(InitialUri));

                var manager = NewRoutingTableManager(routingTableMock.Object, poolManagerMock.Object);
                var routingTableReturnMock = new Mock<IRoutingTable>();

                // When
                await manager.UpdateRoutingTableWithInitialUriFallbackAsync(c => Task.FromResult(c != null
                    ? null
                    : routingTableReturnMock.Object));

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

                var routingTableMock = new Mock<IRoutingTable>();
                routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(t));

                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()))
                    .Returns(TaskHelper.GetCompletedTask())
                    .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(t));

                var initialUriSet = new HashSet<Uri> {s, t};
                var mockProvider = new Mock<IInitialServerAddressProvider>();
                mockProvider.Setup(x => x.Get()).Returns(initialUriSet);

                var manager =
                    NewRoutingTableManager(routingTableMock.Object, poolManagerMock.Object, mockProvider.Object);

                Task<IRoutingTable> UpdateRoutingTableFunc(ISet<Uri> set)
                {
                    if (set != null)
                    {
                        set.Add(a);
                        set.Add(b);
                        return Task.FromResult((IRoutingTable) null);
                    }

                    return Task.FromResult(new Mock<IRoutingTable>().Object);
                }

                // When

                await manager.UpdateRoutingTableWithInitialUriFallbackAsync(UpdateRoutingTableFunc);

                // Then
                // verify the method is actually called
                poolManagerMock.Verify(x => x.AddConnectionPoolAsync(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                routingTableMock.Verify(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()), Times.Once);
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
                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object);

                // When
                var newRoutingTable = await manager.UpdateRoutingTableAsync(null, connection =>
                    throw new NotSupportedException($"Unknown uri: {connection.Server.Address}"));

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
                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object);

                // When
                var newRoutingTable = await manager.UpdateRoutingTableAsync(null, connection =>
                {
                    // the second connection will give a new routingTable
                    if (connection.Equals(connA)) // uriA
                    {
                        routingTable.Remove(uriA);
                        return TaskHelper.GetFailedTask<IRoutingTable>(new SessionExpiredException("failed init"));
                    }

                    if (connection.Equals(connB)) // uriB
                    {
                        return Task.FromResult(NewRoutingTable(new[] {uriA}, new[] {uriA}, new[] {uriA}));
                    }

                    return TaskHelper.GetFailedTask<IRoutingTable>(
                        new NotSupportedException($"Unknown uri: {connection.Server.Address}"));
                });

                // Then
                newRoutingTable.All().Should().ContainInOrder(uriA);
                routingTable.All().Should().ContainInOrder(uriB);
            }

            [Fact]
            public async Task ShouldPropagateServiceUnavailable()
            {
                var uri = new Uri("bolt+routing://123:456");
                var routingTable = new RoutingTable(new List<Uri> {uri});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(uri))
                    .Returns(Task.FromResult(new Mock<IConnection>().Object));

                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object);

                var exception = await Record.ExceptionAsync(() => manager.UpdateRoutingTableAsync(null,
                    conn => throw new ServiceUnavailableException("Procedure not found")));

                exception.Should().BeOfType<ServiceUnavailableException>();
                exception.Message.Should().Be("Procedure not found");
            }


            [Fact]
            public async Task ShouldPropagateProtocolError()
            {
                var uri = new Uri("bolt+routing://123:456");
                var routingTable = new RoutingTable(new List<Uri> {uri});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(uri))
                    .Returns(Task.FromResult(new Mock<IConnection>().Object));
                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object);

                var exception = await Record.ExceptionAsync(() =>
                    manager.UpdateRoutingTableAsync(null,
                        conn => throw new ProtocolException("Cannot parse procedure result")));

                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Be("Cannot parse procedure result");
            }

            [Fact]
            public async Task ShouldPropagateAuthenticationException()
            {
                // Given
                var uri = new Uri("bolt+routing://123:456");
                var routingTable = new RoutingTable(new List<Uri> {uri});
                var poolManagerMock = new Mock<IClusterConnectionPoolManager>();
                poolManagerMock.Setup(x => x.CreateClusterConnectionAsync(uri))
                    .Returns(TaskHelper.GetFailedTask<IConnection>(
                        new AuthenticationException("Failed to auth the client to the server.")));
                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object);

                // When
                var error = await Record.ExceptionAsync(() => manager.UpdateRoutingTableAsync());

                // Then
                error.Should().BeOfType<AuthenticationException>();
                error.Message.Should().Contain("Failed to auth the client to the server.");

                // while the server is not removed
                routingTable.All().Should().ContainInOrder(uri);
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
                    .Returns(Task.FromResult(connA)).Returns(Task.FromResult(connB));
                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object);


                // When
                var updateRoutingTable = await manager.UpdateRoutingTableAsync(null, conn =>
                {
                    if (conn.Equals(connA))
                    {
                        return Task.FromResult(NewRoutingTable(new[] {uriX}, new Uri[0], new[] {uriX}));
                    }

                    if (conn.Equals(connB))
                    {
                        return Task.FromResult(NewRoutingTable(new[] {uriY}, new[] {uriY}, new[] {uriY}));
                    }

                    return TaskHelper.GetFailedTask<IRoutingTable>(
                        new NotSupportedException($"Unknown uri: {conn.Server.Address}"));
                });

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
                    .Returns(Task.FromResult(connA));
                var manager = NewRoutingTableManager(routingTable, poolManagerMock.Object);

                // When
                var updateRoutingTable = await manager.UpdateRoutingTableAsync(null, conn =>
                {
                    if (conn.Equals(connA))
                    {
                        return Task.FromResult(NewRoutingTable(new[] {uriX}, new[] {uriX}));
                    }

                    return TaskHelper.GetFailedTask<IRoutingTable>(new NotSupportedException($"Unknown uri: {conn.Server.Address}"));
                });

                // Then
                updateRoutingTable.All().Should().ContainInOrder(uriX);
                manager.IsReadingInAbsenceOfWriter.Should().BeTrue();
            }
        }
    }
}