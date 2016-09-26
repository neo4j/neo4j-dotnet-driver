// Copyright (c) 2002-2016 "Neo Technology,"
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

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    using System.Linq;

    [Collection(IntegrationCollection.CollectionName)]
    public class ConnectionIT
    {
        private readonly string _serverEndPoint;
        private readonly IAuthToken _authToken;

        private readonly ITestOutputHelper _output;

        public ConnectionIT(ITestOutputHelper output, IntegrationTestFixture fixture)
        {
            _output = output;
            _serverEndPoint = fixture.ServerEndPoint;
            _authToken = fixture.AuthToken;
            fixture.RestartServerWithProcedures(new DirectoryInfo("../../Resources/longRunningStatement.jar").FullName);
        }

        [Fact]
        public void ShouldDoHandShake()
        {
            using (var driver = GraphDatabase.Driver(
                _serverEndPoint,
                _authToken,
                Config.Builder.WithLogger( new DebugLogger {Level = LogLevel.Trace}).ToConfig()))
            {
                using (var session = driver.Session())
                {
                    var result = session.Run("RETURN 2 as Number" );
                    result.Consume();
                    result.Keys.Should().Contain("Number");
                    result.Keys.Count.Should().Be(1);
                }
            }
        }

        [Fact]
        public void ShouldProvideRealmWithBasicAuthToken()
        {
            var oldAuthToken = _authToken.AsDictionary();
            var newAuthToken = AuthTokens.Basic(oldAuthToken["principal"].ValueAs<string>(), oldAuthToken["credentials"].ValueAs<string>(), "native");

            using (var driver = GraphDatabase.Driver(
                _serverEndPoint,
                newAuthToken))
            {
                using (var session = driver.Session())
                {
                    var result = session.Run("RETURN 2 as Number");
                    result.Consume();
                    result.Keys.Should().Contain("Number");
                    result.Keys.Count.Should().Be(1);
                }
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

            using (var driver = GraphDatabase.Driver(
                _serverEndPoint,
                newAuthToken))
            {
                using (var session = driver.Session())
                {
                    var result = session.Run("RETURN 2 as Number");
                    result.Consume();
                    result.Keys.Should().Contain("Number");
                    result.Keys.Count.Should().Be(1);
                }
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

            using (var driver = GraphDatabase.Driver(
                _serverEndPoint,
                newAuthToken))
            {
                using (var session = driver.Session())
                {
                    var result = session.Run("RETURN 2 as Number");
                    result.Consume();
                    result.Keys.Should().Contain("Number");
                    result.Keys.Count.Should().Be(1);
                }
            }
        }

        [Fact]
        public void GetsSummary()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken, Config.Builder.WithLogger(new DebugLogger { Level = LogLevel.Trace }).ToConfig()))
            using (var session = driver.Session())
            {
                var result = session.Run("PROFILE CREATE (p:Person { Name: 'Test'})");
                var stats = result.Consume().Counters;
                _output.WriteLine(stats.ToString());
            }
        }

        [Fact]
        public void ShouldBeAbleToRunMultiStatementsInOneTransaction()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken, Config.Builder.WithLogger(new DebugLogger {Level = LogLevel.Trace}).ToConfig()))
            using (var session = driver.Session())
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
        public void BuffersResultsOfOneQuerySoTheyCanBeReadAfterAnotherSubsequentQueryHasBeenParsed()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            using (var session = driver.Session())
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
        public void ResultsHaveReceivedButNotBeenReadGetBufferedAfterSessionClosed()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            {
                IStatementResult result;
                using (var session = driver.Session())
                {
                    result = session.Run("unwind range(1,3) as n RETURN n");
                }
                var resultAll = result.ToList();

                // Records that has not been read inside session still saved
                resultAll.Count.Should().Be(3);
                resultAll.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(1,2,3);

                // Summary is still saved
                result.Summary.Statement.Text.Should().Be("unwind range(1,3) as n RETURN n");
                result.Summary.StatementType.Should().Be(StatementType.ReadOnly);
            }
        }

        [Fact]
        public void BuffersResultsOfOneTxSoTheyCanBeReadAfterAnotherSubsequentTx()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            using (var session = driver.Session())
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

        [Fact]
        public void TheSessionErrorShouldBeClearedForEachSession()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            {
                using (var session = driver.Session())
                {
                    var ex = Record.Exception(() => session.Run("Invalid Cypher").Consume());
                    ex.Should().BeOfType<ClientException>();
                    ex.Message.Should().StartWith("Invalid input 'I'");
                }
                using (var session = driver.Session())
                {
                    var result = session.Run("RETURN 1");
                    result.Single()[0].ValueAs<int>().Should().Be(1);
                }
            }
        }

        [Fact]
        public void AfterErrorTheFirstSyncShouldAckFailureSoThatNewStatementCouldRun()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken,
                Config.Builder.WithLogger(new DebugLogger { Level = LogLevel.Trace }).ToConfig()))
            {
                using (var session = driver.Session())
                {
                    var ex = Record.Exception(() => session.Run("Invalid Cypher").Consume());
                    ex.Should().BeOfType<ClientException>();
                    ex.Message.Should().StartWith("Invalid input 'I'");
                    var result = session.Run("RETURN 1");
                    result.Single()[0].ValueAs<int>().Should().Be(1);
                }
            }
        }

        [Fact]
        public void AfterErrorTheFirstSyncShouldAckFailureSoThatNewStatementCouldRunForTx()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken,
                Config.Builder.WithLogger(new DebugLogger { Level = LogLevel.Trace }).ToConfig()))
            {
                using (var session = driver.Session())
                {
                    using (var tx = session.BeginTransaction())
                    {
                        var ex = Record.Exception(() => tx.Run("Invalid Cypher").Consume());
                        ex.Should().BeOfType<ClientException>();
                        ex.Message.Should().StartWith("Invalid input 'I'");
                    }

                    var result = session.Run("RETURN 1");
                    result.Single()[0].ValueAs<int>().Should().Be(1);
                }
            }
        }

        [Fact]
        public void ShouldNotThrowExceptionWhenDisposeSessionAfterDriver()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken,
                Config.Builder.WithLogger(new DebugLogger {Level = LogLevel.Trace}).ToConfig());

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
        public async void ShouldKillLongRunningStatement()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken,
                Config.Builder.WithLogger(new DebugLogger { Level = LogLevel.Debug }).ToConfig()))
            {
                using (var session = driver.Session())
                {
                    var cancelTokenSource = new CancellationTokenSource();
                    var resetSession = ResetSessionAfterTimeout(session, 5, cancelTokenSource.Token);

                    var result = session.Run("CALL test.driver.longRunningStatement({seconds})",
                        new Dictionary<string, object> { { "seconds", 20 } });
                    var exception = Record.Exception(() => result.Consume());

                    // if we finished procedure then we cancel the reset timeout
                    cancelTokenSource.Cancel();
                    await resetSession;

                    exception.Should().BeOfType<ClientException>();
                    exception.Message.StartsWith("Failed to invoke procedure `test.driver.longRunningStatement`: " +
                                                 "Caused by: org.neo4j.graphdb.TransactionTerminatedException");
                }
            }
        }

        [Fact]
        public async void ShouldKillLongStreamingResult()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken,
                Config.Builder.WithLogger(new DebugLogger { Level = LogLevel.Debug }).ToConfig()))
            {
                using (var session = driver.Session())
                {
                    var cancelTokenSource = new CancellationTokenSource();
                    var resetSession = ResetSessionAfterTimeout(session, 5, cancelTokenSource.Token);

                    var result = session.Run("CALL test.driver.longStreamingResult({seconds})",
                        new Dictionary<string, object> { { "seconds", 20L } });

                    var exception = Record.Exception(() => result.Consume());

                    // if we finished procedure then we cancel the reset timeout
                    cancelTokenSource.Cancel();
                    await resetSession;

                    exception.Should().BeOfType<ClientException>();
                    exception.Message.StartsWith("Failed to call procedure `test.driver.longStreamingResult(seconds :: INTEGER?) :: (record :: STRING?)");
                }
            }
        }

        public async Task ResetSessionAfterTimeout(ISession session, int seconds, CancellationToken cancelToken)
        {
            await Task.Delay(seconds*1000, cancelToken);
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
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            {
                using (var session = driver.Session())
                {
                    session.Run("RETURN 1").Consume();
                    session.Reset();
                    session.Run("RETURN 2").Consume();
                }
            }
        }

        [Fact]
        public void ShouldAllowMoreTxAfterSessionReset()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            {
                using (var session = driver.Session())
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
        }

        [Fact]
        public void ShouldMarkTxAsFailedAndDisallowRunAfterSessionReset()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            using (var session = driver.Session())
            {
                using (var tx = session.BeginTransaction())
                {
                    session.Reset();
                    var exception = Record.Exception(()=>tx.Run("Return 1"));
                    exception.Should().BeOfType<ClientException>();
                    exception.Message.Should().StartWith("Cannot run more statements in this transaction");
                }
            }
        }


        [Fact]
        public void ShouldAllowBeginNewTxAfterResetAndResultConsumed()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            using (var session = driver.Session())
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
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            using (var session = driver.Session())
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