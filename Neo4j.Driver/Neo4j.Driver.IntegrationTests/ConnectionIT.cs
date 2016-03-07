//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Internal;
using Xunit;
using Xunit.Abstractions;
using Path = System.IO.Path;

namespace Neo4j.Driver.IntegrationTests
{
    using System.Linq;

    [Collection(IntegrationCollection.CollectionName)]
    public class ConnectionIT
    {
        private int Port { get; set; }
        private string ServerEndPoint => $"bolt://localhost:{Port}";

        private readonly IntegrationTestFixture fixture;
        private readonly ITestOutputHelper output;

        public ConnectionIT(ITestOutputHelper output, IntegrationTestFixture fixture)
        {
            this.fixture = fixture;
            this.output = output;
            Port = fixture.Port;
        }

        [Fact]
        public void ShouldDoHandShake()
        {
            using (var driver = GraphDatabase.Driver(
                ServerEndPoint, 
                Config.Builder.WithLogger( new DebugLogger {Level = LogLevel.Trace}).ToConfig()))
            {
                using (var session = driver.Session())
                {
                    var resultCursor = session.Run("RETURN 2 as Number" );
                    resultCursor.Keys.Should().Contain("Number");
                    resultCursor.Keys.Count.Should().Be(1);
                }
            }
        }

        [Fact]
        public void ShouldEstablishConnectionWhenAuthEnabled()
        {
            var authFilePath = Path.Combine(fixture.Neo4jHome, "data/dbms/auth");
            if (File.Exists(authFilePath))
            {
                File.Delete(authFilePath);
            }
            fixture.RestartServerWithUpdatedSettings(new Dictionary<string, string>
            {
                {"dbms.security.auth_enabled", "true"}
            });
            try
            {
                using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthTokens.Basic("neo4j", "neo4j")))
                {
                    using (var session = driver.Session())
                    {
                        var exception = Record.Exception(() => session.Run("RETURN 2 as Number"));
                        exception.Should().BeOfType<ClientException>();
                        exception.Message.Should().StartWith("The credentials have expired and need to be updated.");
                    }
                }
                // update auth and run something
                using (var driver = GraphDatabase.Driver(
                    ServerEndPoint,
                    new AuthToken(new Dictionary<string, object>
                    {
                        {"scheme", "basic"},
                        {"principal", "neo4j"},
                        {"credentials", "neo4j"},
                        {"new_credentials", "lala"}
                    })))
                using (var session = driver.Session())
                {
                    var resultCursor = session.Run("RETURN 2 as Number");
                    resultCursor.Keys.Should().Contain("Number");
                    resultCursor.Keys.Count.Should().Be(1);
                }
            }
            finally
            {
                File.Delete(authFilePath);
                fixture.RestartServerWithUpdatedSettings(new Dictionary<string, string>
                {
                    {"dbms.security.auth_enabled", "false"}
                });
            }
        }

        [Fact]
        public void GetsSummary()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, Config.Builder.WithLogger(new DebugLogger { Level = LogLevel.Trace }).ToConfig()))
            using (var session = driver.Session())
            {
                var cursor = session.Run("PROFILE CREATE (p:Person { Name: 'Test'})");
                var stats = cursor.Summary.Counters;
                output.WriteLine(stats.ToString());
            }
        }

        [Fact]
        public void ShouldBeAbleToRunMultiStatementsInOneTransaction()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost:7687", Config.Builder.WithLogger(new DebugLogger {Level = LogLevel.Trace}).ToConfig()))
            using (var session = driver.Session())
            using (var tx = session.BeginTransaction())
            {
                // clean db
                tx.Run("MATCH (n) DETACH DELETE n RETURN count(*)");
                var result = tx.Run("CREATE (n {name:'Steve Brook'}) RETURN n.name");

                while (result.Next())
                {
                    foreach (var keyValuePair in result.Values())
                    {
                        output.WriteLine($"{keyValuePair.Key} = {keyValuePair.Value}");
                    }
                }
            }
        }

        [Fact]
        public void BuffersResultsOfOneQuerySoTheyCanBeReadAfterAnotherSubsequentQueryHasBeenParsed()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost"))
            using (var session = driver.Session())
            {
                var cursor1 = session.Run("unwind range(1,3) as n RETURN n");
                var cursor2 = session.Run("unwind range(4,6) as n RETURN n");

                var result2All = cursor2.Stream().ToList();
                var result1All = cursor1.Stream().ToList();

                result2All.Select(r => (int)r.Values["n"]).Should().ContainInOrder(4, 5, 6);
                result1All.Select(r => (int)r.Values["n"]).Should().ContainInOrder(1, 2, 3);
            }
        }
    }
}