// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Threading;
using System.Threading.Tasks;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Xunit;

namespace Neo4j.Driver.Tests.Connector;

public class CachingResolverTests
{
    [Fact]
    public void ShouldResolve()
    {
        var resolverMock = new Mock<IHostResolver>();
        var resolver = new CachingHostResolver(resolverMock.Object, 1000);

        resolver.Resolve("localhost");

        resolverMock.Verify(x => x.Resolve("localhost"));
    }

    [Fact]
    public async void ShouldResolveAsync()
    {
        var resolverMock = new Mock<IHostResolver>();
        var resolver = new CachingHostResolver(resolverMock.Object, 1000);

        await resolver.ResolveAsync("localhost");

        resolverMock.Verify(x => x.ResolveAsync("localhost"));
    }

    [Fact]
    public void ShouldResolveOnce()
    {
        var resolverMock = new Mock<IHostResolver>();
        var resolver = new CachingHostResolver(resolverMock.Object, 5000);

        resolver.Resolve("localhost");
        resolver.Resolve("localhost");

        resolverMock.Verify(x => x.Resolve("localhost"), Times.Once);
    }

    [Fact]
    public async void ShouldResolveOnceAsync()
    {
        var resolverMock = new Mock<IHostResolver>();
        var resolver = new CachingHostResolver(resolverMock.Object, 5000);

        await resolver.ResolveAsync("localhost");
        await resolver.ResolveAsync("localhost");

        resolverMock.Verify(x => x.ResolveAsync("localhost"), Times.Once);
    }

    [Fact]
    public async void ShouldResolveOnceMixedAsyncFirst()
    {
        var resolverMock = new Mock<IHostResolver>();
        var resolver = new CachingHostResolver(resolverMock.Object, 5000);

        await resolver.ResolveAsync("localhost");
        resolver.Resolve("localhost");
        await resolver.ResolveAsync("localhost");
        resolver.Resolve("localhost");

        resolverMock.Verify(x => x.ResolveAsync("localhost"), Times.Once);
    }

    [Fact]
    public async void ShouldResolveOnceMixedSyncFirst()
    {
        var resolverMock = new Mock<IHostResolver>();
        var resolver = new CachingHostResolver(resolverMock.Object, 5000);

        resolver.Resolve("localhost");
        await resolver.ResolveAsync("localhost");
        resolver.Resolve("localhost");
        await resolver.ResolveAsync("localhost");

        resolverMock.Verify(x => x.Resolve("localhost"), Times.Once);
    }

    [Fact]
    public void ShouldExpireCached()
    {
        var resolverMock = new Mock<IHostResolver>();
        var resolver = new CachingHostResolver(resolverMock.Object, 1000);

        resolver.Resolve("localhost");
        Thread.Sleep(1500);
        resolver.Resolve("localhost");

        resolverMock.Verify(x => x.Resolve("localhost"), Times.Exactly(2));
    }

    [Fact]
    public async void ShouldExpireCachedAsync()
    {
        var resolverMock = new Mock<IHostResolver>();
        var resolver = new CachingHostResolver(resolverMock.Object, 1000);

        await resolver.ResolveAsync("localhost");
        await Task.Delay(1500);
        await resolver.ResolveAsync("localhost");

        resolverMock.Verify(x => x.ResolveAsync("localhost"), Times.Exactly(2));
    }
}
