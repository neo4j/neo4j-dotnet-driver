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
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector.Trust;
using Xunit;
using static Neo4j.Driver.Internal.NetworkExtensions;

namespace Neo4j.Driver.Tests
{
    public class NetworkExtensionsTests
    {
        public class ParseRoutingContextMethod
        {
            private const int DefaultBoltPort = 7687;

            [Theory]
            [InlineData("bolt")]            
            public void ShouldParseEmptyRoutingContext(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var routingContext = raw.ParseRoutingContext(DefaultBoltPort);

                routingContext.Should().BeEmpty();
            }

            [Theory]
            [InlineData("neo4j")]
            public void ShouldParseDefaultEntryRoutingContext(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var routingContext = raw.ParseRoutingContext(DefaultBoltPort);

                routingContext.Should().HaveCount(1);
                routingContext["address"].Should().Be("localhost:7687");
            }

            [Theory]
            [InlineData("neo4j")]
            public void ShouldParseMultipleRoutingContext(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost:7687/cat?name=molly&age=1&color=white");
                var routingContext = raw.ParseRoutingContext(DefaultBoltPort);

                routingContext["name"].Should().Be("molly");
                routingContext["age"].Should().Be("1");
                routingContext["color"].Should().Be("white");
                routingContext["address"].Should().Be("localhost:7687");
            }

            [Theory]
            [InlineData("neo4j")]
            public void ShouldParseSingleRoutingContext(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost:7687/cat?name=molly");
                var routingContext = raw.ParseRoutingContext(DefaultBoltPort);

                routingContext["name"].Should().Be("molly");
            }

            [Theory]
            [InlineData("neo4j")]
            public void ShouldErrorIfMissingValue(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost:7687/cat?name=");
                var exception = Record.Exception(()=> raw.ParseRoutingContext(DefaultBoltPort));
                exception.Should().BeOfType<ArgumentException>();
                exception.Message.Should().Contain("Invalid parameters: 'name=' in URI");
            }

            [Theory]
            [InlineData("neo4j")]
            public void ShouldErrorIfDuplicateKey(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost:7687/cat?name=molly&name=mostly_white");
                var exception = Record.Exception(() => raw.ParseRoutingContext(DefaultBoltPort));
                exception.Should().BeOfType<ArgumentException>();
                exception.Message.Should().Contain("Duplicated query parameters with key 'name'");
            }

            [Theory]            
            [InlineData("neo4j", "localhost:1234",      "localhost:1234")]
            [InlineData("neo4j", "g.example.com",       "g.example.com:7687")]
            [InlineData("neo4j", "203.0.113.254",       "203.0.113.254:7687")]
            [InlineData("neo4j", "[2001:DB8::]",        "[2001:db8::]:7687")]
            [InlineData("neo4j", "localhost", "localhost:7687")]
            public void ShouldContainAddressContext(string scheme, string address, string expectedAddress)
            {
                var raw = new Uri($"{scheme}://{address}");
                var routingContext = raw.ParseRoutingContext(DefaultBoltPort);

                routingContext["address"].Should().Be(expectedAddress);
            }
        }

        public class IsSimpleUriSchemeMethod
        {
            [Theory]
            [InlineData("bolt")]
            [InlineData("neo4j")]
            public void ShouldBeSimpleUri(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var isSimple = raw.IsSimpleUriScheme();

                isSimple.Should().BeTrue();
            }

            [Theory]
            [InlineData("bolt+s")]
            [InlineData("bolt+ssc")]
            [InlineData("neo4j+s")]
            [InlineData("neo4j+ssc")]
            public void ShouldNotBeSimpleUri(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var isSimple = raw.IsSimpleUriScheme();

                isSimple.Should().BeFalse();
            }

            [Theory]
            [InlineData("bolt+ss")]
            [InlineData("bolts")]
            [InlineData("neo4js")]
            [InlineData("neo4j-ssc")]
            public void ShouldErrorForUnknownBoltUri(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var ex = Record.Exception(() => raw.IsSimpleUriScheme());
                ex.Should().BeOfType<NotSupportedException>();
            }
        }

        public class IsRoutingUriMethod
        {
            [Theory]
            [InlineData("neo4j")]
            [InlineData("neo4j+s")]
            [InlineData("neo4j+ssc")]
            public void ShouldBeRoutingUri(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var isRoutingUri = raw.IsRoutingUri();

                isRoutingUri.Should().BeTrue();
            }

            [Theory]
            [InlineData("bolt")]
            [InlineData("bolt+s")]
            [InlineData("bolt+ssc")]
            public void ShouldNotBeRoutingUri(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var isRoutingUri = raw.IsRoutingUri();

                isRoutingUri.Should().BeFalse();
            }

            [Theory]
            [InlineData("bolt+ss")]
            [InlineData("bolts")]
            [InlineData("neo4js")]
            [InlineData("neo4j-ssc")]
            public void ShouldErrorForUnknownBoltUri(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var ex = Record.Exception(() => raw.IsRoutingUri());
                ex.Should().BeOfType<NotSupportedException>();
            }
        }

        public class ParseUriSchemeToEncryptionManagerMethod
        {
            [Theory]
            [InlineData("bolt")]
            [InlineData("neo4j")]
            public void ShouldBeNoEncryptionNoTrust(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var manager = raw.ParseUriSchemeToEncryptionManager(null);

                manager.UseTls.Should().BeFalse();
                manager.TrustManager.Should().BeNull();
            }

            [Theory]
            [InlineData("bolt+s")]
            [InlineData("neo4j+s")]
            public void ShouldBeEncryptionWithChainTrust(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var log = new Mock<ILogger>().Object;
                var manager = raw.ParseUriSchemeToEncryptionManager(log);

                manager.UseTls.Should().BeTrue();
                manager.TrustManager.Should().BeOfType<ChainTrustManager>();
                manager.TrustManager.Logger.Should().Be(log);
            }

            [Theory]
            [InlineData("bolt+ssc")]
            [InlineData("neo4j+ssc")]
            public void ShouldBeEncryptionWithInsecureTrust(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var log = new Mock<ILogger>().Object;
                var manager = raw.ParseUriSchemeToEncryptionManager(log);

                manager.UseTls.Should().BeTrue();
                manager.TrustManager.Should().BeOfType<InsecureTrustManager>();
                manager.TrustManager.Logger.Should().Be(log);
            }

            [Theory]
            [InlineData("bolt+ss")]
            [InlineData("bolts")]
            [InlineData("neo4js")]
            [InlineData("neo4j-ssc")]
            public void ShouldErrorForUnknownBoltUri(string scheme)
            {
                var raw = new Uri($"{scheme}://localhost/?");
                var ex = Record.Exception(() => raw.ParseUriSchemeToEncryptionManager(null));
                ex.Should().BeOfType<NotSupportedException>();
            }
        }
    }
}
