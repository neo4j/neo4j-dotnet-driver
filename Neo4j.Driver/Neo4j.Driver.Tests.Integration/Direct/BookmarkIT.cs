// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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

using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct;

public class BookmarkIT : DirectDriverTestBase
{
    private const string BookmarkHeader = "neo4j:bookmark:v1:tx";

    public BookmarkIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
    {
    }

    private IDriver Driver => Server.Driver;

    [RequireServerFact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
    public async Task ShouldContainLastBookmarkAfterTx()
    {
        var session = Driver.AsyncSession();

        try
        {
            session.LastBookmark.Should().BeNull();

            await CreateNodeInTx(session, 1);

            session.LastBookmark.Should().NotBeNull();
            session.LastBookmark.Values.Should().NotBeEmpty();
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
    public async Task BookmarkUnchangedAfterRolledBackTx()
    {
        var session = Driver.AsyncSession();
        try
        {
            await CreateNodeInTx(session, 1);

            var bookmark = session.LastBookmark;
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

            session.LastBookmark.Should().Be(bookmark);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
    public async Task BookmarkUnchangedAfterTxFailure()
    {
        var session = Driver.AsyncSession();
        try
        {
            await CreateNodeInTx(session, 1);

            var bookmark = session.LastBookmark;
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

            session.LastBookmark.Should().Be(bookmark);
        }
        finally
        {
            await session.CloseAsync();
        }
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

    private static async Task<int> CountNodeInTx(IDriver driver, int id, Bookmarks bookmarks = null)
    {
        var session = driver.AsyncSession(o => o.WithBookmarks(bookmarks));
        try
        {
            var tx = await session.BeginTransactionAsync();
            try
            {
                var cursor = await tx.RunAsync("MATCH (a:Person {id: $id}) RETURN a", new { id });
                var records = await cursor.ToListAsync();
                await tx.CommitAsync();
                return records.Count;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        finally
        {
            await session.CloseAsync();
        }
    }
}
