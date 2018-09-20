// Copyright (c) 2002-2018 "Neo4j,"
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
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class BookmarkIT : DirectDriverTestBase
    {
        private IDriver Driver => Server.Driver;

        public BookmarkIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.1.0")]
        public void ShouldContainLastBookmarkAfterTx()
        {
            using (var session = Driver.Session())
            {
                session.LastBookmark.Should().BeNull();

                CreateNodeInTx(session, 1);

                session.LastBookmark.Should().NotBeNull();
                session.LastBookmark.Should().StartWith("neo4j:bookmark:v1:tx");
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.1.0")]
        public void BookmarkUnchangedAfterRolledBackTx()
        {
            using (var session = Driver.Session())
            {
                CreateNodeInTx(session, 1);
                var bookmark = session.LastBookmark;
                bookmark.Should().NotBeNullOrEmpty();

                using (var tx = session.BeginTransaction())
                {
                    tx.Run("CREATE (a:Person)");
                    tx.Failure();
                }
                session.LastBookmark.Should().Be(bookmark);
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.1.0")]
        public void BookmarkUnchangedAfterTxFailure()
        {
            using (var session = Driver.Session())
            {
                CreateNodeInTx(session, 1);
                var bookmark = session.LastBookmark;
                bookmark.Should().NotBeNullOrEmpty();

                var tx = session.BeginTransaction();
                tx.Run("RETURN");
                tx.Success();
                var exception = Record.Exception(() => tx.Dispose());
                exception.Should().BeOfType<ClientException>();
                session.LastBookmark.Should().Be(bookmark);
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.1.0")]
        public void ShouldIgnoreButLogInvalidBookmark()
        {
            var invalidBookmark = "invalid bookmark format";
            var loggerMock = new Mock<IDriverLogger>();
            using(var driver = GraphDatabase.Driver(Server.BoltUri, Server.AuthToken, new Config {DriverLogger = loggerMock.Object}))
            using (var session = (Session)driver.Session())
            {
                session.BeginTransaction(invalidBookmark);
                session.LastBookmark.Should().BeNull(); // ignored
            }
            loggerMock.Verify(x=>x.Info("Failed to recognize bookmark '{0}' and this bookmark is ignored.",
                invalidBookmark), Times.Once); // but logged
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.1.0")]
        public void ShouldThrowForUnreachableBookmark()
        {
            using (var session = (Session)Driver.Session())
            {
                CreateNodeInTx(session, 1);

                // Config the default server bookmark_ready_timeout to be something smaller than 30s to speed up this test
                var exception = Record.Exception(() => session.BeginTransaction(session.LastBookmark + "0"));
                exception.Should().BeOfType<TransientException>();
                exception.Message.Should().Contain("Database not up to the requested version:");
            }
        }


        [RequireServerVersionGreaterThanOrEqualToFact("3.1.0")]
        public void ShouldWaitOnBookmark()
        {
            using (var session = Driver.Session())
            {
                // get a bookmark
                session.LastBookmark.Should().BeNull();
                CreateNodeInTx(session, 1);

                session.LastBookmark.Should().NotBeNull();
                session.LastBookmark.Should().StartWith(BookmarkHeader);
                var lastBookmarkNum = BookmarkNum(session.LastBookmark);

                // start a thread to create lastBookmark + 1 tx 
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(500);
                    using (var anotherSession = Driver.Session())
                    {
                        CreateNodeInTx(anotherSession, 2);
                    }
                });

                // wait for lastBookmark + 1
                var waitForBookmark = $"{BookmarkHeader}{lastBookmarkNum + 1}";
                CountNodeInTx(session, 2, waitForBookmark).Should().Be(1);
            }
        }

        private const string BookmarkHeader = "neo4j:bookmark:v1:tx";

        private long BookmarkNum(string bookmark)
        {
            return Convert.ToInt64(bookmark.Substring(BookmarkHeader.Length));
        }

        private static void CreateNodeInTx(ISession session, int id, string bookmark = null)
        {
            using (var tx = ((Session)session).BeginTransaction(bookmark))
            {
                tx.Run("CREATE (a:Person {id: $id})", new {id});
                tx.Success();
            }
        }

        private static int CountNodeInTx(ISession session, int id, string bookmark = null)
        {
            using (var tx = ((Session)session).BeginTransaction(bookmark))
            {
                var result = tx.Run("MATCH (a:Person {id: $id}) RETURN a", new { id });
                tx.Success();
                return result.Count();
            }
        }
    }
}
