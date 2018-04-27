// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Routing
{
    public class LeastConnectedLoadBalancingStrategyTests
    {
        [Fact]
        public void ShouldHandleEmptyReadersList()
        {
            var connectionPoolMock = new Mock<IClusterConnectionPool>();
            var strategy = NewLeastConnectedStrategy(connectionPoolMock.Object);

            var reader = strategy.SelectReader(new List<Uri>());

            reader.Should().BeNull();
        }

        [Fact]
        public void ShouldHandleEmptyWritersList()
        {
            var connectionPoolMock = new Mock<IClusterConnectionPool>();
            var strategy = NewLeastConnectedStrategy(connectionPoolMock.Object);

            var writer = strategy.SelectWriter(new List<Uri>());

            writer.Should().BeNull();
        }

        [Fact]
        public void ShouldHandleSingleReaderWithoutInUseConnections()
        {
            var address = new Uri("reader:7687");
            var connectionPoolMock = new Mock<IClusterConnectionPool>();
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address)).Returns(0);
            var strategy = NewLeastConnectedStrategy(connectionPoolMock.Object);

            var reader = strategy.SelectReader(new List<Uri> {address});

            reader.Should().Be(address);
        }

        [Fact]
        public void ShouldHandleSingleWriterWithoutInUseConnections()
        {
            var address = new Uri("writer:7687");
            var connectionPoolMock = new Mock<IClusterConnectionPool>();
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address)).Returns(0);
            var strategy = NewLeastConnectedStrategy(connectionPoolMock.Object);

            var writer = strategy.SelectWriter(new List<Uri> {address});

            writer.Should().Be(address);
        }

        [Fact]
        public void ShouldHandleSingleReaderWithInUseConnections()
        {
            var address = new Uri("reader:7687");
            var connectionPoolMock = new Mock<IClusterConnectionPool>();
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address)).Returns(42);
            var strategy = NewLeastConnectedStrategy(connectionPoolMock.Object);

            var reader = strategy.SelectReader(new List<Uri> {address});

            reader.Should().Be(address);
        }

        [Fact]
        public void ShouldHandleSingleWriterWithInUseConnections()
        {
            var address = new Uri("writer:7687");
            var connectionPoolMock = new Mock<IClusterConnectionPool>();
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address)).Returns(42);
            var strategy = NewLeastConnectedStrategy(connectionPoolMock.Object);

            var writer = strategy.SelectWriter(new List<Uri> {address});

            writer.Should().Be(address);
        }

        [Fact]
        public void ShouldHandleMultipleReadersWithInUseConnections()
        {
            var address1 = new Uri("reader:1");
            var address2 = new Uri("reader:2");
            var address3 = new Uri("reader:3");

            var connectionPoolMock = new Mock<IClusterConnectionPool>();
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address1)).Returns(3);
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address2)).Returns(4);
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address3)).Returns(1);

            var strategy = NewLeastConnectedStrategy(connectionPoolMock.Object);

            var reader = strategy.SelectReader(new List<Uri> {address1, address2, address3});

            reader.Should().Be(address3);
        }

        [Fact]
        public void ShouldHandleMultipleWritersWithInUseConnections()
        {
            var address1 = new Uri("writer:1");
            var address2 = new Uri("writer:2");
            var address3 = new Uri("writer:3");
            var address4 = new Uri("writer:4");

            var connectionPoolMock = new Mock<IClusterConnectionPool>();
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address1)).Returns(5);
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address2)).Returns(6);
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address3)).Returns(0);
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(address4)).Returns(1);

            var strategy = NewLeastConnectedStrategy(connectionPoolMock.Object);

            var writer = strategy.SelectWriter(new List<Uri> {address1, address2, address3, address4});

            writer.Should().Be(address3);
        }

        [Fact]
        public void ShouldReturnDifferentReaderOnEveryInvocationWhenNoInUseConnections()
        {
            var address1 = new Uri("reader:1");
            var address2 = new Uri("reader:2");
            var address3 = new Uri("reader:3");
            var addresses = new List<Uri> {address1, address2, address3};

            var connectionPoolMock = new Mock<IClusterConnectionPool>();
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(It.IsAny<Uri>())).Returns(0);

            var strategy = NewLeastConnectedStrategy(connectionPoolMock.Object);

            strategy.SelectReader(addresses).Should().Be(address1);
            strategy.SelectReader(addresses).Should().Be(address2);
            strategy.SelectReader(addresses).Should().Be(address3);

            strategy.SelectReader(addresses).Should().Be(address1);
            strategy.SelectReader(addresses).Should().Be(address2);
            strategy.SelectReader(addresses).Should().Be(address3);
        }

        [Fact]
        public void ShouldReturnDifferentWriterOnEveryInvocationWhenNoInUseConnections()
        {
            var address1 = new Uri("writer:1");
            var address2 = new Uri("writer:2");
            var addresses = new List<Uri> {address1, address2};

            var connectionPoolMock = new Mock<IClusterConnectionPool>();
            connectionPoolMock.Setup(x => x.NumberOfInUseConnections(It.IsAny<Uri>())).Returns(0);

            var strategy = NewLeastConnectedStrategy(connectionPoolMock.Object);

            strategy.SelectWriter(addresses).Should().Be(address1);
            strategy.SelectWriter(addresses).Should().Be(address2);

            strategy.SelectWriter(addresses).Should().Be(address1);
            strategy.SelectWriter(addresses).Should().Be(address2);
        }

        private static LeastConnectedLoadBalancingStrategy NewLeastConnectedStrategy(
            IClusterConnectionPool connectionPool)
        {
            return new LeastConnectedLoadBalancingStrategy(connectionPool, new Mock<ILogger>().Object);
        }
    }
}
