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
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Reactive;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Reactive.Testing.ReactiveAssert;
using static Neo4j.Driver.IntegrationTests.VersionComparison;
using static Neo4j.Driver.Reactive.Utils;
using static Neo4j.Driver.Tests.Assertions;
using Notification = Neo4j.Driver.Internal.Result.Notification;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.IntegrationTests.Reactive
{
    public class ResultIT
    {
        public class Navigation : AbstractRxIT
        {
            public Navigation(ITestOutputHelper output, StandAloneIntegrationTestFixture standAlone)
                : base(output, standAlone)
            {
            }

            [RequireServerFact]
            public void ShouldReturnKeys()
            {
                var session = NewSession();
                var result = session.Run("RETURN 1 as f1, true as f2, 'string' as f3");
                var observer = CreateObserver<string[]>();

                result.Keys().Concat(session.Close<string[]>()).SubscribeAndWait(observer);

                observer.Messages.AssertEqual(
                    OnNext(0, MatchesKeys("f1", "f2", "f3")),
                    OnCompleted<string[]>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnSummary()
            {
                var session = NewSession();
                var result = session.Run("RETURN 1 as f1, true as f2, 'string' as f3");
                var observer = CreateObserver<IResultSummary>();

                result.Summary().SubscribeAndWait(observer);

                observer.Messages.AssertEqual(
                    OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                    OnCompleted<IResultSummary>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnKeysAndRecords()
            {
                var keys = new[] {"number", "text"};
                var session = NewSession();
                var result = session.Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var keysObserver = CreateObserver<string[]>();
                var recordsObserver = CreateObserver<IRecord>();

                result.Keys().SubscribeAndWait(keysObserver);
                result.Records().SubscribeAndWait(recordsObserver);

                keysObserver.Messages.AssertEqual(
                    OnNext(0, MatchesKeys("number", "text")),
                    OnCompleted<string[]>(0)
                );
                recordsObserver.Messages.AssertEqual(
                    OnNext(0, MatchesRecord(keys, 1, "t1")),
                    OnNext(0, MatchesRecord(keys, 2, "t2")),
                    OnNext(0, MatchesRecord(keys, 3, "t3")),
                    OnNext(0, MatchesRecord(keys, 4, "t4")),
                    OnNext(0, MatchesRecord(keys, 5, "t5")),
                    OnCompleted<IRecord>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnKeysAndRecordsAndSummary()
            {
                var keys = new[] {"number", "text"};
                var session = NewSession();
                var result = session.Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var keysObserver = CreateObserver<string[]>();
                var recordsObserver = CreateObserver<IRecord>();
                var summaryObserver = CreateObserver<IResultSummary>();

                result.Keys().SubscribeAndWait(keysObserver);
                result.Records().SubscribeAndWait(recordsObserver);
                result.Summary().SubscribeAndWait(summaryObserver);

                keysObserver.Messages.AssertEqual(
                    OnNext(0, MatchesKeys("number", "text")),
                    OnCompleted<string[]>(0)
                );
                recordsObserver.Messages.AssertEqual(
                    OnNext(0, MatchesRecord(keys, 1, "t1")),
                    OnNext(0, MatchesRecord(keys, 2, "t2")),
                    OnNext(0, MatchesRecord(keys, 3, "t3")),
                    OnNext(0, MatchesRecord(keys, 4, "t4")),
                    OnNext(0, MatchesRecord(keys, 5, "t5")),
                    OnCompleted<IRecord>(0)
                );
                summaryObserver.Messages.AssertEqual(
                    OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                    OnCompleted<IResultSummary>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnKeysAndSummaryButRecords()
            {
                var session = NewSession();
                var result = session.Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var countDown = new CountdownEvent(3);
                var keysObserver = CreateObserver<string[]>();
                var recordsObserver = CreateObserver<IRecord>();
                var summaryObserver = CreateObserver<IResultSummary>();

                result.Keys().SubscribeAndCountDown(keysObserver, countDown);
                result.Summary().SubscribeAndCountDown(summaryObserver, countDown);
                result.Records().SubscribeAndCountDown(recordsObserver, countDown);

                countDown.Wait();

                keysObserver.Messages.AssertEqual(
                    OnNext(0, MatchesKeys("number", "text")),
                    OnCompleted<string[]>(0)
                );
                summaryObserver.Messages.AssertEqual(
                    OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                    OnCompleted<IResultSummary>(0)
                );
                recordsObserver.Messages.AssertEqual(
                    OnCompleted<IRecord>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnKeysEvenAfterRecordsAreComplete()
            {
                var keys = new[] {"number", "text"};
                var session = NewSession();
                var result = session.Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var keysObserver = CreateObserver<string[]>();
                var recordsObserver = CreateObserver<IRecord>();

                result.Records().SubscribeAndWait(recordsObserver);

                recordsObserver.Messages.AssertEqual(
                    OnNext(0, MatchesRecord(keys, 1, "t1")),
                    OnNext(0, MatchesRecord(keys, 2, "t2")),
                    OnNext(0, MatchesRecord(keys, 3, "t3")),
                    OnNext(0, MatchesRecord(keys, 4, "t4")),
                    OnNext(0, MatchesRecord(keys, 5, "t5")),
                    OnCompleted<IRecord>(0)
                );

                result.Keys().SubscribeAndWait(keysObserver);
                keysObserver.Messages.AssertEqual(
                    OnNext(0, MatchesKeys("number", "text")),
                    OnCompleted<string[]>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnSummaryAfterRecordsAreComplete()
            {
                var keys = new[] {"number", "text"};
                var session = NewSession();
                var result = session.Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var recordsObserver = CreateObserver<IRecord>();
                var summaryObserver = CreateObserver<IResultSummary>();

                result.Records().SubscribeAndWait(recordsObserver);
                recordsObserver.Messages.AssertEqual(
                    OnNext(0, MatchesRecord(keys, 1, "t1")),
                    OnNext(0, MatchesRecord(keys, 2, "t2")),
                    OnNext(0, MatchesRecord(keys, 3, "t3")),
                    OnNext(0, MatchesRecord(keys, 4, "t4")),
                    OnNext(0, MatchesRecord(keys, 5, "t5")),
                    OnCompleted<IRecord>(0)
                );

                result.Summary().SubscribeAndWait(summaryObserver);
                summaryObserver.Messages.AssertEqual(
                    OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                    OnCompleted<IResultSummary>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnKeysEvenAfterSummaryIsComplete()
            {
                var session = NewSession();
                var result = session.Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var keysObserver = CreateObserver<string[]>();
                var summaryObserver = CreateObserver<IResultSummary>();

                result.Summary().SubscribeAndWait(summaryObserver);
                summaryObserver.Messages.AssertEqual(
                    OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                    OnCompleted<IResultSummary>(0)
                );

                result.Keys().SubscribeAndWait(keysObserver);
                keysObserver.Messages.AssertEqual(
                    OnNext(0, MatchesKeys("number", "text")),
                    OnCompleted<string[]>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnKeysForEachObserver()
            {
                var session = NewSession();
                var result = session.Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var keysObserver1 = CreateObserver<string[]>();
                var keysObserver2 = CreateObserver<string[]>();
                var keysObserver3 = CreateObserver<string[]>();

                result.Keys().SubscribeAndWait(keysObserver1);
                result.Summary().SubscribeAndDiscard();

                result.Keys().SubscribeAndWait(keysObserver2);
                result.Keys().SubscribeAndWait(keysObserver3);

                keysObserver1.Messages.AssertEqual(
                    OnNext(0, MatchesKeys("number", "text")),
                    OnCompleted<string[]>(0)
                );
                keysObserver1.Messages.AssertEqual(keysObserver2.Messages);
                keysObserver1.Messages.AssertEqual(keysObserver3.Messages);
            }

            [RequireServerFact]
            public void ShouldReturnSummaryForEachObserver()
            {
                var session = NewSession();
                var result = session.Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var summaryObserver1 = CreateObserver<IResultSummary>();
                var summaryObserver2 = CreateObserver<IResultSummary>();
                var summaryObserver3 = CreateObserver<IResultSummary>();

                result.Summary().SubscribeAndWait(summaryObserver1);
                result.Summary().SubscribeAndWait(summaryObserver2);
                result.Summary().SubscribeAndWait(summaryObserver3);

                summaryObserver1.Messages.AssertEqual(
                    OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                    OnCompleted<IResultSummary>(0)
                );
                summaryObserver1.Messages.AssertEqual(summaryObserver2.Messages);
                summaryObserver1.Messages.AssertEqual(summaryObserver3.Messages);
            }

            [RequireServerFact]
            public void ShouldSubsequentRecordsReturnEmpty()
            {
                var keys = new[] {"number", "text"};
                var session = NewSession();
                var result = session.Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var recordsObserver1 = CreateObserver<IRecord>();
                var recordsObserver2 = CreateObserver<IRecord>();

                result.Records().SubscribeAndWait(recordsObserver1);
                recordsObserver1.Messages.AssertEqual(
                    OnNext(0, MatchesRecord(keys, 1, "t1")),
                    OnNext(0, MatchesRecord(keys, 2, "t2")),
                    OnNext(0, MatchesRecord(keys, 3, "t3")),
                    OnNext(0, MatchesRecord(keys, 4, "t4")),
                    OnNext(0, MatchesRecord(keys, 5, "t5")),
                    OnCompleted<IRecord>(0)
                );

                result.Records().SubscribeAndWait(recordsObserver2);
                recordsObserver2.Messages.AssertEqual(
                    OnCompleted<IRecord>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnEmptyKeysForStatementWithNoReturn()
            {
                var session = NewSession();
                var result = session.Run("CREATE ({ id: $id })", new {id = 5});
                var keysObserver = CreateObserver<string[]>();

                result.Keys().Concat(session.Close<string[]>()).SubscribeAndWait(keysObserver);

                keysObserver.Messages.AssertEqual(
                    OnNext(0, MatchesKeys()),
                    OnCompleted<string[]>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnEmptyRecordsForStatementWithNoReturn()
            {
                var session = NewSession();
                var result = session.Run("CREATE ({ id: $id })", new {id = 5});
                var recordsObserver = CreateObserver<IRecord>();

                result.Records().SubscribeAndWait(recordsObserver);

                recordsObserver.Messages.AssertEqual(
                    OnCompleted<IRecord>(0)
                );
            }

            [RequireServerFact]
            public void ShouldReturnSummaryForStatementWithNoReturn()
            {
                var session = NewSession();
                var result = session.Run("CREATE ({ id: $id })", new {id = 5});
                var summaryObserver = CreateObserver<IResultSummary>();

                result.Summary().SubscribeAndWait(summaryObserver);

                summaryObserver.Messages.AssertEqual(
                    OnNext(0,
                        MatchesSummary(new
                            {Counters = new {NodesCreated = 1}, StatementType = StatementType.WriteOnly})),
                    OnCompleted<IResultSummary>(0)
                );
            }

            [RequireServerFact]
            public void ShouldFailOnKeysWhenRunFails()
            {
                var session = NewSession();
                var result = session.Run("THIS IS NOT A CYPHER");
                var keysObserver = CreateObserver<string[]>();

                result.Keys().Concat(session.Close<string[]>()).SubscribeAndWait(keysObserver);

                keysObserver.Messages.AssertEqual(
                    OnError<string[]>(0, MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                );
            }

            [RequireServerFact]
            public void ShouldFailOnSubsequentKeysWhenRunFails()
            {
                var session = NewSession();
                var result = session.Run("THIS IS NOT A CYPHER");
                var keysObserver1 = CreateObserver<string[]>();
                var keysObserver2 = CreateObserver<string[]>();

                result.Keys().SubscribeAndWait(keysObserver1);
                result.Keys().SubscribeAndWait(keysObserver2);

                keysObserver1.Messages.AssertEqual(
                    OnError<string[]>(0, MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                );
                keysObserver1.Messages.AssertEqual(keysObserver2.Messages);
            }

            [RequireServerFact]
            public void ShouldFailOnRecordsWhenRunFails()
            {
                var session = NewSession();
                var result = session.Run("THIS IS NOT A CYPHER");
                var recordsObserver = CreateObserver<IRecord>();

                result.Records().Concat(session.Close<IRecord>()).SubscribeAndWait(recordsObserver);

                recordsObserver.Messages.AssertEqual(
                    OnError<IRecord>(0, MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                );
            }

            [RequireServerFact]
            public void ShouldFailOnSummaryWhenRunFails()
            {
                var session = NewSession();
                var result = session.Run("THIS IS NOT A CYPHER");
                var summaryObserver = CreateObserver<IResultSummary>();

                result.Summary().Concat(session.Close<IResultSummary>()).SubscribeAndWait(summaryObserver);

                summaryObserver.Messages.AssertEqual(
                    OnError<IResultSummary>(0,
                        MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldStreamRecordsOnDemand()
            {
                var session = NewSession();
                var result = session.Run("UNWIND RANGE(1, $x) AS n RETURN n", new {x = 10000});
                var recordsObserver = CreateObserver<int>();

                result.Records().Select(r => r[0].As<int>()).Take(999).SubscribeAndWait(recordsObserver);

                recordsObserver.Messages.AssertEqual(
                    Enumerable.Range(1, 999).Select(i => OnNext(0, i)).Concat(new[] {OnCompleted<int>(0)})
                );
            }
        }

        public class Summary : AbstractRxIT
        {
            public Summary(ITestOutputHelper output, StandAloneIntegrationTestFixture standAlone)
                : base(output, standAlone)
            {
            }

            [RequireServerFact]
            public void ShouldReturnNonNullSummary()
            {
                var result = NewSession().Run("UNWIND RANGE(1,10) AS n RETURN n");
                var observer = new TestScheduler().CreateObserver<IResultSummary>();

                result.Summary().SubscribeAndWait(observer);

                observer.Messages.AssertEqual(
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
                var result = NewSession().Run(statement, parameters);
                var observer = new TestScheduler().CreateObserver<IResultSummary>();

                result.Summary().SubscribeAndWait(observer);

                observer.Messages.AssertEqual(
                    OnNext(0, predicate),
                    OnCompleted<IResultSummary>(0)
                );
            }
        }
    }
}