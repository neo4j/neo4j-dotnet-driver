// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Reactive;
using Xunit;
using Xunit.Abstractions;
using Neo4j.Driver.Internal;
using static Neo4j.Driver.IntegrationTests.VersionComparison;
using static Neo4j.Driver.Reactive.Utils;
using static Neo4j.Driver.Tests.Assertions;

namespace Neo4j.Driver.IntegrationTests.Reactive
{
    public class SummaryIT
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
                    .Summary()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext<IResultSummary>(0, s => s != null),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnSummaryWithStatementText()
            {
                VerifySummaryStatementTextAndParams("UNWIND RANGE(1, 10) AS n RETURN n, true", null);
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnSummaryWithStatementTextAndParams()
            {
                VerifySummaryStatementTextAndParams("UNWIND RANGE(1,$x) AS n RETURN n, $y",
                    new {x = 50, y = false});
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnStatementTypeAsWriteOnly()
            {
                VerifySummary("CREATE (n)", null, MatchesSummary(new {StatementType = StatementType.WriteOnly}));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnStatementTypeAsReadOnly()
            {
                VerifySummary("MATCH (n) RETURN n LIMIT 1", null,
                    MatchesSummary(new {StatementType = StatementType.ReadOnly}));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnStatementTypeAsReadWrite()
            {
                VerifySummary("CREATE (n) RETURN n", null,
                    MatchesSummary(new {StatementType = StatementType.ReadWrite}));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnUpdateStatisticsWithCreates()
            {
                VerifySummary("CREATE (n:Label1 {id: $id1})-[:KNOWS]->(m:Label2 {id: $id2}) RETURN n, m",
                    new {id1 = 10, id2 = 20},
                    MatchesSummary(new {Counters = new Counters(2, 0, 1, 0, 2, 2, 0, 0, 0, 0, 0)}));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnUpdateStatisticsWithDeletes()
            {
                VerifySummary("CREATE (n:Label3 {id: $id1})-[:KNOWS]->(m:Label4 {id: $id2}) RETURN n, m",
                    new {id1 = 10, id2 = 20}, s => true);
                VerifySummary("MATCH (n:Label3)-[r:KNOWS]->(m:Label4) DELETE n, r", null,
                    MatchesSummary(new {Counters = new Counters(0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0)}));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnUpdateStatisticsWithIndexCreate()
            {
                VerifySummary("CREATE INDEX on :Label(prop)", null,
                    MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0)}));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnUpdateStatisticsWithIndexRemove()
            {
                // Ensure that an index exists
                using (var session = Server.Driver.Session())
                {
                    session.Run("CREATE INDEX on :Label(prop)").Summary();
                }

                VerifySummary("DROP INDEX on :Label(prop)", null,
                    MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0)}));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnUpdateStatisticsWithConstraintCreate()
            {
                VerifySummary("CREATE CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE", null,
                    MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0)}));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnUpdateStatisticsWithConstraintRemove()
            {
                // Ensure that a constraint exists
                using (var session = Server.Driver.Session())
                {
                    session.Run("CREATE CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE").Summary();
                }

                VerifySummary("DROP CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE", null,
                    MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)}));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldNotReturnPlanAndProfile()
            {
                VerifySummary("CREATE (n) RETURN n", null,
                    MatchesSummary(new
                    {
                        HasPlan = false, Plan = default(IPlan), HasProfile = false, Profile = default(IProfiledPlan)
                    }));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnPlanButNoProfile()
            {
                VerifySummary("EXPLAIN CREATE (n) RETURN n", null,
                    MatchesSummary(
                        new
                        {
                            HasPlan = true, Plan = new Plan("ProduceResults", null, new[] {"n"}, null),
                            HasProfile = false, Profile = default(IProfiledPlan)
                        }, opts => opts.Excluding(x => x.SelectedMemberPath == "Plan.Arguments")
                            .Excluding(x => x.SelectedMemberPath == "Plan.Children")));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnPlanAndProfile()
            {
                VerifySummary("PROFILE CREATE (n) RETURN n", null,
                    MatchesSummary(
                        new
                        {
                            HasPlan = true, HasProfile = true,
                            Profile = new ProfiledPlan("ProduceResults", null, new[] {"n"}, null, 0, 1)
                        },
                        opts => opts.Excluding(x => x.SelectedMemberPath == "Profile.Arguments")
                            .Excluding(x => x.SelectedMemberPath == "Profile.Children")));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldNotReturnNotifications()
            {
                VerifySummary("CREATE (n) RETURN n", null,
                    Matches<IResultSummary>(s => s.Notifications.Should().BeEmpty()));
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnNotifications()
            {
                VerifySummary("EXPLAIN MATCH (n:ThisLabelDoesNotExist) RETURN n", null,
                    MatchesSummary(new
                        {
                            Notifications = new[]
                            {
                                new Notification("Neo.ClientNotification.Statement.UnknownLabelWarning",
                                    "The provided label is not in the database.",
                                    "One of the labels in your query is not available in the database, make sure you didn\'t misspell it or that the label is available when you run this statement in your application (the missing label name is: ThisLabelDoesNotExist)",
                                    null, "WARNING")
                            }
                        },
                        options => options.ExcludingMissingMembers()
                            .Excluding(x => x.SelectedMemberPath == "Notifications[0].Position")));
            }

            private void VerifySummaryStatementTextAndParams(string statement, object parameters)
            {
                VerifySummary(statement, parameters,
                    MatchesSummary(new {Statement = new Statement(statement, parameters.ToDictionary())}));
            }

            private void VerifySummary(string statement, object parameters, Func<IResultSummary, bool> predicate)
            {
                NewRunnable()
                    .Run(statement, parameters)
                    .Summary()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, predicate),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            public override void Dispose()
            {
                using (var session = Server.Driver.Session())
                {
                    foreach (var drop in session.Run(
                        "CALL db.constraints()"))
                    {
                        if (drop.Values.TryGetValue("description", out var name) ||
                            drop.Values.TryGetValue("name", out name))
                        {
                            session.Run($"DROP {name}").Summary();
                        }
                    }

                    foreach (var drop in session.Run(
                        "CALL db.indexes()"))
                    {
                        if (drop.Values.TryGetValue("description", out var name) ||
                            drop.Values.TryGetValue("name", out name))
                        {
                            session.Run($"DROP {name}").Summary();
                        }
                    }
                }

                base.Dispose();
            }
        }

        public class Session : Specs
        {
            private readonly IRxSession rxSession;

            public Session(ITestOutputHelper output, StandAloneIntegrationTestFixture standAlone)
                : base(output, standAlone)
            {
                rxSession = NewSession();
            }

            protected override IRxRunnable NewRunnable()
            {
                return rxSession;
            }

            public override void Dispose()
            {
                rxSession.Close<int>().WaitForCompletion();

                base.Dispose();
            }
        }

        public class Transaction : Specs
        {
            private readonly IRxSession rxSession;
            private readonly IRxTransaction rxTransaction;

            public Transaction(ITestOutputHelper output, StandAloneIntegrationTestFixture standAlone)
                : base(output, standAlone)
            {
                rxSession = NewSession();
                rxTransaction = rxSession.BeginTransaction().SingleAsync().Wait();
            }

            protected override IRxRunnable NewRunnable()
            {
                return rxTransaction;
            }

            public override void Dispose()
            {
                rxTransaction.Commit<int>().WaitForCompletion();
                rxSession.Close<int>().WaitForCompletion();

                base.Dispose();
            }
        }
    }
}