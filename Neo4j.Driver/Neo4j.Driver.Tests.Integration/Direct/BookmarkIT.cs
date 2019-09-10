// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct
{
    public class BookmarkIT : DirectDriverTestBase
    {
        private const string BookmarkHeader = "neo4j:bookmark:v1:tx";

        private IDriver Driver => Server.Driver;

        public BookmarkIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

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
                var exc = await Record.ExceptionAsync(async () =>
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

        [RequireServerFact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
        public async Task ShouldThrowForUnreachableBookmark()
        {
            Bookmark bookmark;
            var session = Driver.AsyncSession();
            try
            {
                await CreateNodeInTx(session, 1);
                bookmark = session.LastBookmark;
            }
            finally
            {
                await session.CloseAsync();
            }

            // Config the default server bookmark_ready_timeout to be something smaller than 30s to speed up this test
            session = Driver.AsyncSession(Bookmark.From(BookmarkValue(bookmark) + "0"));
            try
            {
                var exc = await Record.ExceptionAsync(() => session.BeginTransactionAsync());

                exc.Should().BeOfType<TransientException>().Which
                    .Message.Should().Contain("not up to the requested version:");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact("3.1.0", VersionComparison.GreaterThanOrEqualTo)]
        public async Task ShouldWaitOnBookmark()
        {
            var session = Driver.AsyncSession();
            try
            {
                // get a bookmark
                session.LastBookmark.Should().BeNull();
                await CreateNodeInTx(session, 1);

                session.LastBookmark.Should().NotBeNull();
                session.LastBookmark.Values.Should().NotBeEmpty();
                var lastBookmarkNum = BookmarkNum(session.LastBookmark);

                // start a thread to create lastBookmark + 1 tx 
#pragma warning disable 4014
                Task.Factory.StartNew(async () =>
#pragma warning restore 4014
                {
                    await Task.Delay(500);
                    var anotherSession = Driver.AsyncSession();
                    try
                    {
                        await CreateNodeInTx(anotherSession, 2);
                    }
                    finally
                    {
                        await anotherSession.CloseAsync();
                    }
                });

                // wait for lastBookmark + 1
                var waitForBookmark = BookmarkNumOverride(session.LastBookmark, lastBookmarkNum + 1);
                var count = await CountNodeInTx(Driver, 2, waitForBookmark);
                count.Should().Be(1);
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
                await tx.RunAsync("CREATE (a:Person {id: $id})", new {id});
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static async Task<int> CountNodeInTx(IDriver driver, int id, Bookmark bookmark = null)
        {
            var session = driver.AsyncSession(bookmark);
            try
            {
                var tx = await session.BeginTransactionAsync();
                try
                {
                    var cursor = await tx.RunAsync("MATCH (a:Person {id: $id}) RETURN a", new {id});
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

        private static long BookmarkNum(Bookmark bookmark)
        {
            return LastBookmarkValue(bookmark).seq;
        }

        private static string BookmarkValue(Bookmark bookmark)
        {
            return LastBookmarkValue(bookmark).value;
        }

        private static Bookmark BookmarkNumOverride(Bookmark bookmark, long newSeq)
        {
            var (value, seq) = LastBookmarkValue(bookmark);
            var prefix = value.Substring(0, value.Length - seq.ToString().Length);

            return Bookmark.From(prefix + newSeq);
        }

        private static (string value, long seq) LastBookmarkValue(Bookmark bookmark)
        {
            return bookmark.Values.Select(v => (value: v, seq: Convert.ToInt64(ExtractNumber(v))))
                .OrderByDescending(v => v.seq).First();
        }

        private static string ExtractNumber(string value)
        {
            return Regex.Match(value, @"\D+(\d+)$").Groups[1].Value;
        }
    }
}