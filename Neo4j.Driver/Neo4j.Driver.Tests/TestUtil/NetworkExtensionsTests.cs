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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;
using static Neo4j.Driver.Internal.NetworkExtensions;

namespace Neo4j.Driver.Tests
{
    public class NetworkExtensionsTests
    {
        public class IpAddressResolveAsyncMethod
        {
            [Fact]
            public void ShouldSortIPv6AddrInFront()
            {
                var ipAddresses = new List<IPAddress>
                {
                    IPAddress.Parse("10.0.0.1"),
                    IPAddress.Parse("192.168.0.11"),
                    IPAddress.Parse("[::1]")
                };
                var addresses = ipAddresses.OrderBy(x => x, new AddressComparer(AddressFamily.InterNetworkV6)).ToArray();
                addresses.Length.Should().Be(3);
                addresses[0].ToString().Should().Be("::1");
                addresses[1].ToString().Should().Be("10.0.0.1");
                addresses[2].ToString().Should().Be("192.168.0.11");
            }

            [Fact]
            public async void ShouldOnlyGiveIpv4AddressWhenIpv6IsNotEnabled()
            {
                var url = new Uri("bolt://127.0.0.1");
                var ips = await url.ResolveAsyc(false);
                ips.Length.Should().Be(1);
                ips[0].ToString().Should().Be("127.0.0.1");
            }

            [Fact]
            public async void ShouldGiveIpv4Ipv6AddressWhenIpv6IsEnabled()
            {
                var url = new Uri("bolt://127.0.0.1");
                var ips = await url.ResolveAsyc(true);
                ips.Length.Should().Be(2);
                ips[0].ToString().Should().Be("::ffff:127.0.0.1");
                ips[1].ToString().Should().Be("127.0.0.1");
            }
        }

        public class ParseRoutingContextMethod
        {
            [Theory]
            [InlineData("bolt")]
            [InlineData("bolt+routing")]
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
            [InlineData("bolt+routing")]
            public void ShouldParseSingleRoutingContext(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost:7687/cat?name=molly");
                var routingContext = raw.ParseRoutingContext();

                routingContext["name"].Should().Be("molly");
            }

            [Theory]
            [InlineData("bolt")]
            [InlineData("bolt+routing")]
            public void ShouldErrorIfMissingValue(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost:7687/cat?name=");
                var exception = Record.Exception(()=> raw.ParseRoutingContext());
                exception.Should().BeOfType<ArgumentException>();
                exception.Message.Should().Contain("Invalid parameters: 'name=' in URI");
            }

            [Theory]
            [InlineData("bolt")]
            [InlineData("bolt+routing")]
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
