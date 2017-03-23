// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class BookmarkIT : DirectDriverIT
    {
        private IDriver Driver => Server.Driver;
        private bool Skip { get; set; }

        public BookmarkIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
            CheckBookmarkSupport();
        }

        private void CheckBookmarkSupport()
        {
            string version = null;
            using (var session = Driver.Session())
            {
                version = session.Run("RETURN 1").Consume().Server.Version;
            }
            if (ServerVersion.Version(version) >= ServerVersion.V3_1_0)
            {
                Skip = false;
            }
            else
            {
                Skip = true;
                Output.WriteLine("Bookmark tests did not run");
            }
        }

        [RequireServerFact]
        public void ShouldContainLastBookmarkAfterTx()
        {
            if (Skip) return;
            using (var session = Driver.Session())
            {
                session.LastBookmark.Should().BeNull();

                CreateNodeInTx(session);

                session.LastBookmark.Should().NotBeNull();
                session.LastBookmark.Should().StartWith("neo4j:bookmark:v1:tx");
            }
        }

        [RequireServerFact]
        public void BookmarkUnchangedAfterRolledBackTx()
        {
            if (Skip) return;
            using (var session = Driver.Session())
            {
                CreateNodeInTx(session);
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

        [RequireServerFact]
        public void BookmarkUnchangedAfterTxFailure()
        {
            if (Skip) return;
            using (var session = Driver.Session())
            {
                CreateNodeInTx(session);
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

        [RequireServerFact]
        public void ShouldThrowForInvalidBookmark()
        {
            if (Skip) return;
            var invalidBookmark = "invalid bookmark format";
            using (var session = (Session)Driver.Session())
            {
                var exception = Record.Exception(() => session.BeginTransaction(invalidBookmark));
                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().Contain($"does not conform to pattern {BookmarkHeader}");
            }
        }

        [RequireServerFact]
        public void ShouldThrowForUnreachableBookmark()
        {
            if (Skip) return;
            using (var session = (Session)Driver.Session())
            {
                CreateNodeInTx(session);

                // Config the default server bookmark_ready_timeout to be something smaller than 30s to speed up this test
                var exception = Record.Exception(() => session.BeginTransaction(session.LastBookmark + "0"));
                exception.Should().BeOfType<TransientException>();
                exception.Message.Should().Contain("Database not up to the requested version:");
            }
        }


        [RequireServerFact]
        public void ShouldWaitOnBookmark()
        {
            if (Skip) return;
            using (var session = Driver.Session())
            {
                // get a bookmark
                session.LastBookmark.Should().BeNull();
                CreateNodeInTx(session);

                session.LastBookmark.Should().NotBeNull();
                session.LastBookmark.Should().StartWith(BookmarkHeader);
                var lastBookmarkNum = BookmarkNum(session.LastBookmark);

                var queue = new ConcurrentQueue<long>();
                // start a thread to create lastBookmark + 1 tx
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    using (var anotherSession = Driver.Session())
                    {
                        CreateNodeInTx(anotherSession);
                        queue.Enqueue(BookmarkNum(anotherSession.LastBookmark));
                    }
                });

                // wait for lastBookmark + 1 and create lastBookmark + 2
                var waitForBookmark = $"{BookmarkHeader}{lastBookmarkNum + 1}";
                CreateNodeInTx(session, waitForBookmark);

                queue.Enqueue(BookmarkNum(session.LastBookmark));

                queue.Count.Should().Be(2);
                long value;
                queue.TryDequeue(out value).Should().BeTrue();
                value.Should().Be(lastBookmarkNum + 1);
                queue.TryDequeue(out value).Should().BeTrue();
                value.Should().Be(lastBookmarkNum + 2);
            }
        }

        private const string BookmarkHeader = "neo4j:bookmark:v1:tx";

        private long BookmarkNum(string bookmark)
        {
            return Convert.ToInt64(bookmark.Substring(BookmarkHeader.Length));
        }

        private static void CreateNodeInTx(ISession session, string bookmark = null)
        {
            using (var tx = ((Session)session).BeginTransaction(bookmark))
            {
                tx.Run("CREATE (a:Person)");
                tx.Success();
            }
        }
    }
}
