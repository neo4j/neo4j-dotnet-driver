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
using Neo4j.Driver.Reactive;
using Xunit.Abstractions;
using static Microsoft.Reactive.Testing.ReactiveAssert;
using static Neo4j.Driver.Reactive.Utils;

namespace Neo4j.Driver.IntegrationTests.Reactive
{
    public class SessionIT : AbstractRxIT
    {
        public SessionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        [RequireServerFact]
        public void ShouldAllowSessionRun()
        {
            var session = Server.Driver.RxSession();

            session.Run("UNWIND [1,2,3,4] AS n RETURN n")
                .Records()
                .Select(r => r["n"].As<int>())
                .OnErrorResumeNext(session.Close<int>())
                .WaitForCompletion()
                .AssertEqual(
                    OnNext(0, 1),
                    OnNext(0, 2),
                    OnNext(0, 3),
                    OnNext(0, 4),
                    OnCompleted<int>(0));
        }

        [RequireServerFact]
        public void ShouldBeAbleToReuseSessionAfterFailure()
        {
            var session = NewSession();

            session.Run("INVALID STATEMENT")
                .Records()
                .WaitForCompletion()
                .AssertEqual(
                    OnError<IRecord>(0, MatchesException<ClientException>()));

            session.Run("RETURN 1")
                .Records()
                .WaitForCompletion()
                .AssertEqual(
                    OnNext(0, MatchesRecord(new[] {"1"}, 1)),
                    OnCompleted<IRecord>(0));
        }

        [RequireServerFact]
        public void ShouldRunTransactionWithoutRetries()
        {
            var work = new ConfigurableTransactionWork("CREATE (:WithoutRetry) RETURN 5");

            NewSession()
                .WriteTransaction(work.Work)
                .WaitForCompletion()
                .AssertEqual(
                    OnNext(0, 5),
                    OnCompleted<int>(0));

            work.Invocations.Should().Be(1);
            CountNodes("WithoutRetry").Should().Be(1);
        }

        [RequireServerFact]
        public void ShouldRunTransactionWithRetriesOnReactiveFailures()
        {
            var work = new ConfigurableTransactionWork("CREATE (:WithReactiveFailure) RETURN 7")
            {
                ReactiveFailures = new Exception[]
                {
                    new ServiceUnavailableException("service is unavailable"),
                    new SessionExpiredException("expired"),
                    new TransientException("transient", "transient error")
                }
            };

            NewSession()
                .WriteTransaction(work.Work)
                .WaitForCompletion()
                .AssertEqual(
                    OnNext(0, 7),
                    OnCompleted<int>(0));

            work.Invocations.Should().Be(4);
            CountNodes("WithReactiveFailure").Should().Be(1);
        }

        [RequireServerFact]
        public void ShouldRunTransactionWithRetriesOnSynchronousFailures()
        {
            var work = new ConfigurableTransactionWork("CREATE (:WithSyncFailure) RETURN 7")
            {
                SyncFailures = new Exception[]
                {
                    new ServiceUnavailableException("service is unavailable"),
                    new SessionExpiredException("expired"),
                    new TransientException("transient", "transient error")
                }
            };

            NewSession()
                .WriteTransaction(work.Work)
                .WaitForCompletion()
                .AssertEqual(
                    OnNext(0, 7),
                    OnCompleted<int>(0));

            work.Invocations.Should().Be(4);
            CountNodes("WithSyncFailure").Should().Be(1);
        }

        [RequireServerFact("4.0.0", VersionComparison.GreaterThanOrEqualTo)]
        public void ShouldFailOnTransactionThatCannotBeRetried()
        {
            var work = new ConfigurableTransactionWork("UNWIND [10, 5, 0] AS x CREATE (:Hi) RETURN 10/x");

            NewSession()
                .WriteTransaction(work.Work)
                .WaitForCompletion()
                .AssertEqual(
                    OnNext(0, 1),
                    OnNext(0, 2),
                    OnError<int>(0, MatchesException<ClientException>(e => e.Message.Contains("/ by zero"))));

            work.Invocations.Should().Be(1);
            CountNodes("Hi").Should().Be(0);
        }

        [RequireServerFact]
        public void ShouldFailEvenAfterATransientError()
        {
            var work = new ConfigurableTransactionWork("CREATE (:Person) RETURN 1")
            {
                SyncFailures = new[] {new TransientException("code", "error")},
                ReactiveFailures = new[] {new DatabaseException("offline", "database is offline")}
            };

            NewSession()
                .WriteTransaction(work.Work)
                .WaitForCompletion()
                .AssertEqual(
                    OnError<int>(0,
                        MatchesException<DatabaseException>(e => e.Message.Contains("database is offline"))));

            work.Invocations.Should().Be(2);
            CountNodes("Person").Should().Be(0);
        }

        private int CountNodes(string label)
        {
            return NewSession()
                .Run($"MATCH (n:{label}) RETURN count(n)")
                .Records()
                .Select(r => r[0].As<int>())
                .SingleAsync()
                .Wait();
        }

        private class ConfigurableTransactionWork
        {
            private readonly string _statement;
            private int _invocations;
            private IEnumerator<Exception> _syncFailures;
            private IEnumerator<Exception> _reactiveFailures;

            public ConfigurableTransactionWork(string statement)
            {
                _statement = statement;
                _invocations = 0;
                _syncFailures = Enumerable.Empty<Exception>().GetEnumerator();
                _reactiveFailures = Enumerable.Empty<Exception>().GetEnumerator();
            }

            public int Invocations => _invocations;

            public IEnumerable<Exception> SyncFailures
            {
                set => _syncFailures = (value ?? Enumerable.Empty<Exception>()).GetEnumerator();
            }

            public IEnumerable<Exception> ReactiveFailures
            {
                set => _reactiveFailures = (value ?? Enumerable.Empty<Exception>()).GetEnumerator();
            }

            public IObservable<int> Work(IRxTransaction txc)
            {
                Interlocked.Increment(ref _invocations);

                if (_syncFailures.MoveNext())
                {
                    throw _syncFailures.Current;
                }

                if (_reactiveFailures.MoveNext())
                {
                    return Observable.Throw<int>(_reactiveFailures.Current);
                }

                return txc.Run(_statement).Records().Select(r => r[0].As<int>());
            }
        }
    }
}