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

            [RequireServerFact]
            public void ShouldReturnNonNullSummary()
            {
                NewRunnable()
                    .Run("UNWIND RANGE(1,10) AS n RETURN n")
                    .Summary()
                    .SubscribeAndWait(CreateObserver<IResultSummary>())
                    .AssertEqual(
                        OnNext<IResultSummary>(0, s => s != null),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnSummaryWithStatementText()
            {
                VerifySummaryStatementTextAndParams("UNWIND RANGE(1, 10) AS n RETURN n, true", null);
            }

            [RequireServerFact]
            public void ShouldReturnSummaryWithStatementTextAndParams()
            {
                VerifySummaryStatementTextAndParams("UNWIND RANGE(1,$x) AS n RETURN n, $y",
                    new {x = 50, y = false});
            }

            [RequireServerTheory]
            [InlineData("CREATE (n)", StatementType.WriteOnly)]
            [InlineData("MATCH (n) RETURN n LIMIT 1", StatementType.ReadOnly)]
            [InlineData("CREATE (n) RETURN n", StatementType.ReadWrite)]
            public void ShouldReturnStatementType(string statement, StatementType expectedType)
            {
                VerifySummary(statement, null, MatchesSummary(new {StatementType = expectedType}));
            }

            [RequireServerFact]
            public void ShouldReturnUpdateStatisticsWithCreates()
            {
                VerifySummary("CREATE (n:Label1 {id: $id1})-[:KNOWS]->(m:Label2 {id: $id2}) RETURN n, m",
                    new {id1 = 10, id2 = 20},
                    MatchesSummary(new {Counters = new Counters(2, 0, 1, 0, 2, 2, 0, 0, 0, 0, 0)}));
            }

            [RequireServerFact]
            public void ShouldReturnUpdateStatisticsWithDeletes()
            {
                VerifySummary("CREATE (n:Label3 {id: $id1})-[:KNOWS]->(m:Label4 {id: $id2}) RETURN n, m",
                    new {id1 = 10, id2 = 20}, s => true);
                VerifySummary("MATCH (n:Label3)-[r:KNOWS]->(m:Label4) DELETE n, r", null,
                    MatchesSummary(new {Counters = new Counters(0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0)}));
            }

            [RequireServerFact]
            public void ShouldReturnUpdateStatisticsWithIndexCreate()
            {
                VerifySummary("CREATE INDEX on :Label(prop)", null,
                    MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0)}));
            }

            [RequireServerFact]
            public void ShouldReturnUpdateStatisticsWithIndexRemove()
            {
                // Ensure that an index exists
                using (var session = Server.Driver.Session())
                {
                    session.Run("CREATE INDEX on :Label(prop)").Consume();
                }

                VerifySummary("DROP INDEX on :Label(prop)", null,
                    MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0)}));
            }

            [RequireServerFact]
            public void ShouldReturnUpdateStatisticsWithConstraintCreate()
            {
                VerifySummary("CREATE CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE", null,
                    MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0)}));
            }

            [RequireServerFact]
            public void ShouldReturnUpdateStatisticsWithConstraintRemove()
            {
                // Ensure that a constraint exists
                using (var session = Server.Driver.Session())
                {
                    session.Run("CREATE CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE").Consume();
                }

                VerifySummary("DROP CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE", null,
                    MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)}));
            }

            [RequireServerFact]
            public void ShouldNotReturnPlanAndProfile()
            {
                VerifySummary("CREATE (n) RETURN n", null,
                    MatchesSummary(new
                    {
                        HasPlan = false, Plan = default(IPlan), HasProfile = false, Profile = default(IProfiledPlan)
                    }));
            }

            [RequireServerFact]
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

            [RequireServerFact]
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

            [RequireServerFact]
            public void ShouldNotReturnNotifications()
            {
                VerifySummary("CREATE (n) RETURN n", null,
                    Matches<IResultSummary>(s => s.Notifications.Should().BeEmpty()));
            }

            [RequireServerFact(Skip = "Seems to be flaky")]
            public void ShouldReturnNotifications()
            {
                VerifySummary("EXPLAIN MATCH (n),(m) RETURN n,m", null,
                    MatchesSummary(new
                        {
                            Notifications = new[]
                            {
                                new Notification("Neo.ClientNotification.Statement.CartesianProductWarning",
                                    "This query builds a cartesian product between disconnected patterns.",
                                    "If a part of a query contains multiple disconnected patterns, this will build a cartesian product between all those parts. This may produce a large amount of data and slow down query processing. While occasionally intended, it may often be possible to reformulate the query that avoids the use of this cross product, perhaps by adding a relationship between the different parts or by using OPTIONAL MATCH (identifier is: (m))",
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
                    .SubscribeAndWait(CreateObserver<IResultSummary>())
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
                        "CALL db.constraints() yield description RETURN 'DROP ' + description"))
                    {
                        session.Run(drop[0].As<string>()).Consume();
                    }

                    foreach (var drop in session.Run(
                        "CALL db.indexes() yield description RETURN 'DROP ' + description"))
                    {
                        session.Run(drop[0].As<string>()).Consume();
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
                rxSession.Close<int>().SubscribeAndDiscard();

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
                rxTransaction.Commit<int>().SubscribeAndDiscard();
                rxSession.Close<int>().SubscribeAndDiscard();

                base.Dispose();
            }
        }
    }
}