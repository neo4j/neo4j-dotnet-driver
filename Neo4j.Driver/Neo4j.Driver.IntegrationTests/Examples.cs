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
using System.Collections.Generic;
using System.Linq;
//tag::minimal-example-import[]
using Neo4j.Driver;
//end::minimal-example-import[]
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.IntegrationTests;
using Xunit;
using Xunit.Abstractions;

namespace Examples
{
    [Collection(IntegrationCollection.CollectionName)]
    public class Examples
    {
        private readonly ITestOutputHelper output;

        public Examples(ITestOutputHelper output)
        {
            this.output = output;
            ClearDatabase();
        }

        [Fact]
        public void MinimalExample()
        {
            //tag::minimal-example[]
            using (var driver = GraphDatabase.Driver("bolt://localhost:7687"))
            using (var session = driver.Session())
            {
                session.Run("CREATE (neo:Person {name:'Neo', age:23})");

                var result = session.Run("MATCH (p:Person) WHERE p.name = 'Neo' RETURN p.age");
                while (result.Next())
                {
                    output.WriteLine($"Neo is {result.Value("p.age")} years old.");
                }
            }
            //end::minimal-example[]
        }

        [Fact]
        public void ConstructDriver()
        {
            //tag::construct-driver[]
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            //end::construct-driver[]
            driver.Dispose();
        }

        [Fact]
        public void Configuration()
        {
            //tag::configuration[]
            var driver = GraphDatabase.Driver("bolt:localhost:7687", Config.DefaultConfig);
            //end::configuration[]
            driver.Dispose();
        }

        [Fact]
        public void Statement()
        {
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();
           
            //tag::statement[]
            var result = session.Run("CREATE (p:Person { name: {name} })",
                new Dictionary<string, object> {{"name", "The One"}});
            var theOnesCreated = result.Summarize().UpdateStatistics.NodesCreated;
            output.WriteLine($"There were {theOnesCreated} the ones created.");
            //end::statement[]
            driver.Dispose();
        }

        [Fact]
        public void StatementWithoutParams()
        {
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();

            //tag::statement-without-parameters[]
            var result = session.Run("CREATE (p:Person { name: 'The One' })");
            var theOnesCreated = result.Summarize().UpdateStatistics.NodesCreated;
            output.WriteLine($"There were {theOnesCreated} the ones created.");
            //end::statement-without-parameters[]
            driver.Dispose();
        }

        [Fact]
        public void ResultCursor()
        {
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();
            session.Run("CREATE (p:Person { name: 'The One', age: 44 })");

            //tag::result-cursor[]
            var result = session.Run("MATCH (p:Person { name: {name} }) RETURN p.age",
                new Dictionary<string, object> { { "name", "The One" } });

            while (result.Next())
            {
                output.WriteLine($"Record: {result.Position()}");
                foreach (var keyValuePair in result.Values())
                {
                    output.WriteLine($"{keyValuePair.Key} = {keyValuePair.Value}");
                }
            }
            //end::result-cursor[]

            driver.Dispose();
        }

        [Fact]
        public void RetainResultQuery()
        {
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();
            session.Run("CREATE (p:Person { name: 'The One', age: 44 })");

            //tag::retain-result-query[]
            var result = session.Run("MATCH (p:Person { name: {name} }) RETURN p.age",
                new Dictionary<string, object> { { "name", "The One" } });

            var records = result.Stream().ToList();

            session.Dispose();

            foreach (var record in records)
            {
                foreach (var keyValuePair in record.Values)
                {
                    output.WriteLine($"{keyValuePair.Key} = {keyValuePair.Value}");
                }
            }
            //end::retain-result-query[]

            driver.Dispose();
        }

        [Fact]
        public void RetainResultProcess()
        {
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();
            session.Run("CREATE (p:Person { name: 'The One', age: 44 })");

            //tag::retain-result-process[]
            var result = session.Run("MATCH (p:Person { name: {name} }) RETURN p.age",
                new Dictionary<string, object> { { "name", "The One" } });

            var records = result.Stream().ToList();

            session.Dispose();

            foreach (var record in records)
            {
                foreach (var keyValuePair in record.Values)
                {
                    output.WriteLine($"{keyValuePair.Key} = {keyValuePair.Value}");
                }
            }
            //end::retain-result-process[]

            driver.Dispose();
        }

        [Fact]
        public void HandleCypherError()
        {
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();

            //tag::handle-cypher-error[]
            try
            {
                session.Run("This will cause a syntax error");
            }
            catch (ClientException ex)
            {
                output.WriteLine(ex.Message);
            }
            //end::handle-cypher-error[]

            driver.Dispose();
        }

        [Fact]
        public void TransactionCommit()
        {
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();

            //tag::transaction-commit[]
            using (ITransaction tx = session.BeginTransaction())
            {
                tx.Run("CREATE (p:Person { name: 'The One', age: 22 })");
                tx.Success();
            }
            //end::transaction-commit[]

            driver.Dispose();
        }

        [Fact]
        public void TransactionRollback()
        {
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();

            //tag::transaction-rollback[]
            using (ITransaction tx = session.BeginTransaction())
            {
                tx.Run("CREATE (p:Person { name: 'The One' })");
                // optional to explicitly call tx.Failure();
            }
            //end::transaction-rollback[]

            driver.Dispose();
        }

        [Fact]
        public void ResultSummaryQueryProfile()
        {
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();

            //tag::result-summary-query-profile[]
            var result = session.Run("PROFILE MATCH (p:Person { name: {name} }) RETURN id(p)",
                            new Dictionary<string, object> { { "name", "The One" } });

            IResultSummary summary = result.Summarize();

            output.WriteLine(summary.StatementType.ToString());
            output.WriteLine(summary.Profile.ToString());
            //end::result-summary-query-profile[]

            driver.Dispose();
        }

        [Fact]
        public void ResultSummaryNotifications()
        {
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();

            //tag::result-summary-notifications[]
            var summary = session.Run("EXPLAIN MATCH (a), (b) RETURN a,b").Summarize();

            foreach (var notification in summary.Notifications)
            {
                output.WriteLine(notification.ToString());
            }
            //end::result-summary-notifications[]

            driver.Dispose();
        }

        [Fact(Skip = "Requires server certificate to be installed on host system.")]
        public void TlsRequireEncryption()
        {
            //tag::tls-require-encryption[]
            // .Net driver by default use tls-signed
            var driver = GraphDatabase.Driver("bolt://localhost:7687", Config.Builder.WithTlsEnabled(true).ToConfig());
            //end::tls-require-encryption[]
            driver.Dispose();
        }

        [Fact(Skip = "Requires server certificate to be installed on host system.")]
        public void TlsSigned()
        {
            //tag::tls-signed[]
            var driver = GraphDatabase.Driver("bolt://localhost:7687", Config.Builder.WithTlsEnabled(true).ToConfig());
            //end::tls-signed[]
            driver.Dispose();
        }

        private void ClearDatabase()
        {
            Driver driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();
            var result = session.Run("MATCH (n) DETACH DELETE n RETURN count(*)");
            while (result.Next())
            {
                // consume
            }
            driver.Dispose();
        }

        //tag::tls-trust-on-first-use[]
        // Not supported in .Net driver
        //end::tls-trust-on-first-use[]

    }
}