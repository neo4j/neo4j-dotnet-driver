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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class RoundRobinLoadBalancerTests
    {
        public class AcquireConnectionMethod
        {
            private static RoundRobinRoutingTable NewRoutingTable(
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

            private static RoundRobinLoadBalancer SetupLoadBalancer(
                IEnumerable<Uri> routers=null,
                IEnumerable<Uri> readers=null,
                IEnumerable<Uri> writers=null)
            {
                // create a routing table which knows a few servers
                var routingTable = NewRoutingTable(routers, readers, writers);
                return SetupLoadBalancer(routingTable);
            }

            private static RoundRobinLoadBalancer SetupLoadBalancer(RoundRobinRoutingTable routingTable, Mock<IPooledConnection> mockedConn = null)
            {
                var uris = routingTable.All();

                // create a mocked connection pool which no matter what you ask, you will always get the same mocked connection
                mockedConn = mockedConn ?? new Mock<IPooledConnection>();
                var mockedConnPool = new Mock<IConnectionPool>();
                mockedConnPool.Setup(x => x.Acquire()).Returns(mockedConn.Object);
                var dict = new ConcurrentDictionary<Uri, IConnectionPool>();

                // add all routers/readers/writer uri and map it to the single mocked connection pool
                foreach (var uri in uris)
                {
                    dict.TryAdd(uri, mockedConnPool.Object);
                }

                // create a cluster pool which knows the conns with the uri server
                var clusterConnPool = new ClusterConnectionPool(null, dict);

                return new RoundRobinLoadBalancer(clusterConnPool, routingTable);
            }

            public class NewroutingTableMethod
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
                    Func<IPooledConnection, RoundRobinRoutingTable> updateRoutingTableFunc =
                        connection =>
                        {
                            if (!balancerRoutingTable.IsStale())
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
                public void ShouldThrowServerUnavailableExceptionWhenNoAvailableRouters()
                {
                    // Given
                    var routingTable = NewRoutingTable();
                    var balancer = new RoundRobinLoadBalancer(null, routingTable);

                    // When
                    var error = Record.Exception(() => balancer.UpdateRoutingTable());

                    // Then
                    error.Should().BeOfType<ServiceUnavailableException>();
                    error.Message.Should().Contain("Failed to connect to any routing server.");
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
                public void ForgetUnavailableRouterIfUpdateRoutingTableThrowsInvalidDiscoveryException()
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");
                    var uri2 = new Uri("bolt+routing://123:789");
                    // a routing table which knows a read/write uri
                    var routingTable = NewRoutingTable(new[] {uri, uri2});

                    var balancer = SetupLoadBalancer(routingTable);

                    // When
                    var error = Record.Exception(() =>balancer.UpdateRoutingTable(connection =>
                    {
                        // never successfully rediscovery
                        throw new InvalidDiscoveryException("Invalid");
                    }));

                    // Then
                    routingTable.All().Should().BeEmpty();
                    error.Should().BeOfType<ServiceUnavailableException>();
                }



                [Fact]
                public void ShouldUpdateSuccessfullyDespiteUnavailableRouters()
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");
                    var mockedConn = new Mock<IPooledConnection>();
                    mockedConn.Setup(x => x.Server.Address).Returns($"{uri.Host}:{uri.Port}");
                    var mockedConnPool = new Mock<IConnectionPool>();
                    mockedConnPool.Setup(x => x.Acquire()).Returns(mockedConn.Object);

                    var uri2 = new Uri("bolt+routing://123:789");
                    var mockedConn2 = new Mock<IPooledConnection>();
                    mockedConn2.Setup(x => x.Server.Address).Returns($"{uri2.Host}:{uri2.Port}");
                    var mockedConnPool2 = new Mock<IConnectionPool>();
                    mockedConnPool2.Setup(x => x.Acquire()).Returns(mockedConn2.Object);

                    var dict = new ConcurrentDictionary<Uri, IConnectionPool>();
                    dict.TryAdd(uri, mockedConnPool.Object);
                    dict.TryAdd(uri2, mockedConnPool2.Object);

                    // create a cluster pool which knows the conns with the uri server
                    var clusterConnPool = new ClusterConnectionPool(null, dict);

                    // a routing table which knows a read/write uri
                    var routingTable = NewRoutingTable(new[] { uri, uri2 });

                    var balancer = new RoundRobinLoadBalancer(clusterConnPool, routingTable);

                    // When
                    balancer.UpdateRoutingTable(connection =>
                    {
                        if (connection.Server.Address.Equals("123:789"))
                        {
                            return NewRoutingTable(new[] {uri});
                        }

                        // never successfully rediscovery
                        throw new InvalidDiscoveryException("Invalid");
                    });

                    // Then
                    routingTable.All().Should().ContainInOrder(uri2);
                }
            }

            public class AcquireReadWriteConnectionMethod
            {
                private static RoundRobinRoutingTable CreateroutingTable(Uri uri, AccessMode mode)
                {
                    RoundRobinRoutingTable routingTable;
                    switch (mode)
                    {
                        case AccessMode.Read:
                            routingTable = NewRoutingTable(null, new[] {uri});
                            break;
                        case AccessMode.Write:
                            routingTable = NewRoutingTable(null, null, new[] {uri});
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown type {mode} to this test.");
                    }
                    return routingTable;
                }

                private static IPooledConnection AcquiredConn(RoundRobinLoadBalancer balancer, AccessMode mode)
                {
                    IPooledConnection acquiredConn;
                    switch (mode)
                    {
                        case AccessMode.Read:
                            acquiredConn = balancer.AcquireReadConnection();
                            break;
                        case AccessMode.Write:
                            acquiredConn = balancer.AcquireWriteConnection();
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown type {mode} to this test.");
                    }
                    return acquiredConn;
                }

                [Theory]
                [InlineData(AccessMode.Read)]
                [InlineData(AccessMode.Write)]
                public void ShouldThrowSessionExpiredExceptionIfNoServerAvailable(AccessMode mode)
                {
                    // Given
                    var routingTable = NewRoutingTable();
                    var balancer = new RoundRobinLoadBalancer(null, routingTable);

                    // When
                    var error = Record.Exception(()=>AcquiredConn(balancer, mode));

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
                    // a routing table which knows a read/write uri
                    var routingTable = CreateroutingTable(uri, mode);
                    var mockedConn = new Mock<IPooledConnection>();
                    var balancer = SetupLoadBalancer(routingTable, mockedConn);

                    // When
                    var acquiredConn = AcquiredConn(balancer, mode);

                    // Then
                    acquiredConn.Should().Be(mockedConn.Object);
                }

                [Theory]
                [InlineData(AccessMode.Read)]
                [InlineData(AccessMode.Write)]
                public void ShouldRemoveFromRoutingTableIfFailedToEstablishConn(AccessMode mode)
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");
                    // a routing table which knows a uri
                    var routingTable = CreateroutingTable(uri, mode);

                    var mockedConnPool = new Mock<IConnectionPool>();
                    var dict = new ConcurrentDictionary<Uri, IConnectionPool>();
                    dict.TryAdd(uri, mockedConnPool.Object);

                    // a cluster pool which knows the conns with the read uri server
                    var clusterConnPool = new ClusterConnectionPool(null, dict);
                    var balancer = new RoundRobinLoadBalancer(clusterConnPool, routingTable);

                    mockedConnPool.Setup(x => x.Acquire())
                        .Callback(() =>
                        {
                            throw balancer.CreateClusterPooledConnectionErrorHandler(uri).OnConnectionError(new IOException("failed init"));
                        });

                    // When
                    var error = Record.Exception(()=>AcquiredConn(balancer, mode));

                    // Then
                    error.Should().BeOfType<SessionExpiredException>();
                    error.Message.Should().Contain("Failed to connect to any");
                }
            }
        }
    }
}
