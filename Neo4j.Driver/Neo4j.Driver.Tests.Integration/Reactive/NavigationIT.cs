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

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnKeys()
            {
                NewRunnable()
                    .Run("RETURN 1 as f1, true as f2, 'string' as f3")
                    .Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesKeys("f1", "f2", "f3")),
                        OnCompleted<string[]>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnSummary()
            {
                NewRunnable()
                    .Run("RETURN 1 as f1, true as f2, 'string' as f3")
                    .Consume()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnKeysAndRecords()
            {
                var keys = new[] {"number", "text"};
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesKeys("number", "text")),
                        OnCompleted<string[]>(0)
                    );

                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesRecord(keys, 1, "t1")),
                        OnNext(0, MatchesRecord(keys, 2, "t2")),
                        OnNext(0, MatchesRecord(keys, 3, "t3")),
                        OnNext(0, MatchesRecord(keys, 4, "t4")),
                        OnNext(0, MatchesRecord(keys, 5, "t5")),
                        OnCompleted<IRecord>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnKeysAndRecordsAndSummary()
            {
                var keys = new[] {"number", "text"};
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesKeys(keys)),
                        OnCompleted<string[]>(0)
                    );

                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesRecord(keys, 1, "t1")),
                        OnNext(0, MatchesRecord(keys, 2, "t2")),
                        OnNext(0, MatchesRecord(keys, 3, "t3")),
                        OnNext(0, MatchesRecord(keys, 4, "t4")),
                        OnNext(0, MatchesRecord(keys, 5, "t5")),
                        OnCompleted<IRecord>(0)
                    );

                result.Consume()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnKeysAndSummaryButRecords()
            {
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                // When
                result.Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesKeys("number", "text")),
                        OnCompleted<string[]>(0)
                    );

                result.Consume()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                        OnCompleted<IResultSummary>(0)
                    );

                // Then
                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<IRecord>(0, MatchesException<ResultConsumedException>())
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnKeysButSummaryOrRecords()
            {
                var runner = NewRunnable();
                var result = runner.Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                if (runner is IRxSession rxSession)
                {
                    rxSession.Close<int>().WaitForCompletion().AssertEqual(OnCompleted<int>(0));
                }
                else if (runner is IRxTransaction rxTx)
                {
                    rxTx.Commit<int>().WaitForCompletion().AssertEqual(OnCompleted<int>(0));
                }

                // When
                result.Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<string[]>(0, MatchesException<ClientException>())
                    );

                result.Consume()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<IResultSummary>(0, MatchesException<ClientException>())
                    );

                // Then
                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<IRecord>(0, MatchesException<ClientException>())
                    );
            }


            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnKeysEvenAfterRecordsAreComplete()
            {
                var keys = new[] {"number", "text"};
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesRecord(keys, 1, "t1")),
                        OnNext(0, MatchesRecord(keys, 2, "t2")),
                        OnNext(0, MatchesRecord(keys, 3, "t3")),
                        OnNext(0, MatchesRecord(keys, 4, "t4")),
                        OnNext(0, MatchesRecord(keys, 5, "t5")),
                        OnCompleted<IRecord>(0)
                    );

                result.Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesKeys(keys)),
                        OnCompleted<string[]>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnSummaryAfterRecordsAreComplete()
            {
                var keys = new[] {"number", "text"};
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesRecord(keys, 1, "t1")),
                        OnNext(0, MatchesRecord(keys, 2, "t2")),
                        OnNext(0, MatchesRecord(keys, 3, "t3")),
                        OnNext(0, MatchesRecord(keys, 4, "t4")),
                        OnNext(0, MatchesRecord(keys, 5, "t5")),
                        OnCompleted<IRecord>(0)
                    );

                result.Consume()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnKeysEvenAfterSummaryIsComplete()
            {
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Consume()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                        OnCompleted<IResultSummary>(0)
                    );

                result.Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesKeys("number", "text")),
                        OnCompleted<string[]>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnKeysForEachObserver()
            {
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                var keys1 = result.Keys().WaitForCompletion();
                result.Consume().WaitForCompletion();

                var keys2 = result.Keys().WaitForCompletion();
                var keys3 = result.Keys().WaitForCompletion();

                keys1.AssertEqual(
                    OnNext(0, MatchesKeys("number", "text")),
                    OnCompleted<string[]>(0)
                );
                keys1.AssertEqual(keys2);
                keys1.AssertEqual(keys3);
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnSummaryForEachObserver()
            {
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                var summary1 = result.Consume().WaitForCompletion();
                var summary2 = result.Consume().WaitForCompletion();
                var summary3 = result.Consume().WaitForCompletion();

                summary1.AssertEqual(
                    OnNext(0, MatchesSummary(new {StatementType = StatementType.ReadOnly})),
                    OnCompleted<IResultSummary>(0)
                );
                summary1.AssertEqual(summary2);
                summary1.AssertEqual(summary3);
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldSubsequentRecordsThrowsError()
            {
                var keys = new[] {"number", "text"};
                var result = NewRunnable().Run("UNWIND RANGE(1,5) AS n RETURN n as number, 't'+n as text");

                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesRecord(keys, 1, "t1")),
                        OnNext(0, MatchesRecord(keys, 2, "t2")),
                        OnNext(0, MatchesRecord(keys, 3, "t3")),
                        OnNext(0, MatchesRecord(keys, 4, "t4")),
                        OnNext(0, MatchesRecord(keys, 5, "t5")),
                        OnCompleted<IRecord>(0)
                    );

                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<IRecord>(0, MatchesException<ResultConsumedException>())
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnEmptyKeysForStatementWithNoReturn()
            {
                NewRunnable()
                    .Run("CREATE ({ id: $id })", new {id = 5})
                    .Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesKeys()),
                        OnCompleted<string[]>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnEmptyRecordsForStatementWithNoReturn()
            {
                NewRunnable()
                    .Run("CREATE ({ id: $id })", new {id = 5})
                    .Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnCompleted<IRecord>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldReturnSummaryForStatementWithNoReturn()
            {
                NewRunnable()
                    .Run("CREATE ({ id: $id })", new {id = 5})
                    .Consume()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0,
                            MatchesSummary(new
                                {Counters = new {NodesCreated = 1}, StatementType = StatementType.WriteOnly})),
                        OnCompleted<IResultSummary>(0)
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldFailOnKeysWhenRunFails()
            {
                NewRunnable()
                    .Run("THIS IS NOT A CYPHER")
                    .Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<string[]>(0,
                            MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldFailOnSubsequentKeysWhenRunFails()
            {
                var result = NewRunnable().Run("THIS IS NOT A CYPHER");

                var keys1 = result.Keys().WaitForCompletion();
                var keys2 = result.Keys().WaitForCompletion();

                keys1.AssertEqual(
                    OnError<string[]>(0, MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                );
                keys1.AssertEqual(keys2);
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldFailOnRecordsWhenRunFails()
            {
                var result = NewRunnable()
                    .Run("THIS IS NOT A CYPHER");

                result
                    .Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<IRecord>(0,
                            MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                    );

                result
                    .Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<IRecord>(0,
                            MatchesException<ResultConsumedException>())
                    );
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldFailOnSummaryWhenRunFails()
            {
                var result = NewRunnable().Run("THIS IS NOT A CYPHER");

                var summary1 = result.Consume();
                var summary2 = result.Consume();
                summary1
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<IResultSummary>(0,
                            MatchesException<ClientException>(e => e.Message.StartsWith("Invalid input")))
                    );

                summary2.AssertEqual(summary1);
            }

            [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
            public void ShouldStreamRecordsOnDemand()
            {
                var result = NewRunnable().Run("UNWIND RANGE(1, $x) AS n RETURN n", new {x = 10000});

                result.Records()
                    .Select(r => r[0].As<int>())
                    .Take(999)
                    .WaitForCompletion()
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