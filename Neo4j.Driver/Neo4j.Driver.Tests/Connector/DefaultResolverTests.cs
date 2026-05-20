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

using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Connector.Resolvers;
using Neo4j.Driver.Tests.Filters;
using Xunit;

namespace Neo4j.Driver.Tests.Connector;

public class DefaultResolverTests
{
    [Fact]
    public void ShouldResolve()
    {
        var resolverMock = new Mock<IHostResolver>();
        var resolver = new DefaultHostResolver(resolverMock.Object);

        resolver.Resolve("some_host");

        resolverMock.Verify(r => r.Resolve("some_host"));
    }

    [Fact]
    public async Task ShouldResolveAsync()
    {
        var resolverMock = new Mock<IHostResolver>();
        var resolver = new DefaultHostResolver(resolverMock.Object);

        await resolver.ResolveAsync("some_host");

        resolverMock.Verify(r => r.ResolveAsync("some_host"));
    }

    [Fact]
    public void ShouldParseLocalhost()
    {
        var resolver = new DefaultHostResolver();
        var ipAddresses = resolver.Resolve("LocALhOsT");

        ipAddresses.Should().Contain(IPAddress.IPv6Loopback);
        ipAddresses.Should().Contain(IPAddress.Loopback);
        ipAddresses.Should().Contain(IPAddress.Parse("[::1]"));
        ipAddresses.Should().Contain(IPAddress.Parse("127.0.0.1"));
    }

    [Fact]
    public async Task ShouldParseLocalhostAsync()
    {
        var resolver = new DefaultHostResolver();
        var ipAddresses = await resolver.ResolveAsync("LocALhOsT");

        ipAddresses.Should().Contain(IPAddress.IPv6Loopback);
        ipAddresses.Should().Contain(IPAddress.Loopback);
        ipAddresses.Should().Contain(IPAddress.Parse("[::1]"));
        ipAddresses.Should().Contain(IPAddress.Parse("127.0.0.1"));
    }
    
    [Fact]
    public void ShouldParseLoopback()
    {
        var resolver = new DefaultHostResolver();
        var ipAddresses = resolver.Resolve("127.0.0.1");

        ipAddresses.Should().HaveCount(2).And.ContainInOrder(IPAddress.IPv6Loopback, IPAddress.Loopback);
    }
    
    [Fact]
    public async Task ShouldParseLoopbackAsync()
    {
        var resolver = new DefaultHostResolver();
        var ipAddresses = await resolver.ResolveAsync("127.0.0.1");

        ipAddresses.Should().HaveCount(2).And.ContainInOrder(IPAddress.IPv6Loopback, IPAddress.Loopback);
    }

    [Fact]
    public void ShouldParseIPv6Loopback()
    {
        var resolver = new DefaultHostResolver();
        var ipAddresses = resolver.Resolve("[::1]");

        ipAddresses.Should().HaveCount(1).And.Contain(IPAddress.IPv6Loopback);
    }

    [Fact]
    public async Task ShouldParseIPv6LoopbackAsync()
    {
        var resolver = new DefaultHostResolver();
        var ipAddresses = await resolver.ResolveAsync("[::1]");

        ipAddresses.Should().HaveCount(1).And.Contain(IPAddress.IPv6Loopback);
    }        
    
    [MonoFact]
    public void ShouldNotResolveLocalhostOnMono()
    {
        var resolverMock = new Mock<IHostResolver>(MockBehavior.Strict);
        var resolver = new DefaultHostResolver(resolverMock.Object);

        resolver.Resolve("localhost");
    }

    [MonoFact]
    public async Task ShouldNotResolveLocalhostOnMonoAsync()
    {
        var resolverMock = new Mock<IHostResolver>(MockBehavior.Strict);
        var resolver = new DefaultHostResolver(resolverMock.Object);

        await resolver.ResolveAsync("localhost");
    }
}
