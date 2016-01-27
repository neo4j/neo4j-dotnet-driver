//tag::minimal-example-import[]

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Neo4j.Driver;
using Neo4j.Driver.Exceptions;
//end::minimal-example-import[]
using Xunit;
using Xunit.Abstractions;

namespace Examples
{
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
            var driver = GraphDatabase.Driver("bolt://localhost:7687");
            var session = driver.Session();

            session.Run("CREATE (neo:Person {name:'Neo', age:23})");

            var result = session.Run("MATCH (p:Person) WHERE p.name = 'Neo' RETURN p.age");
            while (result.Next())
            {
                output.WriteLine($"Neo is {result.Value("p.age")} years old.");
            }

            session.Dispose();
            driver.Dispose();
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
            var driver = GraphDatabase.Driver("bolt:localhost:7687", Config.DefaultConfig());
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
            //var theOnesCreated = result.Summarize().UpdateStatistics.NodesCreated;
            //output.WriteLine($"There were {theOnesCreated} the ones created.");
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
            //var theOnesCreated = result.Summarize().UpdateStatistics.NodesCreated;
            //output.WriteLine($"There were {theOnesCreated} the ones created.");
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
                    // do more things on the record??
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
                tx.Failure();
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

            output.WriteLine(summary.StatementType);
            output.WriteLine(summary.Profile);
            //end::result-summary-query-profile[]

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

        /*
        # tag::result-summary-query-profile[]
        # tag::result-summary-notifications[]
        # tag::tls-require-encryption[]
        # tag::tls-trust-on-first-use[]
        # tag::tls-signed[]
        */
    }
}