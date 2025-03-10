// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Microsoft.Reactive.Testing;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Tests.Reactive.Utils;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.Internals.VersionComparison;
using static Neo4j.Driver.Tests.Reactive.Utils.Utils;
using static Neo4j.Driver.Tests.TestUtil.Assertions;

namespace Neo4j.Driver.IntegrationTests.Reactive;

public abstract class SummaryIT
{
    public abstract class Specs : AbstractRxIT
    {
        protected Specs(ITestOutputHelper output, StandAloneIntegrationTestFixture standAlone)
            : base(output, standAlone)
        {
        }

        protected abstract IRxRunnable NewRunnable();

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnNonNullSummary()
        {
            NewRunnable()
                .Run("UNWIND RANGE(1,10) AS n RETURN n")
                .Consume()
                .WaitForCompletion()
                .AssertEqual(
                    OnNext<IResultSummary>(0, s => s != null),
                    OnCompleted<IResultSummary>(0));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnSummaryWithQueryText()
        {
            VerifySummaryQueryTextAndParams("UNWIND RANGE(1, 10) AS n RETURN n, true", null);
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnSummaryWithQueryTextAndParams()
        {
            VerifySummaryQueryTextAndParams(
                "UNWIND RANGE(1,$x) AS n RETURN n, $y",
                new { x = 50, y = false });
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnQueryTypeAsWriteOnly()
        {
            VerifySummary("CREATE (n)", null, MatchesSummary(new { QueryType = QueryType.WriteOnly }));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnQueryTypeAsReadOnly()
        {
            VerifySummary(
                "MATCH (n) RETURN n LIMIT 1",
                null,
                MatchesSummary(new { QueryType = QueryType.ReadOnly }));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnQueryTypeAsReadWrite()
        {
            VerifySummary(
                "CREATE (n) RETURN n",
                null,
                MatchesSummary(new { QueryType = QueryType.ReadWrite }));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithCreates()
        {
            VerifySummary(
                "CREATE (n:Label1 {id: $id1})-[:KNOWS]->(m:Label2 {id: $id2}) RETURN n, m",
                new { id1 = 10, id2 = 20 },
                MatchesSummary(new { Counters = new Counters(2, 0, 1, 0, 2, 2, 0, 0, 0, 0, 0, 0, null, null) }));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithDeletes()
        {
            VerifySummary(
                "CREATE (n:Label3 {id: $id1})-[:KNOWS]->(m:Label4 {id: $id2}) RETURN n, m",
                new { id1 = 10, id2 = 20 },
                _ => true);

            VerifySummary(
                "MATCH (n:Label3)-[r:KNOWS]->(m:Label4) DELETE n, r",
                null,
                MatchesSummary(new { Counters = new Counters(0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, null, null) }));
        }

        [RequireServerFact("4.0.0", "5.0.0", Between)]
        public void ShouldReturnUpdateStatisticsWithIndexCreate()
        {
            VerifySummary(
                "CREATE INDEX on :Label(prop)",
                null,
                MatchesSummary(new { Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, null, null) }));
        }

        [RequireServerFact("4.0.0", "5.0.0", Between)]
        public void ShouldReturnUpdateStatisticsWithIndexRemove()
        {
            // Ensure that an index exists
            using (var session = Server.Driver.Session())
            {
                session.Run("CREATE INDEX on :Label(prop)").Consume();
            }

            VerifySummary(
                "DROP INDEX on :Label(prop)",
                null,
                MatchesSummary(new { Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, null, null) }));
        }

        [RequireServerFact("4.0.0", "5.0.0", Between)]
        public void ShouldReturnUpdateStatisticsWithConstraintCreate()
        {
            VerifySummary(
                "CREATE CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE",
                null,
                MatchesSummary(new { Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, null, null) }));
        }

        [RequireServerFact("4.0.0", "5.0.0", Between)]
        public void ShouldReturnUpdateStatisticsWithConstraintRemove()
        {
            // Ensure that a constraint exists
            using (var session = Server.Driver.Session())
            {
                session.Run("CREATE CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE").Consume();
            }

            VerifySummary(
                "DROP CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE",
                null,
                MatchesSummary(new { Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, null, null) }));
        }

        [RequireServerFact("5.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithIndexCreate_5xX()
        {
            VerifySummary(
                "CREATE INDEX label_prop FOR (n:Label) ON (n.prop)",
                null,
                MatchesSummary(new { Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, null, null) }));
        }

        [RequireServerFact("5.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithIndexRemove_5xX()
        {
            // Ensure that an index exists
            using (var session = Server.Driver.Session())
            {
                session.Run("CREATE INDEX label_prop FOR (n:Label) ON (n.prop)").Consume();
            }

            VerifySummary(
                "DROP INDEX label_prop",
                null,
                MatchesSummary(new { Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, null, null) }));
        }

        [RequireServerFact("5.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithConstraintCreate_5xX()
        {
            VerifySummary(
                "CREATE CONSTRAINT book_isbn_uniq FOR (book:Book) REQUIRE book.isbn IS UNIQUE",
                null,
                MatchesSummary(new { Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, null, null) }));
        }

        [RequireServerFact("5.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithConstraintRemove_5xX()
        {
            // Ensure that a constraint exists
            using (var session = Server.Driver.Session())
            {
                session.Run("CREATE CONSTRAINT book_isbn_uniq FOR (book:Book) REQUIRE book.isbn IS UNIQUE").Consume();
            }

            VerifySummary(
                "DROP CONSTRAINT book_isbn_uniq IF EXISTS",
                null,
                MatchesSummary(new { Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, null, null) }));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldNotReturnPlanAndProfile()
        {
            VerifySummary(
                "CREATE (n) RETURN n",
                null,
                MatchesSummary(
                    new
                    {
                        HasPlan = false, Plan = default(IPlan), HasProfile = false, Profile = default(IProfiledPlan)
                    }));
        }
        
        [RequireServerFact("4.0.0", GreaterThanOrEqualTo, Skip = "Broken with servers 5.6+")]
        public void ShouldReturnPlanButNoProfile()
        {
            VerifySummary(
                "EXPLAIN CREATE (n) RETURN n",
                null,
                MatchesSummary(
                    new
                    {
                        HasPlan = true,
                        Plan = new Plan("ProduceResults", null, new[] { "n" }, null),
                        HasProfile = false,
                        Profile = default(IProfiledPlan)
                    },
                    opts => opts.Excluding(x => x.Path == "Plan.OperatorType")
                        .Excluding(x => x.Path == "Plan.Arguments")
                        .Excluding(x => x.Path == "Plan.Children")));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldNotReturnNotifications()
        {
            VerifySummary(
                "CREATE (n) RETURN n",
                null,
                Matches<IResultSummary>(s => s.Notifications.Should().BeNull()));
        }

        [RequireServerFact("4.0.0", "5.6.0", Between)]
        public void ShouldReturnNotifications()
        {
            VerifySummary(
                "EXPLAIN MATCH (n:ThisLabelDoesNotExistReactive) RETURN n",
                null,
                MatchesSummary(
                    new
                    {
                        Notifications = new[]
                        {
                            new Notification(
                                "Neo.ClientNotification.Statement.UnknownLabelWarning",
                                null,
                                null,
                                null,
                                "WARNING",
                                null)
                        }
                    },
                    options => options.ExcludingMissingMembers()
                        .Excluding(x => x.Path == "Notifications[0].Position")
                        .Excluding(x => x.Path == "Notifications[0].Title")
                        .Excluding(x => x.Path == "Notifications[0].Description")));
        }


        [RequireServerFact("5.7.0", GreaterThanOrEqualTo)]
        public void ShouldReturnNotificationsWithCategory()
        {
            VerifySummary(
                "EXPLAIN MATCH (n:ThisLabelDoesNotExistReactive) RETURN n",
                null,
                MatchesSummary(
                    new
                    {
                        Notifications = new []
                        {
                            new Notification(
                                "Neo.ClientNotification.Statement.UnknownLabelWarning",
                                null,
                                null,
                                null,
                                "WARNING",
                                "UNRECOGNIZED")
                        }
                    },
                    options => options.ExcludingMissingMembers()
                        .Excluding(x => x.Path == "Notifications[0].Position")
                        .Excluding(x => x.Path == "Notifications[0].Title")
                        .Excluding(x => x.Path == "Notifications[0].Description")));
        }

        private void VerifySummaryQueryTextAndParams(string query, object parameters)
        {
            VerifySummary(
                query,
                parameters,
                MatchesSummary(new { Query = new Query(query, parameters.ToDictionary()) }));
        }

        private void VerifySummary(string query, object parameters, Func<IResultSummary, bool> predicate)
        {
            NewRunnable()
                .Run(query, parameters)
                .Consume()
                .WaitForCompletion()
                .AssertEqual(
                    OnNext(0, predicate),
                    OnCompleted<IResultSummary>(0));
        }

        public override void Dispose()
        {
            if (!IsDispose)
            {
                CleanUpOnDispose();
            }

            base.Dispose();
        }

        private void CleanUpOnDispose()
        {
            using var session = Server.Driver.Session();

            if (RequireServer.RequiredServerAvailable("5.0.0", GreaterThanOrEqualTo))
            {
                foreach (var drop in session.Run("SHOW CONSTRAINTS").ToList())
                {
                    if (drop.Values.TryGetValue("name", out var name))
                    {
                        session.Run($"DROP CONSTRAINT {name}").Consume();
                    }
                }

                foreach (var drop in session.Run("SHOW INDEXES").ToList())
                {
                    if (drop.Values.TryGetValue("name", out var name))
                    {
                        session.Run($"DROP INDEX {name}").Consume();
                    }
                }
            }
            else
            {
                foreach (var drop in session.Run("CALL db.constraints()").ToList())
                {
                    if (drop.Values.TryGetValue("name", out var name))
                    {
                        session.Run($"DROP CONSTRAINT {name}").Consume();
                    }
                }

                foreach (var drop in session.Run("CALL db.indexes()").ToList())
                {
                    if (drop.Values.TryGetValue("name", out var name))
                    {
                        session.Run($"DROP INDEX {name}").Consume();
                    }
                }
            }
        }
    }

    public class Session : Specs
    {
        private readonly IRxSession _rxSession;

        public Session(ITestOutputHelper output, StandAloneIntegrationTestFixture standAlone)
            : base(output, standAlone)
        {
            _rxSession = NewSession();
        }

        protected override IRxRunnable NewRunnable()
        {
            return _rxSession;
        }

        public override void Dispose()
        {
            if (!IsDispose)
            {
                _rxSession.Close<int>().WaitForCompletion();
            }

            base.Dispose();
        }
    }

    public class Transaction : Specs
    {
        private readonly IRxSession _rxSession;
        private readonly IRxTransaction _rxTransaction;

        public Transaction(ITestOutputHelper output, StandAloneIntegrationTestFixture standAlone)
            : base(output, standAlone)
        {
            _rxSession = NewSession();
            _rxTransaction = _rxSession.BeginTransaction().SingleAsync().Wait();
        }

        protected override IRxRunnable NewRunnable()
        {
            return _rxTransaction;
        }

        public override void Dispose()
        {
            if (!IsDispose)
            {
                _rxTransaction.Commit<int>().WaitForCompletion();
                _rxSession.Close<int>().WaitForCompletion();
            }

            base.Dispose();
        }
    }
}
