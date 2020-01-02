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
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class DefaultResolverTests
    {

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ShouldResolve(bool ipv6Preferred)
        {
            var resolverMock = new Mock<IHostResolver>();
            var resolver = new DefaultHostResolver(resolverMock.Object, ipv6Preferred);

            resolver.Resolve("some_host");

            resolverMock.Verify(r => r.Resolve("some_host"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void ShouldResolveAsync(bool ipv6Preferred)
        {
            var resolverMock = new Mock<IHostResolver>();
            var resolver = new DefaultHostResolver(resolverMock.Object, ipv6Preferred);

            await resolver.ResolveAsync("some_host");

            resolverMock.Verify(r => r.ResolveAsync("some_host"));
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ShouldParseLocalhost(bool ipv6Preferred)
        {
            var resolver = new DefaultHostResolver(ipv6Preferred);
            var ipAddresses = resolver.Resolve("LocALhOsT");

#if NET452
            ipAddresses.Should().Contain(IPAddress.IPv6Loopback);
            ipAddresses.Should().Contain(IPAddress.Loopback);
            ipAddresses.Should().Contain(IPAddress.Parse("[::1]"));
#endif
            ipAddresses.Should().Contain(IPAddress.Parse("127.0.0.1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void ShouldParseLocalhostAsync(bool ipv6Preferred)
        {
            var resolver = new DefaultHostResolver(ipv6Preferred);
            var ipAddresses = await resolver.ResolveAsync("LocALhOsT");

#if NET452
            ipAddresses.Should().Contain(IPAddress.IPv6Loopback);
            ipAddresses.Should().Contain(IPAddress.Loopback);
            ipAddresses.Should().Contain(IPAddress.Parse("[::1]"));
#endif
            ipAddresses.Should().Contain(IPAddress.Parse("127.0.0.1"));
        }

        [Fact]
        public void ShouldParseLoopback()
        {
            var resolver = new DefaultHostResolver(false);
            var ipAddresses = resolver.Resolve("127.0.0.1");

            ipAddresses.Should().HaveCount(1).And.Contain(IPAddress.Loopback);
        }

        [Fact]
        public void ShouldParseLoopbackWhenIPv6Preferred()
        {
            var resolver = new DefaultHostResolver(true);
            var ipAddresses = resolver.Resolve("127.0.0.1");

            ipAddresses.Should().HaveCount(2).And.ContainInOrder(IPAddress.IPv6Loopback, IPAddress.Loopback);
        }

        [Fact]
        public async void ShouldParseLoopbackAsync()
        {
            var resolver = new DefaultHostResolver(false);
            var ipAddresses = await resolver.ResolveAsync("127.0.0.1");

            ipAddresses.Should().HaveCount(1).And.Contain(IPAddress.Loopback);
        }

        [Fact]
        public async void ShouldParseLoopbackWhenIPv6PreferredAsync()
        {
            var resolver = new DefaultHostResolver(true);
            var ipAddresses = await resolver.ResolveAsync("127.0.0.1");

            ipAddresses.Should().HaveCount(2).And.ContainInOrder(IPAddress.IPv6Loopback, IPAddress.Loopback);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ShouldParseIPv6Loopback(bool ipv6Preferred)
        {
            var resolver = new DefaultHostResolver(ipv6Preferred);
            var ipAddresses = resolver.Resolve("[::1]");

            ipAddresses.Should().HaveCount(1).And.Contain(IPAddress.IPv6Loopback);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void ShouldParseIPv6LoopbackAsync(bool ipv6Preferred)
        {
            var resolver = new DefaultHostResolver(ipv6Preferred);
            var ipAddresses = await resolver.ResolveAsync("[::1]");

            ipAddresses.Should().HaveCount(1).And.Contain(IPAddress.IPv6Loopback);
        }
        
        [Fact]
        public void ShouldOrderIPv4First()
        {
            var resolverMock = new Mock<IHostResolver>();
            resolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(new[]
                {IPAddress.Parse("[::1]"),IPAddress.Parse("10.0.0.1"), IPAddress.Parse("192.168.0.11")});

            var resolver = new DefaultHostResolver(resolverMock.Object, false);
            var ipAddresses = resolver.Resolve("some_host");

            ipAddresses.Should().HaveCount(3).And.ContainInOrder(IPAddress.Parse("10.0.0.1"),
                IPAddress.Parse("192.168.0.11"), IPAddress.Parse("[::1]"));
        }

        [Fact]
        public async void ShouldOrderIPv4FirstAsync()
        {
            var resolverMock = new Mock<IHostResolver>();
            resolverMock.Setup(x => x.ResolveAsync(It.IsAny<string>())).Returns(Task.FromResult(new[]
                {IPAddress.Parse("[::1]"), IPAddress.Parse("10.0.0.1"), IPAddress.Parse("192.168.0.11")}));

            var resolver = new DefaultHostResolver(resolverMock.Object, false);
            var ipAddresses = await resolver.ResolveAsync("some_host");

            ipAddresses.Should().HaveCount(3).And.ContainInOrder(IPAddress.Parse("10.0.0.1"),
                IPAddress.Parse("192.168.0.11"), IPAddress.Parse("[::1]"));
        }

        [Fact]
        public void ShouldOrderIPv6FirstWhenIPv6Preferred()
        {
            var resolverMock = new Mock<IHostResolver>();
            resolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(new[]
                {IPAddress.Parse("10.0.0.1"), IPAddress.Parse("[::1]"), IPAddress.Parse("192.168.0.11")});

            var resolver = new DefaultHostResolver(resolverMock.Object, true);
            var ipAddresses = resolver.Resolve("some_host");

            ipAddresses.Should().HaveCount(3).And.ContainInOrder(IPAddress.Parse("[::1]"), IPAddress.Parse("10.0.0.1"),
                IPAddress.Parse("192.168.0.11"));
        }

        [Fact]
        public async void ShouldOrderIPv6FirstWhenIPv6PreferredAsync()
        {
            var resolverMock = new Mock<IHostResolver>();
            resolverMock.Setup(x => x.ResolveAsync(It.IsAny<string>())).Returns(Task.FromResult(new[]
                {IPAddress.Parse("10.0.0.1"), IPAddress.Parse("[::1]"), IPAddress.Parse("192.168.0.11")}));

            var resolver = new DefaultHostResolver(resolverMock.Object, true);
            var ipAddresses = await resolver.ResolveAsync("some_host");

            ipAddresses.Should().HaveCount(3).And.ContainInOrder(IPAddress.Parse("[::1]"), IPAddress.Parse("10.0.0.1"),
                IPAddress.Parse("192.168.0.11"));
        }

        [MonoTheory]
        [InlineData(true)]
        [InlineData(false)]
        public void ShouldNotResolveLocalhostOnMono(bool ipv6Preferred)
        {
            var resolverMock = new Mock<IHostResolver>(MockBehavior.Strict);
            var resolver = new DefaultHostResolver(resolverMock.Object, ipv6Preferred);

            resolver.Resolve("localhost");
        }

        [MonoTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async void ShouldNotResolveLocalhostOnMonoAsync(bool ipv6Preferred)
        {
            var resolverMock = new Mock<IHostResolver>(MockBehavior.Strict);
            var resolver = new DefaultHostResolver(resolverMock.Object, ipv6Preferred);

            await resolver.ResolveAsync("localhost");
        }
        
    }
}