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

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Extensions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.TestUtil;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Stub;

public sealed class MultiDatabasesTests
{
    private readonly Action<ConfigBuilder> _setupConfig;

    public MultiDatabasesTests(ITestOutputHelper output)
    {
        _setupConfig = o => o.WithLogger(TestLogger.Create(output));
    }

    [Fact]
    public async Task ShouldDiscoverEndpointsForADatabaseAndRead()
    {
        using var _ = BoltStubServer.Start("V4/acquire_endpoints_aDatabase", 9001);
        using var __ = BoltStubServer.Start("V4/read_from_aDatabase", 9005);
        await using var driver = GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, _setupConfig);

        await using var session = driver.AsyncSession(
            o =>
                o.WithDatabase("aDatabase").WithDefaultAccessMode(AccessMode.Read));

        var cursor =
            await session.RunAsync("MATCH (n) RETURN n.name");

        var result = await cursor.ToListAsync(r => r[0].As<string>());

        result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
    }

    [Fact]
    public async Task ShouldDiscoverEndpointsForADatabaseAndWrite()
    {
        using var _ = BoltStubServer.Start("V4/acquire_endpoints_aDatabase", 9001);
        using var __ = BoltStubServer.Start("V4/write_to_aDatabase", 9007);
        await using var driver = GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, _setupConfig);

        await using var session = driver.AsyncSession(
            o =>
                o.WithDatabase("aDatabase").WithDefaultAccessMode(AccessMode.Write));

        await session.RunAndConsumeAsync("CREATE (n {name:'Bob'})");
    }

    [Fact]
    public async Task ShouldDiscoverEndpointsForDefaultDatabase()
    {
        using var _ = BoltStubServer.Start("V4/acquire_endpoints_default_database", 9001);
        using var __ = BoltStubServer.Start("V4/read", 9005);
        await using var driver =
            GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, _setupConfig);

        await using var session = driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
        var cursor =
            await session.RunAsync("MATCH (n) RETURN n.name");

        var result = await cursor.ToListAsync(r => r[0].As<string>());

        result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
    }

    [Fact]
    public async Task ShouldThrowOnInvalidRoutingTable()
    {
        using var _ = BoltStubServer.Start("V4/acquire_endpoints_aDatabase_no_servers", 9001);

        var exception = await Record.ExceptionAsync(
            async () =>
            {
                await using var driver =
                    GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, _setupConfig);

                await using var session = driver.AsyncSession(
                    o =>
                        o.WithDatabase("aDatabase").WithDefaultAccessMode(AccessMode.Read));

                await session.RunAsync("MATCH (n) RETURN n.name");
            });

        exception
            .Should()
            .BeOfType<ServiceUnavailableException>()
            .Which.Message.Should()
            .Be("Failed to connect to any routing server.*");
    }

    [Fact]
    public async Task ShouldThrowOnDatabaseNotFound()
    {
        using var _ = BoltStubServer.Start("V4/acquire_endpoints_db_not_found", 9001);

        var exception = await Record.ExceptionAsync(
            async () =>
            {
                await using var driver = GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, _setupConfig);

                await using var session = driver.AsyncSession(
                    o =>
                        o.WithDatabase("aDatabase").WithDefaultAccessMode(AccessMode.Read));

                var cursor =
                    await session.RunAsync("MATCH (n) RETURN n.name");

                var result = await cursor.ToListAsync(r => r[0].As<string>());

                result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
            });

        exception
            .Should()
            .BeOfType<FatalDiscoveryException>()
            .Which.Message.Should()
            .Be("database not found");
    }

    [Fact]
    public async Task ShouldDiscoverEndpointsForADatabaseWithBookmarks()
    {
        using var _ = BoltStubServer.Start("V4/acquire_endpoints_aDatabase_with_bookmark", 9001);
        using var __ = BoltStubServer.Start("V4/read_from_aDatabase_with_bookmark", 9005);
        var bookmark1 = Bookmarks.From("system:1111");
        var bookmark2 = Bookmarks.From("aDatabase:5555");

        await using var driver =
            GraphDatabase.Driver("neo4j://127.0.0.1:9001", AuthTokens.None, _setupConfig);

        await using var session = driver.AsyncSession(
            o =>
                o.WithDatabase("aDatabase")
                    .WithDefaultAccessMode(AccessMode.Read)
                    .WithBookmarks(bookmark1, bookmark2));

        var cursor =
            await session.RunAsync("MATCH (n) RETURN n.name");

        var result = await cursor.ToListAsync(r => r[0].As<string>());

        result.Should().BeEquivalentTo("Bob", "Alice", "Tina");
    }

    [RequireBoltStubServerTheory]
    [InlineData("V3", "neo4j")]
    [InlineData("V3", "bolt")]
    [InlineData("V4", "neo4j")]
    [InlineData("V4", "bolt")]
    public async Task ShouldDetectMultiDatabasesFeature(string boltVersion, string scheme)
    {
        using var _ = BoltStubServer.Start($"{boltVersion}/supports_multidb", 9001);
        await using var driver = GraphDatabase.Driver($"{scheme}://127.0.0.1:9001", AuthTokens.None, _setupConfig);
        var support = await driver.SupportsMultiDbAsync();
        if (boltVersion.Equals("V3"))
        {
            support.Should().BeFalse();
        }
        else
        {
            support.Should().BeTrue();
        }
    }

    [RequireBoltStubServerTheory]
    [InlineData("neo4j")]
    [InlineData("bolt")]
    public async Task ShouldFailToDetectMultiDatabasesFeature(string scheme)
    {
        await using var driver = GraphDatabase.Driver($"{scheme}://127.0.0.1:9099", AuthTokens.None, _setupConfig);
        var error = await Record.ExceptionAsync(() => driver.SupportsMultiDbAsync());
        error.Should().BeOfType<ServiceUnavailableException>();
    }

    [RequireBoltStubServerTheory]
    [InlineData("V3", "neo4j")]
    [InlineData("V3", "bolt")]
    [InlineData("V4", "neo4j")]
    [InlineData("V4", "bolt")]
    public async Task ShouldThrowSecurityErrorWhenFailToHello(string boltVersion, string scheme)
    {
        using var _ = BoltStubServer.Start($"{boltVersion}/fail_to_auth", 9001);
        await using var driver = GraphDatabase.Driver($"{scheme}://127.0.0.1:9001", AuthTokens.None, _setupConfig);
        var error = await Record.ExceptionAsync(() => driver.SupportsMultiDbAsync());
        error.Should().BeOfType<AuthenticationException>().Which.Message.Should().StartWith("blabla");
    }
}
