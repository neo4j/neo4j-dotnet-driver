﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector.Trust;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.Util;
using Xunit;

namespace Neo4j.Driver.Tests.TestUtil;

public class Neo4jUriTests
{
    public class ParseRoutingContextMethod
    {
        private const int DefaultBoltPort = 7687;

        [Theory]
        [InlineData("bolt")]
        public void ShouldParseEmptyRoutingContext(string scheme)
        {
            var raw = new Uri($"{scheme}://localhost/?");
            var routingContext = Neo4jUri.ParseRoutingContext(raw, DefaultBoltPort);
            routingContext.Should().BeNull();
        }

        [Theory]
        [InlineData("neo4j")]
        public void ShouldParseDefaultEntryRoutingContext(string scheme)
        {
            var raw = new Uri($"{scheme}://localhost/?");
            var routingContext = Neo4jUri.ParseRoutingContext(raw, DefaultBoltPort);

            routingContext.Should().HaveCount(1);
            routingContext["address"].Should().Be("localhost:7687");
        }

        [Theory]
        [InlineData("neo4j")]
        public void ShouldParseMultipleRoutingContext(string scheme)
        {
            var raw = new Uri($"{scheme}://localhost:7687/cat?name=molly&age=1&color=white");
            var routingContext = Neo4jUri.ParseRoutingContext(raw, DefaultBoltPort);

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
            var routingContext = Neo4jUri.ParseRoutingContext(raw, DefaultBoltPort);

            routingContext["name"].Should().Be("molly");
        }

        [Theory]
        [InlineData("neo4j")]
        public void ShouldErrorIfMissingValue(string scheme)
        {
            var raw = new Uri($"{scheme}://localhost:7687/cat?name=");
            var exception = Record.Exception(() => Neo4jUri.ParseRoutingContext(raw, DefaultBoltPort));
            exception.Should().BeOfType<ArgumentException>();
            exception.Message.Should().Contain("Invalid parameters: 'name=' in URI");
        }

        [Theory]
        [InlineData("neo4j")]
        public void ShouldErrorIfDuplicateKey(string scheme)
        {
            var raw = new Uri($"{scheme}://localhost:7687/cat?name=molly&name=mostly_white");
            var exception = Record.Exception(() => Neo4jUri.ParseRoutingContext(raw, DefaultBoltPort));
            exception.Should().BeOfType<ArgumentException>();
            exception.Message.Should().Contain("Duplicated query parameters with key 'name'");
        }

        [Theory]
        [InlineData("neo4j", "localhost:1234", "localhost:1234")]
        [InlineData("neo4j", "g.example.com", "g.example.com:7687")]
        [InlineData("neo4j", "203.0.113.254", "203.0.113.254:7687")]
        [InlineData("neo4j", "[2001:DB8::]", "[2001:db8::]:7687")]
        [InlineData("neo4j", "localhost", "localhost:7687")]
        public void ShouldContainAddressContext(string scheme, string address, string expectedAddress)
        {
            var raw = new Uri($"{scheme}://{address}");
            var routingContext = Neo4jUri.ParseRoutingContext(raw, DefaultBoltPort);

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
            var isSimple = Neo4jUri.IsSimpleUriScheme(raw);

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
            var isSimple = Neo4jUri.IsSimpleUriScheme(raw);

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
            var ex = Record.Exception(() => Neo4jUri.IsSimpleUriScheme(raw));
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
            var isRoutingUri = Neo4jUri.IsRoutingUri(raw);

            isRoutingUri.Should().BeTrue();
        }

        [Theory]
        [InlineData("bolt")]
        [InlineData("bolt+s")]
        [InlineData("bolt+ssc")]
        public void ShouldNotBeRoutingUri(string scheme)
        {
            var raw = new Uri($"{scheme}://localhost/?");
            var isRoutingUri = Neo4jUri.IsRoutingUri(raw);

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
            var ex = Record.Exception(() => Neo4jUri.IsRoutingUri(raw));
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
            var manager = Neo4jUri.ParseUriSchemeToEncryptionManager(raw, NullLogger.Instance);

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
            var manager = Neo4jUri.ParseUriSchemeToEncryptionManager(raw, log);

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
            var manager = Neo4jUri.ParseUriSchemeToEncryptionManager(raw, log);

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
            var ex = Record.Exception(() => Neo4jUri.ParseUriSchemeToEncryptionManager(raw, NullLogger.Instance));
            ex.Should().BeOfType<NotSupportedException>();
        }
    }
}

public class BoltRoutingUriMethod
{
    [Theory]
    [InlineData("localhost", "localhost", Neo4jUri.DefaultBoltPort)]
    [InlineData("localhost:9193", "localhost", 9193)]
    [InlineData("neo4j.com", "neo4j.com", Neo4jUri.DefaultBoltPort)]
    [InlineData("royal-server.com.uk", "royal-server.com.uk", Neo4jUri.DefaultBoltPort)]
    [InlineData("royal-server.com.uk:4546", "royal-server.com.uk", 4546)]
    // IPv4
    [InlineData("127.0.0.1", "127.0.0.1", Neo4jUri.DefaultBoltPort)]
    [InlineData("8.8.8.8:8080", "8.8.8.8", 8080)]
    [InlineData("0.0.0.0", "0.0.0.0", Neo4jUri.DefaultBoltPort)]
    [InlineData("192.0.2.235:4329", "192.0.2.235", 4329)]
    [InlineData("172.31.255.255:255", "172.31.255.255", 255)]
    // IPv6
    [InlineData("[1afc:0:a33:85a3::ff2f]", "[1afc:0:a33:85a3::ff2f]", Neo4jUri.DefaultBoltPort)]
    [InlineData("[::1]:1515", "[::1]", 1515)]
    [InlineData("[ff0a::101]:8989", "[ff0a::101]", 8989)]
    // IPv6 with zone id
    [InlineData("[1afc:0:a33:85a3::ff2f%eth1]", "[1afc:0:a33:85a3::ff2f]", Neo4jUri.DefaultBoltPort)]
    [InlineData("[::1%eth0]:3030", "[::1]", 3030)]
    [InlineData("[ff0a::101%8]:4040", "[ff0a::101]", 4040)]
    public void ShouldHaveLocalhost(string input, string host, int port)
    {
        var uri = Neo4jUri.BoltRoutingUri(input);
        uri.Scheme.Should().Be("neo4j");
        uri.Host.Should().Be(host);
        uri.Port.Should().Be(port);
    }
}
