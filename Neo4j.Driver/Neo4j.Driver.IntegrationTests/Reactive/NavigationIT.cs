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

using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.Reactive.Testing;
using Neo4j.Driver.Reactive;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.VersionComparison;
using static Neo4j.Driver.Reactive.Utils;

namespace Neo4j.Driver.IntegrationTests.Reactive
{
    public static class NavigationIT
    {
        public abstract class Specs : AbstractRxIT
        {
            protected Specs(ITestOutputHelper output, StandAloneIntegrationTestFixture standAlone)
                : base(output, standAlone)
            {
            }

            protected abstract IRxRunnable NewRunnable();

            [RequireServerFact]
            public void ShouldReturnKeys()
            {
                NewRunnable()
                    .Run("RETURN 1 as f1, true as f2, 'string' as f3")
                    .Keys()
                    .SubscribeAndWait(CreateObserver<string[]>())
                    .AssertEqual(
                        OnNext(0, MatchesKeys("f1", "f2", "f3")),
                        OnCompleted<string[]>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnSummary()
            {
                NewRunnable()
                    .Run("RETURN 1 as f1, true as f2, 'string' as f3")
                    .Summary()
                    .SubscribeAndWait(CreateObserver<IResultSummary>())
                    .AssertEqual(
                        OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnKeysAndRecords()
            {
                var keys = new[] {"number", "text"};
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Keys()
                    .SubscribeAndWait(CreateObserver<string[]>())
                    .AssertEqual(
                        OnNext(0, MatchesKeys("number", "text")),
                        OnCompleted<string[]>(0)
                    );

                result.Records()
                    .SubscribeAndWait(CreateObserver<IRecord>())
                    .AssertEqual(
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
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Keys()
                    .SubscribeAndWait(CreateObserver<string[]>())
                    .AssertEqual(
                        OnNext(0, MatchesKeys(keys)),
                        OnCompleted<string[]>(0)
                    );

                result.Records()
                    .SubscribeAndWait(CreateObserver<IRecord>())
                    .AssertEqual(
                        OnNext(0, MatchesRecord(keys, 1, "t1")),
                        OnNext(0, MatchesRecord(keys, 2, "t2")),
                        OnNext(0, MatchesRecord(keys, 3, "t3")),
                        OnNext(0, MatchesRecord(keys, 4, "t4")),
                        OnNext(0, MatchesRecord(keys, 5, "t5")),
                        OnCompleted<IRecord>(0)
                    );

                result.Summary()
                    .SubscribeAndWait(CreateObserver<IResultSummary>())
                    .AssertEqual(
                        OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnKeysAndSummaryButRecords()
            {
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                // When
                result.Keys()
                    .SubscribeAndWait(CreateObserver<string[]>())
                    .AssertEqual(
                        OnNext(0, MatchesKeys("number", "text")),
                        OnCompleted<string[]>(0)
                    );

                result.Summary()
                    .SubscribeAndWait(CreateObserver<IResultSummary>())
                    .AssertEqual(
                        OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                        OnCompleted<IResultSummary>(0)
                    );

                // Then
                result.Records()
                    .SubscribeAndWait(CreateObserver<IRecord>())
                    .AssertEqual(
                        OnCompleted<IRecord>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnKeysEvenAfterRecordsAreComplete()
            {
                var keys = new[] {"number", "text"};
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Records()
                    .SubscribeAndWait(CreateObserver<IRecord>())
                    .AssertEqual(
                        OnNext(0, MatchesRecord(keys, 1, "t1")),
                        OnNext(0, MatchesRecord(keys, 2, "t2")),
                        OnNext(0, MatchesRecord(keys, 3, "t3")),
                        OnNext(0, MatchesRecord(keys, 4, "t4")),
                        OnNext(0, MatchesRecord(keys, 5, "t5")),
                        OnCompleted<IRecord>(0)
                    );

                result.Keys()
                    .SubscribeAndWait(CreateObserver<string[]>())
                    .AssertEqual(
                        OnNext(0, MatchesKeys(keys)),
                        OnCompleted<string[]>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnSummaryAfterRecordsAreComplete()
            {
                var keys = new[] {"number", "text"};
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Records()
                    .SubscribeAndWait(CreateObserver<IRecord>())
                    .AssertEqual(
                        OnNext(0, MatchesRecord(keys, 1, "t1")),
                        OnNext(0, MatchesRecord(keys, 2, "t2")),
                        OnNext(0, MatchesRecord(keys, 3, "t3")),
                        OnNext(0, MatchesRecord(keys, 4, "t4")),
                        OnNext(0, MatchesRecord(keys, 5, "t5")),
                        OnCompleted<IRecord>(0)
                    );

                result.Summary()
                    .SubscribeAndWait(CreateObserver<IResultSummary>())
                    .AssertEqual(
                        OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnKeysEvenAfterSummaryIsComplete()
            {
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Summary()
                    .SubscribeAndWait(CreateObserver<IResultSummary>())
                    .AssertEqual(
                        OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                        OnCompleted<IResultSummary>(0)
                    );

                result.Keys()
                    .SubscribeAndWait(CreateObserver<string[]>())
                    .AssertEqual(
                        OnNext(0, MatchesKeys("number", "text")),
                        OnCompleted<string[]>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnKeysForEachObserver()
            {
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var keysObserver1 = CreateObserver<string[]>();
                var keysObserver2 = CreateObserver<string[]>();
                var keysObserver3 = CreateObserver<string[]>();

                result.Keys().SubscribeAndWait(keysObserver1);
                result.Summary().SubscribeAndDiscard();

                result.Keys().SubscribeAndWait(keysObserver2);
                result.Keys().SubscribeAndWait(keysObserver3);

                keysObserver1.AssertEqual(
                    OnNext(0, MatchesKeys("number", "text")),
                    OnCompleted<string[]>(0)
                );
                keysObserver1.AssertEqual(keysObserver2);
                keysObserver1.AssertEqual(keysObserver3);
            }

            [RequireServerFact]
            public void ShouldReturnSummaryForEachObserver()
            {
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");
                var summaryObserver1 = CreateObserver<IResultSummary>();
                var summaryObserver2 = CreateObserver<IResultSummary>();
                var summaryObserver3 = CreateObserver<IResultSummary>();

                result.Summary().SubscribeAndWait(summaryObserver1);
                result.Summary().SubscribeAndWait(summaryObserver2);
                result.Summary().SubscribeAndWait(summaryObserver3);

                summaryObserver1.AssertEqual(
                    OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                    OnCompleted<IResultSummary>(0)
                );
                summaryObserver1.AssertEqual(summaryObserver2);
                summaryObserver1.AssertEqual(summaryObserver3);
            }

            [RequireServerFact]
            public void ShouldSubsequentRecordsReturnEmpty()
            {
                var keys = new[] {"number", "text"};
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Records()
                    .SubscribeAndWait(CreateObserver<IRecord>())
                    .AssertEqual(
                        OnNext(0, MatchesRecord(keys, 1, "t1")),
                        OnNext(0, MatchesRecord(keys, 2, "t2")),
                        OnNext(0, MatchesRecord(keys, 3, "t3")),
                        OnNext(0, MatchesRecord(keys, 4, "t4")),
                        OnNext(0, MatchesRecord(keys, 5, "t5")),
                        OnCompleted<IRecord>(0)
                    );

                result.Records()
                    .SubscribeAndWait(CreateObserver<IRecord>())
                    .AssertEqual(
                        OnCompleted<IRecord>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnEmptyKeysForStatementWithNoReturn()
            {
                NewRunnable()
                    .Run("CREATE ({ id: $id })", new {id = 5})
                    .Keys()
                    .SubscribeAndWait(CreateObserver<string[]>())
                    .AssertEqual(
                        OnNext(0, MatchesKeys()),
                        OnCompleted<string[]>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnEmptyRecordsForStatementWithNoReturn()
            {
                NewRunnable()
                    .Run("CREATE ({ id: $id })", new {id = 5})
                    .Records()
                    .SubscribeAndWait(CreateObserver<IRecord>())
                    .AssertEqual(
                        OnCompleted<IRecord>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldReturnSummaryForStatementWithNoReturn()
            {
                NewRunnable()
                    .Run("CREATE ({ id: $id })", new {id = 5})
                    .Summary()
                    .SubscribeAndWait(CreateObserver<IResultSummary>())
                    .AssertEqual(
                        OnNext(0,
                            MatchesSummary(new
                                {Counters = new {NodesCreated = 1}, StatementType = StatementType.WriteOnly})),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            [RequireServerFact]
            public void ShouldFailOnKeysWhenRunFails()
            {
                NewRunnable()
                    .Run("THIS IS NOT A CYPHER")
                    .Keys()
                    .SubscribeAndWait(CreateObserver<string[]>())
                    .AssertEqual(
                        OnError<string[]>(0,
                            MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                    );
            }

            [RequireServerFact]
            public void ShouldFailOnSubsequentKeysWhenRunFails()
            {
                var result = NewRunnable().Run("THIS IS NOT A CYPHER");
                var keysObserver1 = CreateObserver<string[]>();
                var keysObserver2 = CreateObserver<string[]>();

                result.Keys().SubscribeAndWait(keysObserver1);
                result.Keys().SubscribeAndWait(keysObserver2);

                keysObserver1.AssertEqual(
                    OnError<string[]>(0, MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                );
                keysObserver1.AssertEqual(keysObserver2);
            }

            [RequireServerFact]
            public void ShouldFailOnRecordsWhenRunFails()
            {
                NewRunnable()
                    .Run("THIS IS NOT A CYPHER")
                    .Records()
                    .SubscribeAndWait(CreateObserver<IRecord>())
                    .AssertEqual(
                        OnError<IRecord>(0,
                            MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                    );
            }

            [RequireServerFact]
            public void ShouldFailOnSummaryWhenRunFails()
            {
                var result = NewRunnable().Run("THIS IS NOT A CYPHER");

                result.Summary()
                    .SubscribeAndWait(CreateObserver<IResultSummary>())
                    .AssertEqual(
                        OnError<IResultSummary>(0,
                            MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldStreamRecordsOnDemand()
            {
                var result = NewRunnable().Run("UNWIND RANGE(1, $x) AS n RETURN n", new {x = 10000});

                result.Records()
                    .Select(r => r[0].As<int>())
                    .Take(999)
                    .SubscribeAndWait(CreateObserver<int>())
                    .AssertEqual(
                        Enumerable.Range(1, 999).Select(i => OnNext(0, i)).Concat(new[] {OnCompleted<int>(0)}).ToArray()
                    );
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