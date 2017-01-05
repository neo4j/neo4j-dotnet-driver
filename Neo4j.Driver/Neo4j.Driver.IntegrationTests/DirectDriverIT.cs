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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;
using System.Linq;

namespace Neo4j.Driver.IntegrationTests
{
    public class DirectDriverIT
    {
        public static readonly Config DebugConfig = Config.Builder.WithLogger(new DebugLogger {Level = LogLevel.Debug}).ToConfig();

        [Collection(IntegrationCollection.CollectionName)]
        public class AuthenticationIT
        {
            private readonly ITestOutputHelper _output;

            private readonly string _serverEndPoint;
            private readonly IAuthToken _authToken;

            public AuthenticationIT(ITestOutputHelper output, IntegrationTestFixture fixture)
            {
                _output = output;
                _serverEndPoint = fixture.ServerEndPoint;
                _authToken = fixture.AuthToken;
            }

            [Fact]
            public void ShouldProvideRealmWithBasicAuthToken()
            {
                var oldAuthToken = _authToken.AsDictionary();
                var newAuthToken = AuthTokens.Basic(oldAuthToken["principal"].ValueAs<string>(),
                    oldAuthToken["credentials"].ValueAs<string>(), "native");

                using (var driver = GraphDatabase.Driver(_serverEndPoint, newAuthToken))
                using (var session = driver.Session())
                {
                    var result = session.Run("RETURN 2 as Number");
                    result.Consume();
                    result.Keys.Should().Contain("Number");
                    result.Keys.Count.Should().Be(1);
                }
            }

            [Fact]
            public void ShouldCreateCustomAuthToken()
            {
                var oldAuthToken = _authToken.AsDictionary();
                var newAuthToken = AuthTokens.Custom(
                    oldAuthToken["principal"].ValueAs<string>(),
                    oldAuthToken["credentials"].ValueAs<string>(),
                    "native",
                    "basic");

                using (var driver = GraphDatabase.Driver(_serverEndPoint, newAuthToken))
                using (var session = driver.Session())
                {
                    var result = session.Run("RETURN 2 as Number");
                    result.Consume();
                    result.Keys.Should().Contain("Number");
                    result.Keys.Count.Should().Be(1);
                }
            }

            [Fact]
            public void ShouldCreateCustomAuthTokenWithAdditionalParameters()
            {
                var oldAuthToken = _authToken.AsDictionary();
                var newAuthToken = AuthTokens.Custom(
                    oldAuthToken["principal"].ValueAs<string>(),
                    oldAuthToken["credentials"].ValueAs<string>(),
                    "native",
                    "basic",
                    new Dictionary<string, object> {{"secret", 42}});

                using (var driver = GraphDatabase.Driver(_serverEndPoint, newAuthToken))
                using (var session = driver.Session())
                {
                    var result = session.Run("RETURN 2 as Number");
                    result.Consume();
                    result.Keys.Should().Contain("Number");
                    result.Keys.Count.Should().Be(1);
                }
            }
        }

        [Collection(IntegrationCollection.CollectionName)]
        public class ResultIT
        {
            private readonly ITestOutputHelper _output;
            private readonly IDriver _driver;

            public ResultIT(ITestOutputHelper output, IntegrationTestFixture fixture)
            {
                _output = output;
                _driver = fixture.Driver;
            }

            [Fact]
            public void GetsSummary()
            {
                using (var session = _driver.Session())
                {
                    var result = session.Run("PROFILE CREATE (p:Person { Name: 'Test'})");
                    var stats = result.Consume().Counters;
                    stats.ToString().Should()
                        .Be("Counters{NodesCreated=1, NodesDeleted=0, RelationshipsCreated=0, " +
                        "RelationshipsDeleted=0, PropertiesSet=1, LabelsAdded=1, LabelsRemoved=0, " +
                        "IndexesAdded=0, IndexesRemoved=0, ConstraintsAdded=0, ConstraintsRemoved=0}");
                    var serverInfo = result.Summary.Server;

                    serverInfo.Address.Should().Be("localhost:7687");
                    if (ServerVersion.Version(serverInfo.Version) >= ServerVersion.V3_1_0)
                    {
                        result.Summary.ResultAvailableAfter.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
                        result.Summary.ResultConsumedAfter.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
                    }
                    else
                    {
                        result.Summary.ResultAvailableAfter.Should().BeLessThan(TimeSpan.Zero);
                        result.Summary.ResultConsumedAfter.Should().BeLessThan(TimeSpan.Zero);
                    }
                }
            }

            [Fact]
            public void AccessSummaryAfterFailure()
            {
                using (var session = _driver.Session())
                {
                    var result = session.Run("Invalid");
                    var error = Record.Exception(()=>result.Consume());
                    error.Should().BeOfType<ClientException>();
                    var summary = result.Summary;

                    summary.Should().NotBeNull();
                    summary.Counters.NodesCreated.Should().Be(0);
                    summary.Server.Address.Should().Contain("localhost:7687");
                }
            }

            [Fact]
            public void BufferRecordsAfterSummary()
            {
                using (var session = _driver.Session())
                {
                    var result = session.Run("UNWIND [1,2] AS a RETURN a");
                    var summary = result.Summary;

                    summary.Should().NotBeNull();
                    summary.Counters.NodesCreated.Should().Be(0);
                    summary.Server.Address.Should().Contain("localhost:7687");

                    result.First()["a"].ValueAs<int>().Should().Be(1);
                    result.First()["a"].ValueAs<int>().Should().Be(2);
                }
            }

            [Fact]
            public void DiscardRecordsAfterConsume()
            {
                using (var session = _driver.Session())
                {
                    var result = session.Run("UNWIND [1,2] AS a RETURN a");
                    var summary = result.Consume();

                    summary.Should().NotBeNull();
                    summary.Counters.NodesCreated.Should().Be(0);
                    summary.Server.Address.Should().Contain("localhost:7687");

                    result.ToList().Count.Should().Be(0);
                }
            }

            [Fact]
            public void BuffersResultsOfRunSoTheyCanBeReadAfterAnotherSubsequentRun()
            {
                using (var session = _driver.Session())
                {
                    var result1 = session.Run("unwind range(1,3) as n RETURN n");
                    var result2 = session.Run("unwind range(4,6) as n RETURN n");

                    var result2All = result2.ToList();
                    var result1All = result1.ToList();

                    result2All.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(4, 5, 6);
                    result1All.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(1, 2, 3);
                }
            }

            [Fact]
            public void BufferResultAfterSessionClose()
            {
                IStatementResult result;
                using (var session = _driver.Session())
                {
                    result = session.Run("unwind range(1,3) as n RETURN n");
                }
                var resultAll = result.ToList();

                // Records that has not been read inside session still saved
                resultAll.Count.Should().Be(3);
                resultAll.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(1, 2, 3);

                // Summary is still saved
                result.Summary.Statement.Text.Should().Be("unwind range(1,3) as n RETURN n");
                result.Summary.StatementType.Should().Be(StatementType.ReadOnly);
            }

            [Fact]
            public void BuffersResultsAfterTxCloseSoTheyCanBeReadAfterAnotherSubsequentTx()
            {
                using (var session = _driver.Session())
                {
                    IStatementResult result1, result2;
                    using (var tx = session.BeginTransaction())
                    {
                        result1 = tx.Run("unwind range(1,3) as n RETURN n");
                        tx.Success();
                    }

                    using (var tx = session.BeginTransaction())
                    {
                        result2 = tx.Run("unwind range(4,6) as n RETURN n");
                        tx.Success();
                    }

                    var result2All = result2.ToList();
                    var result1All = result1.ToList();

                    result2All.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(4, 5, 6);
                    result1All.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(1, 2, 3);
                }
            }
        }

        [Collection(IntegrationCollection.CollectionName)]
        public class SessionIT
        {
            private readonly ITestOutputHelper _output;
            private readonly IDriver _driver;
            private readonly string _serverEndPoint;
            private readonly IAuthToken _authToken;

            public SessionIT(ITestOutputHelper output, IntegrationTestFixture fixture)
            {
                _output = output;
                _serverEndPoint = fixture.ServerEndPoint;
                _authToken = fixture.AuthToken;
                _driver = fixture.Driver;
            }

            [Fact]
            public void DisallowNewSessionAfterDriverDispose()
            {
                var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
                var session = driver.Session(AccessMode.Write);
                session.Run("RETURN 1").Single()[0].ValueAs<int>().Should().Be(1);

                driver.Dispose();
                session.Dispose();

                var error = Record.Exception(() => driver.Session());
                error.Should().BeOfType<ObjectDisposedException>();
                error.Message.Should().Contain("Cannot open a new session on a driver that is already disposed.");
            }

            [Fact]
            public void ShouldConnectAndRun()
            {
                using (var session = _driver.Session())
                {
                    var result = session.Run("RETURN 2 as Number");
                    result.Consume();
                    result.Keys.Should().Contain("Number");
                    result.Keys.Count.Should().Be(1);
                }
            }

            [Fact]
            public void ShouldBeAbleToRunMultiStatementsInOneTransaction()
            {
                using (var session = _driver.Session())
                using (var tx = session.BeginTransaction())
                {
                    // clean db
                    tx.Run("MATCH (n) DETACH DELETE n RETURN count(*)");
                    var result = tx.Run("CREATE (n {name:'Steve Brook'}) RETURN n.name");

                    var record = result.Single();
                    record["n.name"].Should().Be("Steve Brook");
                }
            }

            [Fact]
            public void TheSessionErrorShouldBeClearedForEachSession()
            {
                using (var session = _driver.Session())
                {
                    var ex = Record.Exception(() => session.Run("Invalid Cypher").Consume());
                    ex.Should().BeOfType<ClientException>();
                    ex.Message.Should().StartWith("Invalid input 'I'");
                }
                using (var session = _driver.Session())
                {
                    var result = session.Run("RETURN 1");
                    result.Single()[0].ValueAs<int>().Should().Be(1);
                }
            }

            [Fact]
            public void AfterErrorTheFirstSyncShouldAckFailureSoThatNewStatementCouldRun()
            {
                using (var session = _driver.Session())
                {
                    var ex = Record.Exception(() => session.Run("Invalid Cypher").Consume());
                    ex.Should().BeOfType<ClientException>();
                    ex.Message.Should().StartWith("Invalid input 'I'");
                    var result = session.Run("RETURN 1");
                    result.Single()[0].ValueAs<int>().Should().Be(1);
                }
            }

            [Fact]
            public void RollBackTxIfErrorWithConsume()
            {
                // Given
                using (var session = _driver.Session())
                {
                    // When failed to run a tx with consume
                    using (var tx = session.BeginTransaction())
                    {
                        var ex = Record.Exception(() => tx.Run("Invalid Cypher").Consume());
                        ex.Should().BeOfType<ClientException>();
                        ex.Message.Should().StartWith("Invalid input 'I'");
                    }

                    // Then can run more afterwards
                    var result = session.Run("RETURN 1");
                    result.Single()[0].ValueAs<int>().Should().Be(1);
                }
            }

            [Fact]
            public void RollBackTxIfErrorWithoutConsume()
            {
                // Given
                using (var session = _driver.Session())
                {
                    // When failed to run a tx without consume

                    // The following code is the same as using(var tx = session.BeginTx()) {...}
                    // While we have the full control of where the error is thrown
                    var tx = session.BeginTransaction();
                    tx.Run("CREATE (a { name: 'lizhen' })");
                    tx.Run("Invalid Cypher");
                    tx.Success();
                    var ex = Record.Exception(() => tx.Dispose());
                    ex.Should().BeOfType<ClientException>();
                    ex.Message.Should().StartWith("Invalid input 'I'");

                    // Then can still run more afterwards
                    using (var anotherTx = session.BeginTransaction())
                    {
                        var result = anotherTx.Run("MATCH (a {name : 'lizhen'}) RETURN count(a)");
                        result.Single()[0].ValueAs<int>().Should().Be(0);
                    }
                }
            }

            [Fact]
            public void ShouldNotThrowExceptionWhenDisposeSessionAfterDriver()
            {
                var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);

                var session = driver.Session();

                using (var tx = session.BeginTransaction())
                {
                    var ex = Record.Exception(() => tx.Run("Invalid Cypher").Consume());
                    ex.Should().BeOfType<ClientException>();
                    ex.Message.Should().StartWith("Invalid input 'I'");
                }

                var result = session.Run("RETURN 1");
                result.Single()[0].ValueAs<int>().Should().Be(1);

                driver.Dispose();
                session.Dispose();
            }


            [Fact]
            public void ShouldContainLastBookmarkAfterTx()
            {
                string version;
                using (var session = _driver.Session())
                {
                    version = session.Run("RETURN 1").Consume().Server.Version;
                }
                if (ServerVersion.Version(version) >= ServerVersion.V3_1_0)
                {
                    using (var session = _driver.Session())
                    {
                        session.LastBookmark.Should().BeNull();

                        using (var tx = session.BeginTransaction())
                        {
                            tx.Run("CREATE (a:Person)");
                            tx.Success();
                        }

                        session.LastBookmark.Should().NotBeNull();
                        session.LastBookmark.Should().StartWith("neo4j:bookmark:v1:tx");
                    }
                }
            }

            [Fact]
            public void ShouldWaitOnBookmark()
            {
                string version;
                using (var session = _driver.Session())
                {
                    version = session.Run("RETURN 1").Consume().Server.Version;
                }
                if (ServerVersion.Version(version) >= ServerVersion.V3_1_0)
                {
                    using (var session = _driver.Session())
                    {
                        // get a bookmark
                        session.LastBookmark.Should().BeNull();
                        using (var tx = session.BeginTransaction())
                        {
                            tx.Run("CREATE (a:Person)");
                            tx.Success();
                        }

                        session.LastBookmark.Should().NotBeNull();
                        session.LastBookmark.Should().StartWith(BookmarkHeader);
                        var lastBookmarkNum = BookmarkNum(session.LastBookmark);

                        var queue = new ConcurrentQueue<long>();
                        // start a thread to create lastBookmark + 1 tx
                        Task.Factory.StartNew(() =>
                        {
                            Thread.Sleep(100);
                            using (var anotherSession = _driver.Session())
                            {
                                using (var tx = anotherSession.BeginTransaction())
                                {
                                    tx.Run("CREATE (a:Person)");
                                    tx.Success();
                                }
                                queue.Enqueue(BookmarkNum(anotherSession.LastBookmark));
                            }

                        });

                        // wait for lastBookmark + 1 and create lastBookmark + 2
                        var waitForBookmark = $"{BookmarkHeader}{lastBookmarkNum + 1}";
                        using (var tx = session.BeginTransaction(waitForBookmark))
                        {
                            tx.Run("CREATE (a:Person)");
                            tx.Success();
                        }
                        queue.Enqueue(BookmarkNum(session.LastBookmark));

                        queue.Count.Should().Be(2);
                        long value;
                        queue.TryDequeue(out value).Should().BeTrue();
                        value.Should().Be(lastBookmarkNum + 1);
                        queue.TryDequeue(out value).Should().BeTrue();
                        value.Should().Be(lastBookmarkNum + 2);
                    }
                }
            }

            private const string BookmarkHeader = "neo4j:bookmark:v1:tx";

            private long BookmarkNum(string bookmark)
            {
                return Convert.ToInt64(bookmark.Substring(BookmarkHeader.Length));
            }
        }

        [Collection(IntegrationCollection.CollectionName)]
        public class SessionResetIT
        {
            private readonly ITestOutputHelper _output;
            private readonly IDriver _driver;

            public SessionResetIT(ITestOutputHelper output, IntegrationTestFixture fixture)
            {
                _output = output;
                fixture.RestartServerWithProcedures(new DirectoryInfo("../../Resources/longRunningStatement.jar").FullName);
                _driver = fixture.Driver;
            }

            [Fact]
            public async void ShouldKillLongRunningStatement()
            {
                using (var session = _driver.Session())
                {
                    var cancelTokenSource = new CancellationTokenSource();
                    var resetSession = ResetSessionAfterTimeout(session, 5, cancelTokenSource.Token);

                    var result = session.Run("CALL test.driver.longRunningStatement({seconds})",
                        new Dictionary<string, object> {{"seconds", 20}});
                    var exception = Record.Exception(() => result.Consume());

                    // if we finished procedure then we cancel the reset timeout
                    cancelTokenSource.Cancel();
                    await resetSession;

                    var serverInfo = result.Summary.Server;

                    if (ServerVersion.Version(serverInfo.Version) >= ServerVersion.V3_1_0)
                    {
                        exception.Should().BeOfType<TransientException>();
                    }
                    else
                    {
                        exception.Should().BeOfType<ClientException>();
                    }
                    exception.Message.StartsWith("Failed to invoke procedure `test.driver.longRunningStatement`: " +
                                                 "Caused by: org.neo4j.graphdb.TransactionTerminatedException");
                }
            }

            [Fact]
            public async void ShouldKillLongStreamingResult()
            {
                using (var session = _driver.Session())
                {
                    var cancelTokenSource = new CancellationTokenSource();
                    var resetSession = ResetSessionAfterTimeout(session, 5, cancelTokenSource.Token);

                    var result = session.Run("CALL test.driver.longStreamingResult({seconds})",
                        new Dictionary<string, object> {{"seconds", 20L}});

                    var exception = Record.Exception(() => result.Consume());

                    // if we finished procedure then we cancel the reset timeout
                    cancelTokenSource.Cancel();
                    await resetSession;

                    exception.Should().BeOfType<ClientException>();
                    exception.Message.StartsWith(
                        "Failed to call procedure `test.driver.longStreamingResult(seconds :: INTEGER?) :: (record :: STRING?)");
                }
            }

            public async Task ResetSessionAfterTimeout(ISession session, int seconds, CancellationToken cancelToken)
            {
                await Task.Delay(seconds * 1000, cancelToken);
                if (cancelToken.IsCancellationRequested)
                {
                    cancelToken.IsCancellationRequested.Should().Be(false);
                }
                else
                {
                    session.Reset();
                }
            }

            [Fact]
            public void ShouldAllowMoreStatementAfterSessionReset()
            {
                using (var session = _driver.Session())
                {
                    session.Run("RETURN 1").Consume();
                    session.Reset();
                    session.Run("RETURN 2").Consume();
                }
            }

            [Fact]
            public void ShouldAllowMoreTxAfterSessionReset()
            {
                using (var session = _driver.Session())
                {
                    using (var tx = session.BeginTransaction())
                    {
                        tx.Run("Return 1");
                        tx.Success();
                    }
                    session.Reset();
                    using (var tx = session.BeginTransaction())
                    {
                        tx.Run("RETURN 2");
                        tx.Success();
                    }
                }
            }

            [Fact]
            public void ShouldAllowNewTxRunAfterSessionReset()
            {
                using (var session = _driver.Session())
                {
                    using (var tx = session.BeginTransaction())
                    {
                        session.Reset();
                    }
                    using (var tx = session.BeginTransaction())
                    {
                        tx.Run("RETURN 2");
                        tx.Success();
                    }
                }
            }

            [Fact]
            public void ShouldMarkTxAsFailedAndDisallowRunAfterSessionReset()
            {
                using (var session = _driver.Session())
                {
                    using (var tx = session.BeginTransaction())
                    {
                        session.Reset();
                        var exception = Record.Exception(() => tx.Run("Return 1"));
                        exception.Should().BeOfType<ClientException>();
                        exception.Message.Should().StartWith("Cannot run more statements in this transaction");
                    }
                }
            }


            [Fact]
            public void ShouldAllowBeginNewTxAfterResetAndResultConsumed()
            {
                using (var session = _driver.Session())
                {
                    var tx1 = session.BeginTransaction();
                    var result = tx1.Run("Return 1");
                    session.Reset();
                    try
                    {
                        result.Consume();
                    }
                    catch
                    {
                        // ignored
                    }

                    using (var tx = session.BeginTransaction())
                    {
                        tx.Run("RETURN 2");
                        tx.Success();
                    }
                }
            }

            [Fact]
            public async void ShouldThrowExceptionIfErrorAfterResetButNotConsumed()
            {
                using (var session = _driver.Session())
                {
                    session.Run("CALL test.driver.longRunningStatement({seconds})",
                        new Dictionary<string, object> { { "seconds", 20 } });
                    await Task.Delay(5 * 1000);
                    session.Reset();

                    var exception = Record.Exception(() => session.BeginTransaction());

                    exception.Should().BeOfType<ClientException>();
                    exception.Message.Should().StartWith("An error has occurred due to the cancellation of executing a previous statement.");
                }
            }
        }
    }
}