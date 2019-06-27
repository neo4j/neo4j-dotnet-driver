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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver;
using Xunit;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.VersionComparison;

namespace Neo4j.Driver.IntegrationTests
{
    public class BookmarkIT : DirectDriverTestBase
    {
        private const string BookmarkHeader = "neo4j:bookmark:v1:tx";

        private IDriver Driver => Server.Driver;

        public BookmarkIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerFact("3.1.0", GreaterThanOrEqualTo)]
        public async Task ShouldContainLastBookmarkAfterTx()
        {
            var session = Driver.Session();

            try
            {
                session.LastBookmark.Should().BeNull();

                await CreateNodeInTx(session, 1);

                session.LastBookmark.Should().NotBeNull();
                session.LastBookmark.Should().StartWith("neo4j:bookmark:v1:tx");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact("3.1.0", GreaterThanOrEqualTo)]
        public async Task BookmarkUnchangedAfterRolledBackTx()
        {
            var session = Driver.Session();
            try
            {
                await CreateNodeInTx(session, 1);

                var bookmark = session.LastBookmark;
                bookmark.Should().NotBeNullOrEmpty();

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

        [RequireServerFact("3.1.0", GreaterThanOrEqualTo)]
        public async Task BookmarkUnchangedAfterTxFailure()
        {
            var session = Driver.Session();
            try
            {
                await CreateNodeInTx(session, 1);

                var bookmark = session.LastBookmark;
                bookmark.Should().NotBeNullOrEmpty();

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

        [RequireServerFact("3.1.0", GreaterThanOrEqualTo)]
        public async Task ShouldIgnoreInvalidBookmark()
        {
            var session = Driver.Session("invalid bookmark format");
            try
            {
                await session.BeginTransactionAsync();
                session.LastBookmark.Should().BeNull(); // ignored
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact("3.1.0", GreaterThanOrEqualTo)]
        public async Task ShouldThrowForUnreachableBookmark()
        {
            string bookmark;
            var session = Driver.Session();
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
            session = Driver.Session(bookmark + "0");
            try
            {
                var exc = await Record.ExceptionAsync(() => session.BeginTransactionAsync());

                exc.Should().BeOfType<TransientException>().Which
                    .Message.Should().Contain("Database not up to the requested version:");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact("3.1.0", GreaterThanOrEqualTo)]
        public async Task ShouldWaitOnBookmark()
        {
            var session = Driver.Session();
            try
            {
                // get a bookmark
                session.LastBookmark.Should().BeNull();
                await CreateNodeInTx(session, 1);

                session.LastBookmark.Should().NotBeNull().And.StartWith(BookmarkHeader);
                var lastBookmarkNum = BookmarkNum(session.LastBookmark);

                // start a thread to create lastBookmark + 1 tx 
#pragma warning disable 4014
                Task.Factory.StartNew(async () =>
#pragma warning restore 4014
                {
                    await Task.Delay(500);
                    var anotherSession = Driver.Session();
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
                var waitForBookmark = $"{BookmarkHeader}{lastBookmarkNum + 1}";
                var count = await CountNodeInTx(Driver, 2, waitForBookmark);
                count.Should().Be(1);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private static async Task CreateNodeInTx(ISession session, int id)
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

        private static async Task<int> CountNodeInTx(IDriver driver, int id, string bookmark = null)
        {
            var session = driver.Session(bookmark);
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

        private static long BookmarkNum(string bookmark)
        {
            return Convert.ToInt64(bookmark.Substring(BookmarkHeader.Length));
        }
    }
}