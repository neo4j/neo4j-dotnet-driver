// Copyright (c) "Neo4j"
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
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Reactive;
using Xunit.Abstractions;
using Neo4j.Driver.Internal;
using static Neo4j.Driver.IntegrationTests.VersionComparison;
using static Neo4j.Driver.Reactive.Utils;
using static Neo4j.Driver.Tests.Assertions;
using Notification = Neo4j.Driver.Internal.Result.Notification;

namespace Neo4j.Driver.IntegrationTests.Reactive;

public class SummaryIT
{
    public abstract class Specs : AbstractRxIT
    {
        private bool _disposed = false;

        ~Specs() => Dispose(false);

        protected Specs(ITestOutputHelper output, SingleServerFixture standAlone)
            : base(output, standAlone)
        {
        }

        protected abstract IRxRunnable NewRunnable();

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnNonNullSummary()
        {
            var summary = NewRunnable()
                .Run("UNWIND RANGE(1,10) AS n RETURN n")
                .Consume()
                .FirstOrDefaultAsync()
                .Wait();
            summary.Should().NotBeNull();
                // .WaitForCompletion()
                // .AssertEqual(
                //     OnNext<IResultSummary>(0, s => s != null),
                //     OnCompleted<IResultSummary>(0)
                // );
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnSummaryWithQueryText()
        {
            VerifySummaryQueryTextAndParams("UNWIND RANGE(1, 10) AS n RETURN n, true", null);
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnSummaryWithQueryTextAndParams()
        {
            VerifySummaryQueryTextAndParams("UNWIND RANGE(1,$x) AS n RETURN n, $y",
                new {x = 50, y = false});
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnQueryTypeAsWriteOnly()
        {
            VerifySummary("CREATE (n)", null, MatchesSummary(new {QueryType = QueryType.WriteOnly}));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnQueryTypeAsReadOnly()
        {
            VerifySummary("MATCH (n) RETURN n LIMIT 1", null,
                MatchesSummary(new {QueryType = QueryType.ReadOnly}));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnQueryTypeAsReadWrite()
        {
            VerifySummary("CREATE (n) RETURN n", null,
                MatchesSummary(new {QueryType = QueryType.ReadWrite}));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithCreates()
        {
            VerifySummary("CREATE (n:Label1 {id: $id1})-[:KNOWS]->(m:Label2 {id: $id2}) RETURN n, m",
                new {id1 = 10, id2 = 20},
                MatchesSummary(new {Counters = new Counters(2, 0, 1, 0, 2, 2, 0, 0, 0, 0, 0, 0)}));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithDeletes()
        {
            VerifySummary("CREATE (n:Label3 {id: $id1})-[:KNOWS]->(m:Label4 {id: $id2}) RETURN n, m",
                new {id1 = 10, id2 = 20}, s => true);
            VerifySummary("MATCH (n:Label3)-[r:KNOWS]->(m:Label4) DELETE n, r", null,
                MatchesSummary(new {Counters = new Counters(0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0)}));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithIndexCreate()
        {
            VerifySummary("CREATE INDEX on :Label(prop)", null,
                MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0)}));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithIndexRemove()
        {
            TryPrep("CREATE INDEX on :Label(prop)");

            VerifySummary("DROP INDEX on :Label(prop)", null, 
                MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0)}));
        }

        protected virtual void TryPrep(string query)
        {
            using var session = Server.Driver.Session();
            try
            {
                session.Run(query).Consume();
            }
            catch (Neo4jException)
            {
                // ignore.
            }
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithConstraintCreate()
        {
            VerifySummary("CREATE CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE", null,
                MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0)}));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldReturnUpdateStatisticsWithConstraintRemove()
        {
            // Ensure that a constraint exists
            TryPrep("CREATE CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE");

            VerifySummary("DROP CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE", null,
                MatchesSummary(new {Counters = new Counters(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0)}));
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
                        HasPlan = true, 
                        Plan = new Plan("ProduceResults", null, new[] {"n"}, null),
                        HasProfile = false, 
                        Profile = default(IProfiledPlan)
                    }, 
                    opts => opts.Excluding(x => x.SelectedMemberPath == "Plan.OperatorType") 
                        .Excluding(x => x.SelectedMemberPath == "Plan.Arguments")
                        .Excluding(x => x.SelectedMemberPath == "Plan.Children")));
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
            VerifySummary("EXPLAIN MATCH (n:ThisLabelDoesNotExistReactive) RETURN n", null,
                MatchesSummary(new
                    {
                        Notifications = new[]
                        {
                            new Notification("Neo.ClientNotification.Statement.UnknownLabelWarning",
                                null,
                                null,
                                null, 
                                "WARNING")
                        }
                    },
                    options => options.ExcludingMissingMembers()
                        .Excluding(x => x.SelectedMemberPath == "Notifications[0].Position")
                        .Excluding(x => x.SelectedMemberPath == "Notifications[0].Title")
                        .Excluding(x => x.SelectedMemberPath == "Notifications[0].Description")));
        }

        private void VerifySummaryQueryTextAndParams(string query, object parameters)
        {
            VerifySummary(query, parameters,
                MatchesSummary(new {Query = new Query(query, parameters.ToDictionary())}));
        }

        private void VerifySummary(string query, object parameters, Func<IResultSummary, bool> predicate)
        {
            var summary = NewRunnable()
                .Run(query, parameters)
                .Consume()
                .FirstOrDefaultAsync()
                .Wait();

            predicate(summary).Should().BeTrue();
            // .WaitForCompletion()
            // .AssertEqual(
            //     OnNext(0, predicate),
            //     OnCompleted<IResultSummary>(0)
            // );
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                using var session = Server.Driver.Session();
                
                var constaints = session.Run("CALL db.constraints()").ToList();
                foreach (var drop in constaints)
                {
                    if (drop.Values.TryGetValue("name", out var name))
                    {
                        session.Run($"DROP CONSTRAINT {name}").Consume();
                    }
                }

                var indices = session.Run("CALL db.indexes()").ToList();
                foreach (var drop in indices)
                {
                    if (drop.Values.TryGetValue("name", out var name))
                    {
                        session.Run($"DROP INDEX {name}").Consume();
                    }
                }
            }

            //Mark as disposed
            _disposed = true;

            base.Dispose(disposing);
        }
    }

    public class Session : Specs
    {
        private bool _disposed = false;
        private readonly IRxSession rxSession;

        ~Session() => Dispose(false);

        public Session(ITestOutputHelper output, SingleServerFixture standAlone)
            : base(output, standAlone)
        {
            rxSession = NewSession();
        }

        protected override IRxRunnable NewRunnable()
        {
            return rxSession;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                rxSession.Close<object>().ToArray().Wait();
            }

            //Mark as disposed
            _disposed = true;

            base.Dispose(disposing);
        }

    }

    public class Transaction : Specs
    {
        private bool _disposed = false;
        private readonly IRxSession rxSession;
        private readonly IRxTransaction rxTransaction;

        ~Transaction() => Dispose(false);

        public Transaction(ITestOutputHelper output, SingleServerFixture standAlone)
            : base(output, standAlone)
        {
            rxSession = NewSession();
            rxTransaction = rxSession.BeginTransaction().SingleAsync().Wait();
        }
            
        protected override IRxRunnable NewRunnable()
        {
            return rxTransaction;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;
        
            if (disposing)
            {
                rxTransaction.Commit<int>().ToList().Wait();
            }
        
            //Mark as disposed
            _disposed = true;
        
            base.Dispose(disposing);
        }
    }
}