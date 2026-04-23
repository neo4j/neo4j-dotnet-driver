// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Routing;
using Xunit;

namespace Neo4j.Driver.Tests.Routing;

public class InitialServerAddressProviderTests
{
    [Fact]
    public void ShouldUsePortFromResolvedAddress()
    {
        // Driver URI has port 9999, resolver returns localhost:7687.
        // The resolved port (7687) must be used — not the driver URI's port.
        var initUri = new Uri("neo4j://example.com:9999");
        var resolver = new ListAddressResolver(ServerAddress.From("localhost", 7687));
        var provider = new InitialServerAddressProvider(initUri, resolver);

        var uris = provider.Get();

        uris.Should().ContainSingle(because: "resolver returned one address")
            .Which.Port.Should().Be(7687, because: "the resolver specified port 7687");
    }

    [Fact]
    public void ShouldUseHostFromResolvedAddress()
    {
        var initUri = new Uri("neo4j://example.com:9999");
        var resolver = new ListAddressResolver(ServerAddress.From("localhost", 7687));
        var provider = new InitialServerAddressProvider(initUri, resolver);

        var uris = provider.Get();

        uris.Should().ContainSingle()
            .Which.Host.Should().Be("localhost", because: "the resolver specified localhost");
    }
}
