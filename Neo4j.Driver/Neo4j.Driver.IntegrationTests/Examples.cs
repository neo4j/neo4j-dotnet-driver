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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
//tag::minimal-example-import[]
using Neo4j.Driver.V1;
//end::minimal-example-import[]
using Neo4j.Driver.IntegrationTests;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Examples
{
    [Collection(IntegrationCollection.CollectionName)]
    public class Examples
    {
        private ITestOutputHelper Output { get; }
        private readonly string _serverEndPoint;
        private readonly IAuthToken _authToken;

        public Examples(ITestOutputHelper output, IntegrationTestFixture fixture)
        {
            Output = output;
            _serverEndPoint = fixture.ServerEndPoint;
            _authToken = fixture.AuthToken;
            ClearDatabase();
        }

        [Fact]
        public void MinimalExample()
        {
            //tag::minimal-example[]
            using (var driver = GraphDatabase.Driver("bolt://myserver:7687", AuthTokens.Basic("neo4j", "neo4j")))
            using (var session = driver.Session())
            {
                session.Run("CREATE (a:Person {name:'Arthur', title:'King'})");
                var result = session.Run("MATCH (a:Person) WHERE a.name = 'Arthur' RETURN a.name AS name, a.title AS title");

                foreach (var record in result)
                {
                    Output.WriteLine($"{record["title"].As<string>()} {record["name"].As<string>()}");
                }
            }
            //end::minimal-example[]
        }

        [Fact]
        public void ConstructDriver()
        {
            //tag::construct-driver[]
            var driver = GraphDatabase.Driver("bolt://myserver:7687", AuthTokens.Basic("neo4j", "neo4j"));
            //end::construct-driver[]
            driver.Dispose();
        }

        [Fact]
        public void Configuration()
        {
            //tag::configuration[]
            var driver = GraphDatabase.Driver("bolt://myserver:7687", AuthTokens.Basic("neo4j", "neo4j"),
                Config.Builder.WithMaxIdleSessionPoolSize(10).ToConfig());
            //end::configuration[]
            driver.Dispose();
        }

        [Fact]
        public void Statement()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();

            //tag::statement[]
            var result = session.Run("CREATE (person:Person {name: {name}})",
                new Dictionary<string, object> {{"name", "Arthur"}});
            //end::statement[]

            result.Consume();
            driver.Dispose();
        }

        [Fact]
        public void StatementWithoutParams()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();

            //tag::statement-without-parameters[]
            var result = session.Run("CREATE (p:Person { name: 'Arthur' })");
            //end::statement-without-parameters[]
            result.Consume();
            driver.Dispose();
        }

        [Fact]
        public void ResultTraversal()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();
            session.Run("CREATE (weapon:Weapon { name: 'Sword in the stone' })");

            //tag::result-traversal[]
            var searchTerm = "Sword";
            var result = session.Run("MATCH (weapon:Weapon) WHERE weapon.name CONTAINS {term} RETURN weapon.name",
                new Dictionary<string, object> { { "term", searchTerm } });

            Output.WriteLine($"List of weapons called {searchTerm}:");
            foreach (var record in result)
            {
                Output.WriteLine(record["weapon.name"].As<string>());
            }
            //end::result-traversal[]

            driver.Dispose();
        }

        [Fact]
        public void AccessRecord()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();
            session.Run("CREATE (weapon:Weapon { name: 'Sword in the stone', owner: 'Arthur', material: 'Stone', size: 'Huge' })");
            session.Run("CREATE (weapon:Weapon { name: 'Excalibur', owner: 'Arthur', material: 'Iron', size: 'Enormous' })");

            //tag::access-record[]
            var searchTerm = "Arthur";
            var result = session.Run("MATCH (weapon:Weapon) WHERE weapon.owner CONTAINS {term} RETURN weapon.name, weapon.material, weapon.size",
                new Dictionary<string, object> { { "term", searchTerm } });

            Output.WriteLine($"List of weapons owned by {searchTerm}:");
            foreach (var record in result)
            {
                var list = record.Keys.Select(key => $"{key}: {record[key]}").ToList();
                Output.WriteLine(string.Join(", ", list));
            }
            //end::access-record[]

            driver.Dispose();
        }

        [Fact]
        public void RetainResultQuery()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();
            session.Run("CREATE (knight:Person:Knight { name: 'Lancelot', castle: 'Camelot' })");

            //tag::retain-result[]
            var result = session.Run("MATCH (knight:Person:Knight) WHERE knight.castle = {castle} RETURN knight.name AS name",
                new Dictionary<string, object> { { "castle", "Camelot" } });

            var records = result.ToList();
            session.Dispose();

            foreach (var record in records)
            {
                Output.WriteLine($"{record["name"].As<string>()} is a knight of Camelot");
            }
            //end::retain-result[]

            driver.Dispose();
        }

        [Fact]
        public void NestedStatements()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();
            session.Run("CREATE (knight:Person:Knight { name: 'Lancelot', castle: 'Camelot' })");
            session.Run("CREATE (knight:Person { name: 'Arthur', title: 'King' })");

            //tag::nested-statements[]
            var result = session.Run("MATCH (knight:Person:Knight) WHERE knight.castle = {castle} RETURN id(knight) AS knight_id",
                new Dictionary<string, object> { { "castle", "Camelot" } });

            foreach (var record in result)
            {
                session.Run("MATCH (knight) WHERE id(knight) = {id} " +
                            "MATCH (king:Person) WHERE king.name = {king} " +
                            "CREATE (knight)-[:DEFENDS]->(king)",
                    new Dictionary<string, object> {{"id", record["knight_id"]}, {"king", "Arthur"}});
            }
            //end::nested-statements[]
            driver.Dispose();
        }

        [Fact]
        public void HandleCypherError()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();
            var ex = Record.Exception(() =>
            {
                //tag::handle-cypher-error[]
                try
                {
                    session.Run("This will cause a syntax error").Consume();
                }
                catch (ClientException)
                {
                    throw new InvalidOperationException("Something really bad has happened!");
                }
                //end::handle-cypher-error[]
            });

            driver.Dispose();
            ex.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void TransactionCommit()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();

            //tag::transaction-commit[]
            using (var tx = session.BeginTransaction())
            {
                tx.Run("CREATE (:Person {name: 'Guinevere'})");
                tx.Success();
            }
            //end::transaction-commit[]

            driver.Dispose();
        }

        [Fact]
        public void TransactionRollback()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();

            //tag::transaction-rollback[]
            using (var tx = session.BeginTransaction())
            {
                tx.Run("CREATE (:Person {name: 'Merlin'})");
                // optional to explicitly call tx.Failure();
            }
            //end::transaction-rollback[]

            driver.Dispose();
        }

        [Fact]
        public void ResultSummaryQueryProfile()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();

            //tag::result-summary-query-profile[]
            var result = session.Run("PROFILE MATCH (p:Person {name: {name}}) RETURN id(p)",
                            new Dictionary<string, object> { { "name", "Arthur" } });

            IResultSummary summary = result.Consume();

            Output.WriteLine(summary.StatementType.ToString());
            Output.WriteLine(summary.Profile.ToString());
            //end::result-summary-query-profile[]

            driver.Dispose();
        }

        [Fact]
        public void ResultSummaryNotifications()
        {
            var driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();

            //tag::result-summary-notifications[]
            var summary = session.Run("EXPLAIN MATCH (king), (queen) RETURN king, queen").Consume();

            foreach (var notification in summary.Notifications)
            {
                Output.WriteLine(notification.ToString());
            }
            //end::result-summary-notifications[]

            driver.Dispose();
        }

        [Fact(Skip = "Requires server certificate to be installed on host system.")]
        public void TlsRequireEncryption()
        {
            //tag::tls-require-encryption[]
            var driver = GraphDatabase.Driver("bolt://myserver:7687", AuthTokens.Basic("neo4j", "neo4j"),
                Config.Builder.WithEncryptionLevel(EncryptionLevel.Encrypted).ToConfig());
            //end::tls-require-encryption[]
            driver.Dispose();
        }

        [Fact(Skip = "Requires server certificate to be installed on host system.")]
        public void TlsSigned()
        {
            //tag::tls-signed[]
            var driver = GraphDatabase.Driver("bolt://myserver:7687", AuthTokens.Basic("neo4j", "neo4j"),
                Config.Builder.WithEncryptionLevel(EncryptionLevel.Encrypted).ToConfig());
            //end::tls-signed[]
            driver.Dispose();
        }

        [Fact(Skip = "Requires server certificate to be installed on host system.")]
        public void ConnectWithAuthDisabled()
        {
            //tag::connect-with-auth-disabled[]
            var driver = GraphDatabase.Driver("bolt://myserver:7687",
                Config.Builder.WithEncryptionLevel(EncryptionLevel.Encrypted).ToConfig());
            //end::connect-with-auth-disabled[]
            driver.Dispose();
        }

        private void ClearDatabase()
        {
            IDriver driver = GraphDatabase.Driver(_serverEndPoint, _authToken);
            var session = driver.Session();
            var result = session.Run("MATCH (n) DETACH DELETE n RETURN count(*)");
            result.ToList();
            driver.Dispose();
        }

        //tag::tls-trust-on-first-use[]
        // Not supported in .Net driver
        //end::tls-trust-on-first-use[]

    }

    // TODO Remove it after we figure out a way to solve the naming problem
    internal static class ValueExtensions
    {
        public static T As<T>(this object value)
        {
            return V1.ValueExtensions.As<T>(value);
        }
    }
}
