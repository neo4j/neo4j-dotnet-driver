// Copyright (c) 2002-2020 "Neo4j,"
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
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;
using static Neo4j.Driver.Internal.NetworkExtensions;

namespace Neo4j.Driver.Tests
{
    public class NetworkExtensionsTests
    {
        public class ParseRoutingContextMethod
        {
            [Theory]
            [InlineData("bolt")]
            [InlineData("neo4j")]
            public void ShouldParseEmptyRoutingContext(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var routingContext = raw.ParseRoutingContext();

                routingContext.Should().BeEmpty();
            }

            [Theory]
            [InlineData("bolt")]
            [InlineData("neo4j")]
            public void ShouldParseMultipleRoutingContext(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost:7687/cat?name=molly&age=1&color=white");
                var routingContext = raw.ParseRoutingContext();

                routingContext["name"].Should().Be("molly");
                routingContext["age"].Should().Be("1");
                routingContext["color"].Should().Be("white");
            }

            [Theory]
            [InlineData("bolt")]
            [InlineData("neo4j")]
            public void ShouldParseSingleRoutingContext(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost:7687/cat?name=molly");
                var routingContext = raw.ParseRoutingContext();

                routingContext["name"].Should().Be("molly");
            }

            [Theory]
            [InlineData("bolt")]
            [InlineData("neo4j")]
            public void ShouldErrorIfMissingValue(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost:7687/cat?name=");
                var exception = Record.Exception(()=> raw.ParseRoutingContext());
                exception.Should().BeOfType<ArgumentException>();
                exception.Message.Should().Contain("Invalid parameters: 'name=' in URI");
            }

            [Theory]
            [InlineData("bolt")]
            [InlineData("neo4j")]
            public void ShouldErrorIfDuplicateKey(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost:7687/cat?name=molly&name=mostly_white");
                var exception = Record.Exception(() => raw.ParseRoutingContext());
                exception.Should().BeOfType<ArgumentException>();
                exception.Message.Should().Contain("Duplicated query parameters with key 'name'");
            }
        }
    }
}
