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
    public class Examples : IDisposable
    {
        private ITestOutputHelper Output { get; }
        private IDriver Driver { get; }

        public Examples(ITestOutputHelper output, IntegrationTestFixture fixture)
        {
            Output = output;
            Driver = fixture.StandAlone.Driver;
        }

        public void Dispose()
        {
            using (var session = Driver.Session())
            {
                session.Run("MATCH (n) DETACH DELETE n").Consume();
            }
        }

        [Fact]
        public void MinimalExample()
        {
            //tag::minimal-example[]
            using (var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "neo4j")))
            using (var session = driver.Session())
            {
                session.Run("CREATE (a:Person {name: {name}, title: {title}})",
                            new Dictionary<string, object> { {"name", "Arthur"}, {"title", "King"} });

                var result = session.Run("MATCH (a:Person) WHERE a.name = {name} " +
                                         "RETURN a.name AS name, a.title AS title",
                                         new Dictionary<string, object> { {"name", "Arthur"} });

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
            var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "neo4j"));
            //end::construct-driver[]
            driver.Dispose();
        }

        [Fact]
        public void Configuration()
        {
            //tag::configuration[]
            var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "neo4j"),
                Config.Builder.WithMaxIdleSessionPoolSize(10).ToConfig());
            //end::configuration[]
            driver.Dispose();
        }

        [Fact]
        public void Statement()
        {
            using (var session = Driver.Session())
            {
                //tag::statement[]
                var result = session.Run("CREATE (person:Person {name: {name}})",
                    new Dictionary<string, object> {{"name", "Arthur"}});
                //end::statement[]
                result.Consume();
            }
        }

        [Fact]
        public void StatementWithoutParams()
        {
            using (var session = Driver.Session())
            {
                //tag::statement-without-parameters[]
                var result = session.Run("CREATE (p:Person {name: 'Arthur'})");
                //end::statement-without-parameters[]
                result.Consume();
            }
        }

        [Fact]
        public void ResultTraversal()
        {
            using (var session = Driver.Session())
            {
                session.Run("CREATE (weapon:Weapon {name: {name}})",
                    new Dictionary<string, object> {{"name", "Sword in the stone"}});

                //tag::result-traversal[]
                var searchTerm = "Sword";
                var result = session.Run("MATCH (weapon:Weapon) WHERE weapon.name CONTAINS {term} " +
                                         "RETURN weapon.name",
                    new Dictionary<string, object> {{"term", searchTerm}});

                Output.WriteLine($"List of weapons called {searchTerm}:");
                foreach (var record in result)
                {
                    Output.WriteLine(record["weapon.name"].As<string>());
                }
                //end::result-traversal[]
            }
        }

        [Fact]
        public void AccessRecord()
        {
            using (var session = Driver.Session())
            {
                session.Run("CREATE (weapon:Weapon {name: {name}, owner: {owner}, material: {material}, size: {size}})",
                    new Dictionary<string, object>
                    {
                        {"name", "Sword in the stone"},
                        {"owner", "Arthur"},
                        {"material", "Stone"},
                        {"size", "Huge"}
                    });

                session.Run("CREATE (weapon:Weapon {name: {name}, owner: {owner}, material: {material}, size: {size}})",
                    new Dictionary<string, object>
                    {
                        {"name", "Excalibur"},
                        {"owner", "Arthur"},
                        {"material", "Iron"},
                        {"size", "Enormous"}
                    });

                //tag::access-record[]
                var searchTerm = "Arthur";
                var result = session.Run("MATCH (weapon:Weapon) WHERE weapon.owner CONTAINS {term} " +
                                         "RETURN weapon.name, weapon.material, weapon.size",
                    new Dictionary<string, object> {{"term", searchTerm}});

                Output.WriteLine($"List of weapons owned by {searchTerm}:");
                foreach (var record in result)
                {
                    var list = record.Keys.Select(key => $"{key}: {record[key]}").ToList();
                    Output.WriteLine(string.Join(", ", list));
                }
                //end::access-record[]
            }
        }

        [Fact]
        public void RetainResultQuery()
        {
            using (var session = Driver.Session())
            {
                session.Run("CREATE (knight:Person:Knight {name: {name}, castle: {castle}})",
                    new Dictionary<string, object> {{"name", "Lancelot"}, {"castle", "Camelot"}});
            }

            using (var session = Driver.Session())
            {

                //tag::retain-result[]
                var result = session.Run("MATCH (knight:Person:Knight) WHERE knight.castle = {castle} " +
                                         "RETURN knight.name AS name",
                    new Dictionary<string, object> {{"castle", "Camelot"}});

                var records = result.ToList();

                foreach (var record in records)
                {
                    Output.WriteLine($"{record["name"].As<string>()} is a knight of Camelot");
                }
                //end::retain-result[]
            }
        }

        [Fact]
        public void NestedStatements()
        {
            using (var session = Driver.Session())
            {
                session.Run("CREATE (knight:Person:Knight {name: {name}, castle: {castle}})",
                    new Dictionary<string, object> {{"name", "Lancelot"}, {"castle", "Camelot"}});

                session.Run("CREATE (knight:Person {name: {name}, title: {title}})",
                    new Dictionary<string, object> {{"name", "Arthur"}, {"title", "King"}});

                //tag::nested-statements[]
                var result = session.Run("MATCH (knight:Person:Knight) WHERE knight.castle = {castle} " +
                                         "RETURN id(knight) AS knight_id",
                    new Dictionary<string, object> {{"castle", "Camelot"}});

                foreach (var record in result)
                {
                    session.Run("MATCH (knight) WHERE id(knight) = {id} " +
                                "MATCH (king:Person) WHERE king.name = {king} " +
                                "CREATE (knight)-[:DEFENDS]->(king)",
                        new Dictionary<string, object> {{"id", record["knight_id"]}, {"king", "Arthur"}});
                }
                //end::nested-statements[]
            }
        }

        [Fact]
        public void HandleCypherError()
        {
            using (var session = Driver.Session())
            {
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
                ex.Should().BeOfType<InvalidOperationException>();
            }
        }

        [Fact]
        public void TransactionCommit()
        {
            using (var session = Driver.Session())
            {
                //tag::transaction-commit[]
                using (var tx = session.BeginTransaction())
                {
                    tx.Run("CREATE (:Person {name: {name}})",
                        new Dictionary<string, object> {{"name", "Guinevere"}});
                    tx.Success();
                }
                //end::transaction-commit[]
            }
        }

        [Fact]
        public void TransactionRollback()
        {
            using (var session = Driver.Session())
            {
                //tag::transaction-rollback[]
                using (var tx = session.BeginTransaction())
                {
                    tx.Run("CREATE (:Person {name: {name}})",
                        new Dictionary<string, object> {{"name", "Merlin"}});
                    tx.Failure(); // This step is optional. If not called,  tx.Failure() is implicit.
                }
                //end::transaction-rollback[]
            }
        }

        [Fact]
        public void ResultSummaryQueryProfile()
        {
            using (var session = Driver.Session())
            {
                //tag::result-summary-query-profile[]
                var result = session.Run("PROFILE MATCH (p:Person {name: {name}}) RETURN id(p)",
                    new Dictionary<string, object> {{"name", "Arthur"}});

                IResultSummary summary = result.Consume();

                Output.WriteLine(summary.StatementType.ToString());
                Output.WriteLine(summary.Profile.ToString());
                //end::result-summary-query-profile[]
            }
        }

        [Fact]
        public void ResultSummaryNotifications()
        {
            using (var session = Driver.Session())
            {
                //tag::result-summary-notifications[]
                var summary = session.Run("EXPLAIN MATCH (king), (queen) RETURN king, queen").Consume();

                foreach (var notification in summary.Notifications)
                {
                    Output.WriteLine(notification.ToString());
                }
                //end::result-summary-notifications[]
            }
        }

        [Fact(Skip = "Requires server certificate to be installed on host system.")]
        public void TlsRequireEncryption()
        {
            //tag::tls-require-encryption[]
            var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "neo4j"),
                Config.Builder.WithEncryptionLevel(EncryptionLevel.Encrypted).ToConfig());
            //end::tls-require-encryption[]
            driver.Dispose();
        }

        [Fact(Skip = "Requires server certificate to be installed on host system.")]
        public void TlsSigned()
        {
            //tag::tls-signed[]
            var driver = GraphDatabase.Driver("bolt://localhost", AuthTokens.Basic("neo4j", "neo4j"),
                Config.Builder.WithEncryptionLevel(EncryptionLevel.Encrypted)
                    .WithTrustStrategy(TrustStrategy.TrustSystemCaSignedCertificates).ToConfig());
            //end::tls-signed[]
            driver.Dispose();
        }

        [Fact(Skip = "Requires server certificate to be installed on host system.")]
        public void ConnectWithAuthDisabled()
        {
            //tag::connect-with-auth-disabled[]
            var driver = GraphDatabase.Driver("bolt://localhost:7687",
                Config.Builder.WithEncryptionLevel(EncryptionLevel.Encrypted).ToConfig());
            //end::connect-with-auth-disabled[]
            driver.Dispose();
        }

        //tag::tls-trust-on-first-use[]
        // Not supported in this driver
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
