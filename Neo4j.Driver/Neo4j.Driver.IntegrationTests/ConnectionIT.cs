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

                foreach (var record in result)
                {
                    foreach (var keyValuePair in record.Values)
                    {
                        _output.WriteLine($"{keyValuePair.Key} = {keyValuePair.Value}");
                    }
                }
            }
        }

        [Fact]
        public void BuffersNoResultsOfOneQuerySoTheyCanNotBeReadAfterAnotherSubsequentQueryHasBeenParsed()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            using (var session = driver.Session())
            {
                var result1 = session.Run("unwind range(1,3) as n RETURN n");
                var result2 = session.Run("unwind range(4,6) as n RETURN n");
                
                var result2All = result2.ToList();
                var result1All = result1.ToList();

                result2All.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder(4, 5, 6);
                result1All.Select(r => r.Values["n"].ValueAs<int>()).Should().ContainInOrder();
            }
        }

        [Fact]
        public void TheSessionErrorShouldBeClearedForEachSession()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken))
            {
                using (var session = driver.Session())
                {
                    var ex = Record.Exception(() => session.Run("Invalid Cypher"));
                    ex.Should().BeOfType<ClientException>();
                    ex.Message.Should()
                        .Be("Invalid input 'I': expected <init> (line 1, column 1 (offset: 0))\n\"Invalid Cypher\"\n ^");
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
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken, Config.Builder.WithLogger(new DebugLogger { Level = LogLevel.Trace }).ToConfig()))
            {
                using (var session = driver.Session())
                {
                    var ex = Record.Exception(() => session.Run("Invalid Cypher"));
                    ex.Should().BeOfType<ClientException>();
                    ex.Message.Should()
                        .Be("Invalid input 'I': expected <init> (line 1, column 1 (offset: 0))\n\"Invalid Cypher\"\n ^");
                    var result = session.Run("RETURN 1");
                    result.Single()[0].ValueAs<int>().Should().Be(1);
                }
            }
        }

        [Fact]
        public void AfterErrorTheFirstSyncShouldAckFailureSoThatNewStatementCouldRunForTx()
        {
            using (var driver = GraphDatabase.Driver(_serverEndPoint, _authToken, Config.Builder.WithLogger(new DebugLogger { Level = LogLevel.Trace }).ToConfig()))
            {
                using (var session = driver.Session())
                {
                    using (var tx = session.BeginTransaction())
                    {
                        var ex = Record.Exception(() => tx.Run("Invalid Cypher"));
                        ex.Should().BeOfType<ClientException>();
                        ex.Message.Should().Be("Invalid input 'I': expected <init> (line 1, column 1 (offset: 0))\n\"Invalid Cypher\"\n ^");
                    }

                    var result = session.Run("RETURN 1");
                    result.Single()[0].ValueAs<int>().Should().Be(1);
                }
            }
        }
    }
}