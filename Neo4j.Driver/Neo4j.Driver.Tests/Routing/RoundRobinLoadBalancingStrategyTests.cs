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
    public class RoundRobinLoadBalancingStrategyTests
    {
        [Fact]
        public void ShouldHandleEmptyReadersList()
        {
            var strategy = NewRoundRobinStrategy();

            var reader = strategy.SelectReader(new List<Uri>());

            reader.Should().BeNull();
        }

        [Fact]
        public void ShouldHandleEmptyWritersList()
        {
            var strategy = NewRoundRobinStrategy();

            var writer = strategy.SelectWriter(new List<Uri>());

            writer.Should().BeNull();
        }

        [Fact]
        public void ShouldHandleSingleReader()
        {
            var address = new Uri("localhost:1");
            var strategy = NewRoundRobinStrategy();

            var reader = strategy.SelectReader(new List<Uri>() {address});

            reader.Should().Be(address);
        }

        [Fact]
        public void ShouldHandleSingleWriter()
        {
            var address = new Uri("localhost:2");
            var strategy = NewRoundRobinStrategy();

            var writer = strategy.SelectWriter(new List<Uri>() {address});

            writer.Should().Be(address);
        }

        [Fact]
        public void ShouldReturnReadersInRoundRobinOrder()
        {
            var address1 = new Uri("localhost:1");
            var address2 = new Uri("localhost:2");
            var address3 = new Uri("localhost:3");
            var address4 = new Uri("localhost:4");

            var readers = new List<Uri> {address1, address2, address3, address4};
            var strategy = NewRoundRobinStrategy();

            strategy.SelectReader(readers).Should().Be(address1);
            strategy.SelectReader(readers).Should().Be(address2);
            strategy.SelectReader(readers).Should().Be(address3);
            strategy.SelectReader(readers).Should().Be(address4);

            strategy.SelectReader(readers).Should().Be(address1);
            strategy.SelectReader(readers).Should().Be(address2);
            strategy.SelectReader(readers).Should().Be(address3);
            strategy.SelectReader(readers).Should().Be(address4);
        }

        [Fact]
        public void ShouldReturnWritersInRoundRobinOrder()
        {
            var address1 = new Uri("localhost:1");
            var address2 = new Uri("localhost:2");
            var address3 = new Uri("localhost:3");

            var writers = new List<Uri> {address1, address2, address3};
            var strategy = NewRoundRobinStrategy();

            strategy.SelectWriter(writers).Should().Be(address1);
            strategy.SelectWriter(writers).Should().Be(address2);
            strategy.SelectWriter(writers).Should().Be(address3);

            strategy.SelectWriter(writers).Should().Be(address1);
            strategy.SelectWriter(writers).Should().Be(address2);
            strategy.SelectWriter(writers).Should().Be(address3);
        }

        private static RoundRobinLoadBalancingStrategy NewRoundRobinStrategy()
        {
            return new RoundRobinLoadBalancingStrategy(new Mock<ILogger>().Object);
        }
    }
}
