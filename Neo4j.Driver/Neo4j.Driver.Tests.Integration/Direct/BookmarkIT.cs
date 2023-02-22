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
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct;

public sealed class BookmarkIT : DirectDriverTestBase
{
    public BookmarkIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
    {
    }

    private IDriver Driver => Server.Driver;

    [RequireServerFact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
    public async Task ShouldContainLastBookmarkAfterTx()
    {
        await using var session = Driver.AsyncSession();

        session.LastBookmarks.Should().BeNull();

        await CreateNodeInTx(session, 1);

        session.LastBookmarks.Should().NotBeNull();
        session.LastBookmarks.Values.Should().NotBeEmpty();
    }

    [RequireServerFact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
    public async Task BookmarkUnchangedAfterRolledBackTx()
    {
        await using var session = Driver.AsyncSession();
        await CreateNodeInTx(session, 1);

        var bookmark = session.LastBookmarks;
        bookmark.Should().NotBeNull();
        bookmark.Values.Should().NotBeEmpty();

        var tx = await session.BeginTransactionAsync();
        try
        {
            await tx.RunAsync("CREATE (a:Person)");
        }
        finally
        {
            await tx.RollbackAsync();
        }

        session.LastBookmarks.Should().Be(bookmark);
    }

    [RequireServerFact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
    public async Task BookmarkUnchangedAfterTxFailure()
    {
        await using var session = Driver.AsyncSession();
        await CreateNodeInTx(session, 1);

        var bookmark = session.LastBookmarks;
        bookmark.Should().NotBeNull();
        bookmark.Values.Should().NotBeEmpty();

        var tx = await session.BeginTransactionAsync();
        var exc = await Record.ExceptionAsync(
            async () =>
            {
                await tx.RunAsync("RETURN");
                await tx.CommitAsync();
            });

        exc.Should().BeOfType<ClientException>();

        session.LastBookmarks.Should().Be(bookmark);
    }

    private static async Task CreateNodeInTx(IAsyncSession session, int id)
    {
        var tx = await session.BeginTransactionAsync();
        try
        {
            await tx.RunAsync("CREATE (a:Person {id: $id})", new { id });
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
