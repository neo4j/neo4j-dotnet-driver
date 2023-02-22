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

using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Extensions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.TestUtil;
using Xunit;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.Internals.VersionComparison;
using static Neo4j.Driver.SessionConfigBuilder;

namespace Neo4j.Driver.IntegrationTests.Routing;

public sealed class BoltV4IT : RoutingDriverTestBase
{
    private readonly IDriver _driver;

    public BoltV4IT(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture)
        : base(output, fixture)
    {
        _driver = GraphDatabase.Driver(
            Cluster.BoltRoutingUri,
            Cluster.AuthToken,
            o => o.WithLogger(TestLogger.Create(output)));
    }

    [RequireClusterFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldReturnDatabaseInfoForDefaultDatabaseInTxFunc()
    {
        await VerifyDatabaseNameOnSummaryTxFunc(null, "neo4j");
    }

    [RequireClusterFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldReturnDatabaseInfoForDefaultDatabaseWhenSpecifiedInTxFunc()
    {
        await VerifyDatabaseNameOnSummaryTxFunc("neo4j", "neo4j");
    }

    //[RequireClusterFact("4.0.0", GreaterThanOrEqualTo)]
    [RequireClusterFact(Skip = "Requires server fix")]
    public async Task ShouldReturnDatabaseInfoForDatabaseInTxFunc()
    {
        var bookmark = await CreateDatabase(_driver, "foo");

        try
        {
            await VerifyDatabaseNameOnSummaryTxFunc("foo", "foo", bookmark);
        }
        finally
        {
            await DropDatabase(_driver, "foo", bookmark);
        }
    }

    //[RequireClusterFact("4.0.0", GreaterThanOrEqualTo)]
    [RequireClusterFact(Skip = "Requires server fix")]
    public async Task ShouldReturnDatabaseInfoForDatabaseInAutoCommit()
    {
        var dbname = "foo";
        Bookmarks bookmarks;

        await using (var initial = _driver.AsyncSession(ForDatabase("system")))
        {
            await initial.RunAsync(new Query($"CREATE DATABASE {dbname}"));
            bookmarks = initial.LastBookmarks;
        }

        await using var session = _driver.AsyncSession(
            o =>
            {
                if (!string.IsNullOrEmpty(dbname))
                {
                    o.WithDatabase(dbname);
                }

                o.WithBookmarks(bookmarks ?? Bookmarks.Empty);
            });

        var result = await session.RunAsync(new Query("RETURN 1"));
        var summary = await result.ConsumeAsync();
        summary.Database.Should().NotBeNull();
        summary.Database.Name.Should().Be(dbname);
    }

    [RequireClusterFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldThrowForNonExistentDatabaseInTxFunc()
    {
        var exception = await Record.ExceptionAsync(() => VerifyDatabaseNameOnSummaryTxFunc("bar", "bar"));
        exception.Should().BeOfType<ClientException>().Which.Message.Should().Be("*database does not exist*");
    }

    [RequireClusterFact("4.0.0", VersionComparison.LessThan)]
    public async Task ShouldThrowWhenDatabaseIsSpecifiedInTxFunc()
    {
        var exception = await Record.ExceptionAsync(() => VerifyDatabaseNameOnSummaryTxFunc("bar", "bar"));
        exception.Should()
            .BeOfType<ClientException>()
            .Which.Message.Should()
            .Be("*to a server that does not support multiple databases.*");
    }

    private async Task VerifyDatabaseNameOnSummaryTxFunc(string name, string expected, Bookmarks bookmarks = null)
    {
        await using var session = _driver.AsyncSession(
            o =>
            {
                if (!string.IsNullOrEmpty(name))
                {
                    o.WithDatabase(name);
                }

                o.WithBookmarks(bookmarks ?? Bookmarks.Empty);
            });

        var summary =
            await session.ExecuteReadAsync(txc => txc.RunAndConsumeAsync("RETURN 1"));

        summary.Database.Should().NotBeNull();
        summary.Database.Name.Should().Be(expected);
    }

    private static async Task<Bookmarks> CreateDatabase(IDriver driver, string name)
    {
        await using var session = driver.AsyncSession(ForDatabase("system"));
        await session.ExecuteWriteAsync(async txc => await txc.RunAndConsumeAsync($"CREATE DATABASE {name}"));
        return session.LastBookmarks;
    }

    private static async Task DropDatabase(IDriver driver, string name, Bookmarks bookmarks)
    {
        await using var session = driver.AsyncSession(o => o.WithDatabase("system").WithBookmarks(bookmarks));
        await session.ExecuteWriteAsync(async txc => await txc.RunAndConsumeAsync($"DROP DATABASE {name}"));
    }
}
