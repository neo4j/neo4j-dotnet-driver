// Copyright (c) 2002-2016 "Neo Technology,"
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
using System.IO;
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
            public class NewClusterViewMethod
            {
                [Fact]
                public void ShouldThrowServerUnavailableException()
                {
                    // Given
                    var clusterView = new RoundRobinClusterView(new Uri[0], new Uri[0], new Uri[0]);
                    var balancer = new RoundRobinLoadBalancer(null, clusterView);

                    // When
                    var error = Record.Exception(() => balancer.NewClusterView());

                    // Then
                    error.Should().BeOfType<ServerUnavailableException>();
                    error.Message.Should().Contain("Failed to connect to any routing server.");
                }

                [Fact]
                public void ShouldGetANewClusterView()
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");
                    // a cluster view which knows a read/write uri
                    var clusterView = new RoundRobinClusterView(new[] {uri}, new Uri[0], new Uri[0]);

                    var mockedConn = new Mock<IPooledConnection>();
                    var mockedConnPool = new Mock<IConnectionPool>();
                    mockedConnPool.Setup(x => x.Acquire()).Returns(mockedConn.Object);
                    var dict = new ConcurrentDictionary<Uri, IConnectionPool>();
                    dict.TryAdd(uri, mockedConnPool.Object);

                    // a cluster pool which knows the conns with the uri server
                    var clusterConnPool = new ClusterConnectionPool(null, dict);
                    var balancer = new RoundRobinLoadBalancer(clusterConnPool, clusterView);

                    // When
                    var anotherUri = new Uri("bolt+routing://123:789");
                    var result = balancer.NewClusterView((connection, logger) 
                        => new RoundRobinClusterView(new [] {anotherUri}, new Uri[0], new Uri[0]));

                    // Then
                    result.All().Should().ContainInOrder(anotherUri);
                }

                [Fact]
                public void ShouldRemoveTheServerIfRediscoveryThrowInvalidDiscoveryException()
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");
                    var uri2 = new Uri("bolt+routing://123:789");
                    // a cluster view which knows a read/write uri
                    var clusterView = new RoundRobinClusterView(new[] { uri, uri2 }, new Uri[0], new Uri[0]);

                    var mockedConn = new Mock<IPooledConnection>();
                    var mockedConnPool = new Mock<IConnectionPool>();
                    mockedConnPool.Setup(x => x.Acquire()).Returns(mockedConn.Object);
                    var dict = new ConcurrentDictionary<Uri, IConnectionPool>();
                    dict.TryAdd(uri, mockedConnPool.Object);
                    dict.TryAdd(uri2, mockedConnPool.Object);

                    // a cluster pool which knows the conns with the uri server
                    var clusterConnPool = new ClusterConnectionPool(null, dict);

                    var balancer = new RoundRobinLoadBalancer(clusterConnPool, clusterView);

                    // When
                    var error = Record.Exception(() =>balancer.NewClusterView((connection, logger) =>
                    {
                        // never successfully rediscovery
                        throw new InvalidDiscoveryException("Invalid");
                    }));

                    // Then
                    clusterView.All().Should().BeEmpty();
                    error.Should().BeOfType<ServerUnavailableException>();
                }
            }

            public class AcquireReadWriteConnectionMethod
            {
                private static RoundRobinClusterView CreateClusterView(Uri uri, AccessMode mode)
                {
                    RoundRobinClusterView clusterView;
                    switch (mode)
                    {
                        case AccessMode.Read:
                            clusterView = new RoundRobinClusterView(new Uri[0], new[] { uri }, new Uri[0]);
                            break;
                        case AccessMode.Write:
                            clusterView = new RoundRobinClusterView(new Uri[0], new Uri[0], new[] { uri });
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown type {mode} to this test.");
                    }
                    return clusterView;
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
                    var clusterView = new RoundRobinClusterView(new Uri[0], new Uri[0], new Uri[0]);
                    var balancer = new RoundRobinLoadBalancer(null, clusterView);

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
                    // a cluster view which knows a read/write uri
                    var clusterView = CreateClusterView(uri, mode);

                    var mockedConn = new Mock<IPooledConnection>();
                    var mockedConnPool = new Mock<IConnectionPool>();
                    mockedConnPool.Setup(x => x.Acquire()).Returns(mockedConn.Object);
                    var dict = new ConcurrentDictionary<Uri, IConnectionPool>();
                    dict.TryAdd(uri, mockedConnPool.Object);

                    // a cluster pool which knows the conns with the uri server
                    var clusterConnPool = new ClusterConnectionPool(null, dict);
                    var balancer = new RoundRobinLoadBalancer(clusterConnPool, clusterView);

                    // When
                    var acquiredConn = AcquiredConn(balancer, mode);

                    // Then
                    acquiredConn.Should().Be(mockedConn.Object);
                }

                [Theory]
                [InlineData(AccessMode.Read)]
                [InlineData(AccessMode.Write)]
                public void ShouldRemoveFromClusterViewIfFailedToEstablishConn(AccessMode mode)
                {
                    // Given
                    var uri = new Uri("bolt+routing://123:456");
                    // a cluster view which knows a uri
                    var clusterView = CreateClusterView(uri, mode);

                    var mockedConnPool = new Mock<IConnectionPool>();
                    var dict = new ConcurrentDictionary<Uri, IConnectionPool>();
                    dict.TryAdd(uri, mockedConnPool.Object);

                    // a cluster pool which knows the conns with the read uri server
                    var clusterConnPool = new ClusterConnectionPool(null, dict);
                    var balancer = new RoundRobinLoadBalancer(clusterConnPool, clusterView);

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
