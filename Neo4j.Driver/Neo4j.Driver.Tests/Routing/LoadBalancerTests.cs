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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Routing
{
    public class LoadBalancerTests
    {
        public class Constructor
        {
            [Fact]
            public void ShouldEnsureInitialRouter()
            {
                var uri = new Uri("bolt://123:456");
                var config = Config.DefaultConfig;
                var routingSettings = new RoutingSettings(new Dictionary<string, string>());
                var connSettings = new ConnectionSettings(uri, new Mock<IAuthToken>().Object, config);
                var poolSettings = new ConnectionPoolSettings(config);

                var loadbalancer = new LoadBalancer(routingSettings, connSettings, poolSettings, null);

                loadbalancer.ToString().Should().Be(
                    "_routingTable: {[_routers: bolt://123:456/], [_detachedRouters: ], [_readers: ], [_writers: ]}, " +
                    "_clusterConnectionPool: {[{bolt://123:456/ : _availableConnections: {[]}, _inUseConnections: {[]}}]}");
            }
        }

        public class AcquireConnectionMethod
        {
            public class UpdateRoutingTableWithInitialUriFallbackMethod
            {
                [Fact]
                public void ShouldPrependInitialRouterIfWriterIsAbsent()
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");

                    var routingTableMock = new Mock<IRoutingTable>();
                    routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                        .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(uri));

                    var poolMock = new Mock<IClusterConnectionPool>();
                    poolMock.Setup(x => x.Add(It.IsAny<IEnumerable<Uri>>()))
                        .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(uri));

                    var balancer = new LoadBalancer(poolMock.Object, routingTableMock.Object, uri);
                    balancer.IsReadingInAbsenceOfWriter = true;
                    var routingTableReturnMock = new Mock<IRoutingTable>();

                    // When
                    // should throw an exception as the initial routers should not be tried again
                    var exception = Record.Exception(()=>
                    balancer.UpdateRoutingTableWithInitialUriFallback(c => c != null ? null : routingTableReturnMock.Object));
                    exception.Should().BeOfType<ServiceUnavailableException>();

                    // Then
                    poolMock.Verify(x => x.Add(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                    routingTableMock.Verify(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                }

                [Fact]
                public void ShouldAddInitialUriWhenNoAvailableRouters()
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");

                    var routingTableMock = new Mock<IRoutingTable>();
                    routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                        .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(uri));

                    var poolMock = new Mock<IClusterConnectionPool>();
                    poolMock.Setup(x => x.Add(It.IsAny<IEnumerable<Uri>>()))
                        .Callback<IEnumerable<Uri>>(r => r.Single().Should().Be(uri));

                    var balancer = new LoadBalancer(poolMock.Object, routingTableMock.Object, uri);

                    var routingTableReturnMock = new Mock<IRoutingTable>();

                    // When
                    balancer.UpdateRoutingTableWithInitialUriFallback(c => c != null ? null : routingTableReturnMock.Object);

                    // Then
                    poolMock.Verify(x => x.Add(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                    routingTableMock.Verify(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                }

                [Fact]
                public void ShouldNotTryInitialUriIfAlreadyTried()
                {
                    // Given
                    var a = new Uri("bolt+routing://123:456");
                    var b = new Uri("bolt+routing://123:789");
                    var s = a; // should not be retried
                    var t = new Uri("bolt+routing://222:123"); // this should be retried

                    var routingTableMock = new Mock<IRoutingTable>();
                    routingTableMock.Setup(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()))
                        // ensure the retried is only t
                        .Callback<IEnumerable<Uri>>(set => set.Single().Should().Be(t));

                    var poolMock = new Mock<IClusterConnectionPool>();
                    poolMock.Setup(x => x.Add(It.IsAny<IEnumerable<Uri>>()))
                        // ensure the retried is only t
                        .Callback<IEnumerable<Uri>>(set => set.Single().Should().Be(t));

                    var balancer = new LoadBalancer(poolMock.Object, routingTableMock.Object);
                    
                    Func<ISet<Uri>, IRoutingTable> updateRoutingTableFunc = set =>
                    {
                        if (set != null)
                        {
                            set.Add(a);
                            set.Add(b);
                            return null;
                        }
                        else
                        {
                            return new Mock<IRoutingTable>().Object;
                        }
                    };
                    Func<ISet<Uri>> resolveInitialUriFunc = () => new HashSet<Uri> {s, t};
                    // When
                    balancer.UpdateRoutingTableWithInitialUriFallback(updateRoutingTableFunc, resolveInitialUriFunc);

                    // Then
                    // verify the method is actually called
                    poolMock.Verify(x => x.Add(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                    routingTableMock.Verify(x => x.PrependRouters(It.IsAny<IEnumerable<Uri>>()), Times.Once);
                }
            }

            public class UpdateRoutingTableMethod
            {
                [Fact]
                public void ConcurrentUpdateRequestsHaveNoEffect()
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");
                    var balancer = SetupLoadBalancer(new[] {uri});
                    var balancerRoutingTable = NewRoutingTable(new[] {uri});

                    var anotherUri = new Uri("bolt+routing://123:789");
                    var updateCount = 0;
                    var directReturnCount = 0;
                    Func<IConnection, IRoutingTable> updateRoutingTableFunc =
                        connection =>
                        {
                            if (!balancerRoutingTable.IsStale(AccessMode.Write))
                            {
                                Interlocked.Add(ref directReturnCount, 1);
                                return balancerRoutingTable;
                            }
                            Interlocked.Add(ref updateCount, 1);
                            // needs to return a valid routing table to stop update routing table
                            var newTable = NewRoutingTable(new[] { anotherUri }, new[] { anotherUri }, new[] { anotherUri });
                            balancerRoutingTable = newTable;
                            return newTable;
                        };

                    // When calling update routing table in many threads
                    var size = 10;
                    Thread[] threads = new Thread[size];
                    for (var i = 0; i < size; i++)
                    {
                        var thread = new Thread(() =>
                        {
                            var result = balancer.UpdateRoutingTable(updateRoutingTableFunc);
                            result.All().Should().ContainInOrder(anotherUri);
                        });
                        threads[i] = thread;
                        thread.Start();
                    }

                    foreach (var thread in threads)
                    {
                        thread.Join();
                    }
                    updateCount.Should().Be(1);
                    directReturnCount.Should().Be(size-1);
                }

                [Fact]
                public void UpdateReplaceEntireRoutingTable()
                {
                    // Given
                    var uriA = new Uri("bolt+routing://123a:456");
                    var uriB = new Uri("bolt+routing://123b:456");
                    var uriC = new Uri("bolt+routing://123c:456");
                    var balancer = SetupLoadBalancer(new[] { uriA }, new[] { uriB }, new[] { uriC });

                    // When
                    var uriX = new Uri("bolt+routing://123x:789");
                    var uriY = new Uri("bolt+routing://123y:789");
                    var uriZ = new Uri("bolt+routing://123z:789");
                    var result = balancer.UpdateRoutingTable(connection
                        => NewRoutingTable(new[] {uriX}, new[] { uriY }, new[] { uriZ }));

                    // Then
                    result.All().Should().Contain(new [] {uriX, uriY, uriZ});
                }

                [Fact]
                public void ShouldForgetAndTryNextRouterWhenFailedWithConnectionError()
                {
                    // Given
                    var uriA = new Uri("bolt+routing://123:456");
                    var uriB = new Uri("bolt+routing://123:789");

                    // This ensures that uri and uri2 will return in order
                    var routingTable = new ListBasedRoutingTable(new List<Uri> {uriA, uriB});
                    var balancer = SetupLoadBalancer(routingTable);

                    // When
                    var newRoutingTable = balancer.UpdateRoutingTable(connection =>
                    {
                        // the second connectin will give a new routingTable
                        if (connection.Server.Address.Equals(uriA.ToString())) // uriA
                        {
                            ((ClusterConnection)connection).OnError(new ServiceUnavailableException("failed init"));
                        }
                        if (connection.Server.Address.Equals(uriB.ToString())) // uriB
                        {
                            return NewRoutingTable(new[] {uriA}, new [] {uriA}, new []{uriA});
                        }

                        throw new NotSupportedException($"Unknown uri: {connection.Server.Address}");
                    });

                    // Then
                    newRoutingTable.All().Should().ContainInOrder(uriA);
                    routingTable.All().Should().ContainInOrder(uriB);
                }

                [Fact]
                public void ShouldPropagateServiceUnavailable()
                {
                    var balancer = SetupLoadBalancer(new[] {new Uri("bolt+routing://123:456")});

                    var exception = Record.Exception(()=>balancer.UpdateRoutingTable(
                        conn => { throw new ServiceUnavailableException("Procedure not found"); }));

                    exception.Should().BeOfType<ServiceUnavailableException>();
                    exception.Message.Should().Be("Procedure not found");
                }

                [Fact]
                public void ShouldTryNextRouterIfNoReader()
                {
                    // Given
                    var uriA = new Uri("bolt+routing://123:1");
                    var uriB = new Uri("bolt+routing://123:2");

                    var uriX = new Uri("bolt+routing://456:1");
                    var uriY = new Uri("bolt+routing://789:2");

                    var balancer = SetupLoadBalancer(new ListBasedRoutingTable(new List<Uri> {uriA, uriB}));

                    // When
                    var updateRoutingTable = balancer.UpdateRoutingTable(conn =>
                    {
                        if (conn.Server.Address.Equals(uriA.ToString()))
                        {
                            return NewRoutingTable(new[] {uriX}, new Uri[0], new[] {uriX});
                        }
                        if (conn.Server.Address.Equals(uriB.ToString()))
                        {
                            return NewRoutingTable(new[] {uriY}, new[] {uriY}, new[] {uriY});
                        }
                        throw new NotSupportedException($"Unknown uri: {conn.Server.Address}");
                    });

                    // Then
                    updateRoutingTable.All().Should().ContainInOrder(uriY);
                    balancer.IsReadingInAbsenceOfWriter.Should().BeFalse();
                }

                [Fact]
                public void ShouldAcceptRoutingTableIfNoWriter()
                {
                    // Given
                    var uriA = new Uri("bolt+routing://123:1");
                    var uriX = new Uri("bolt+routing://456:1");

                    var balancer = SetupLoadBalancer(new ListBasedRoutingTable(new List<Uri> { uriA }));

                    // When
                    var updateRoutingTable = balancer.UpdateRoutingTable(conn =>
                    {
                        if (conn.Server.Address.Equals(uriA.ToString()))
                        {
                            return NewRoutingTable(new[] {uriX}, new[] {uriX});
                        }
                        throw new NotSupportedException($"Unknown uri: {conn.Server.Address}");
                    });

                    // Then
                    updateRoutingTable.All().Should().ContainInOrder(uriX);
                    balancer.IsReadingInAbsenceOfWriter.Should().BeTrue();
                }

                [Theory]
                [InlineData(1, 1, 1)]
                [InlineData(2, 1, 1)]
                [InlineData(1, 2, 1)]
                [InlineData(2, 2, 1)]
                [InlineData(1, 1, 2)]
                [InlineData(2, 1, 2)]
                [InlineData(1, 2, 2)]
                [InlineData(2, 2, 2)]
                [InlineData(3, 1, 2)]
                [InlineData(3, 2, 1)]
                public void ShouldAcceptValidRoutingTables(int routerCount, int writerCount, int readerCount)
                {
                    var balancer = SetupLoadBalancer(new[] {new Uri("bolt+routing://123:45")});
                    var newRoutingTable = NewRoutingTable(routerCount, readerCount, writerCount);
                    var result = balancer.UpdateRoutingTable(connection => newRoutingTable);

                    // Then
                    result.All().Should().Contain(newRoutingTable.All());
                    newRoutingTable.All().Should().Contain(result.All());
                    balancer.IsReadingInAbsenceOfWriter.Should().BeFalse();
                }

                [Fact]
                public void ShouldPropagateProtocolError()
                {
                    var balancer = SetupLoadBalancer(new[] { new Uri("bolt+routing://123:456") });

                    var exception = Record.Exception(() => balancer.UpdateRoutingTable(
                        conn => { throw new ProtocolException("Cannot parse procedure result"); }));

                    exception.Should().BeOfType<ProtocolException>();
                    exception.Message.Should().Be("Cannot parse procedure result");
                }

                [Fact]
                public void ShouldPropagateAuthenticationException()
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");
                    // a routing table which knows a uri
                    var routingTable = NewRoutingTable(new[] {uri});

                    var mockedClusterPool = new Mock<IClusterConnectionPool>();
                    var balancer = new LoadBalancer(mockedClusterPool.Object, routingTable);

                    var mockedConn = new Mock<IPooledConnection>();
                    var conn = mockedConn.Object;
                    mockedClusterPool.Setup(x => x.TryAcquire(uri, out conn)).Callback(() =>
                    {
                        throw new AuthenticationException("Failed to auth the client to the server.");
                    });
                    
                    // When
                    var error = Record.Exception(() => balancer.UpdateRoutingTable());

                    // Then
                    error.Should().BeOfType<AuthenticationException>();
                    error.Message.Should().Contain("Failed to auth the client to the server.");

                    // while the server is not removed
                    routingTable.All().Should().ContainInOrder(uri);
                }
            }

            public class AcquireReadWriteConnectionMethod
            {
                [Theory]
                [InlineData(AccessMode.Read)]
                [InlineData(AccessMode.Write)]
                public void ShouldThrowSessionExpiredExceptionIfNoServerAvailable(AccessMode mode)
                {
                    // Given
                    var mock = CreateRoutingTable(mode, null, false);
                    var balancer = new LoadBalancer(null, mock.Object);

                    // When
                    var error = Record.Exception(()=>balancer.Acquire(mode));

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
                    var mock = CreateRoutingTable(mode, uri);
                    mock.Setup(m => m.All()).Returns(new HashSet<Uri> { uri });
                    var balancer = SetupLoadBalancer(mock.Object);

                    // When
                    var acquiredConn = balancer.Acquire(mode);

                    // Then
                    acquiredConn.Server.Address.Should().Be(uri.ToString());
                }

                private static Mock<IRoutingTable> CreateRoutingTable(AccessMode mode, Uri uri, bool hasNext = true)
                {
                    var mock = new Mock<IRoutingTable>();
                    mock.Setup(m => m.IsStale(It.IsAny<AccessMode>())).Returns(false);
                    if (mode == AccessMode.Read)
                    {
                        mock.SetupSequence(m => m.TryNextReader(out uri)).Returns(hasNext).Returns(false);
                    }
                    else
                    {
                        mock.SetupSequence(m => m.TryNextWriter(out uri)).Returns(hasNext).Returns(false);
                    }
                    return mock;
                }

                [Theory]
                [InlineData(AccessMode.Read)]
                [InlineData(AccessMode.Write)]
                public void ShouldForgetServerWhenFailedToEstablishConn(AccessMode mode)
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");
                    var routingTableMock = CreateRoutingTable(mode, uri);

                    var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
                    IPooledConnection conn = null;
                    clusterConnPoolMock.Setup(x => x.TryAcquire(uri, out conn))
                        .Callback(() =>
                        {
                            throw new ServiceUnavailableException("failed init");
                        });

                    var balancer = new LoadBalancer(clusterConnPoolMock.Object, routingTableMock.Object);

                    // When
                    var error = Record.Exception(() => balancer.Acquire(mode));

                    // Then
                    error.Should().BeOfType<SessionExpiredException>();
                    error.Message.Should().Contain("Failed to connect to any");

                    // should be removed
                    routingTableMock.Verify(m=>m.Remove(uri), Times.Once);
                    clusterConnPoolMock.Verify(m=>m.Purge(uri), Times.Once);
                }

                [Theory]
                [InlineData(AccessMode.Read)]
                [InlineData(AccessMode.Write)]
                public void ShouldThrowErrorDirectlyIfSecurityError(AccessMode mode)
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");
                    var routingTableMock = CreateRoutingTable(mode, uri);

                    var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
                    IPooledConnection conn = null;
                    clusterConnPoolMock.Setup(x => x.TryAcquire(uri, out conn))
                        .Callback(() =>
                        {
                            throw new SecurityException("Failed to establish ssl connection with the server");
                        });

                    var balancer = new LoadBalancer(clusterConnPoolMock.Object, routingTableMock.Object);

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
                    var routingTableMock = CreateRoutingTable(mode, uri);

                    var clusterConnPoolMock = new Mock<IClusterConnectionPool>();
                    IPooledConnection conn = null;
                    clusterConnPoolMock.Setup(x => x.TryAcquire(uri, out conn)).Returns(false)
                        .Callback(() =>
                        {
                            throw new ProtocolException("do not understand struct 0x01");
                        });

                    var balancer = new LoadBalancer(clusterConnPoolMock.Object, routingTableMock.Object);

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
                    var loadBalancer = new LoadBalancer(clusterPoolMock.Object, routingTableMock.Object);

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
                    var loadBalancer = new LoadBalancer(clusterPoolMock.Object, routingTableMock.Object);

                    loadBalancer.OnWriteError(uri);
                    clusterPoolMock.Verify(x => x.Purge(uri), Times.Never);
                    routingTableMock.Verify(x => x.Remove(uri), Times.Never);
                    routingTableMock.Verify(x => x.RemoveWriter(uri), Times.Once);
                }
            }
        }

        internal class ListBasedRoutingTable : IRoutingTable
        {
            private readonly List<Uri> _routers;
            private readonly List<Uri> _removed;
            private int _count = 0;

            public ListBasedRoutingTable(List<Uri> routers)
            {
                _routers = routers;
                _removed = new List<Uri>();
            }
            public bool IsStale(AccessMode mode)
            {
                return false;
            }

            public bool TryNextRouter(out Uri uri)
            {
                var pos = _count++ % _routers.Count;
                uri = _routers[pos];
                return true;
            }

            public bool TryNextReader(out Uri uri)
            {
                throw new NotSupportedException();
            }

            public bool TryNextWriter(out Uri uri)
            {
                throw new NotSupportedException();
            }

            public void Remove(Uri uri)
            {
                _removed.Add(uri);
            }

            public void RemoveWriter(Uri uri)
            {
                throw new NotSupportedException();
            }

            public ISet<Uri> All()
            {
                return new HashSet<Uri>(_routers.Distinct().Except(_removed.Distinct()));
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public void PrependRouters(IEnumerable<Uri> uris)
            {
                throw new NotSupportedException();
            }
        }

        private static IRoutingTable NewRoutingTable(int routerCount, int readerCount, int writerCount)
        {
            return NewRoutingTable(GenerateServerUris(routerCount), GenerateServerUris(readerCount),
                GenerateServerUris(writerCount));
        }

        private static IEnumerable<Uri> GenerateServerUris(int count)
        {
            var uris = new Uri[count];
            for (var i = 0; i < count; i++)
            {
                uris[i] = new Uri($"bolt+routing://127.0.0.1:{i + 9001}");
            }
            return uris;
        }

        private static IRoutingTable NewRoutingTable(
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
            return new RoundRobinRoutingTable(routers, readers, writers, new Stopwatch(), 1000);
        }

        private static LoadBalancer SetupLoadBalancer(
            IEnumerable<Uri> routers = null,
            IEnumerable<Uri> readers = null,
            IEnumerable<Uri> writers = null)
        {
            // create a routing table which knows a few servers
            var routingTable = NewRoutingTable(routers, readers, writers);
            return SetupLoadBalancer(routingTable);
        }

        private static LoadBalancer SetupLoadBalancer(IRoutingTable routingTable)
        {
            var uris = routingTable.All();

            // create a mocked cluster connection pool, which will return the same connection for each different uri
            var mockedClusterPool = new Mock<IClusterConnectionPool>();

            foreach (var uri in uris)
            {
                var mockedConn = new Mock<IPooledConnection>();
                mockedConn.Setup(x => x.Server.Address).Returns(uri.ToString);
                var conn = mockedConn.Object;
                mockedClusterPool.Setup(x => x.TryAcquire(uri, out conn)).Returns(true);
            }
            return new LoadBalancer(mockedClusterPool.Object, routingTable);
        }
    }
}
