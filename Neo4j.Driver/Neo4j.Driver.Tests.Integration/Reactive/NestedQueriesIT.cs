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
using static Neo4j.Driver.IntegrationTests.VersionComparison;
using static Neo4j.Driver.Reactive.Utils;

namespace Neo4j.Driver.IntegrationTests.Reactive
{
    public class NestedQueriesIT : AbstractRxIT
    {
        public NestedQueriesIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldErrorToRunNestedQueriesWithSessionRuns()
        {
            const int size = 1024;
            var session = Server.Driver.RxSession(o => o.WithFetchSize(5));

            session.Run("UNWIND range(1, $size) AS x RETURN x", new {size})
                .Records()
                .Select(r => r[0].As<int>())
                .Buffer(10)
                .SelectMany(x =>
                    session.Run("UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id", new {x}).Records())
                .Select(r => r[0].As<int>())
                .WaitForCompletion()
                .AssertEqual(
                    OnError<int>(0,
                        MatchesException<ClientException>(e =>
                            e.Message.Contains("consume the current query result before"))));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldCloseSessionWhenRunNestedQueriesWithSessionRuns()
        {
            const int size = 1024;
            var session = Server.Driver.RxSession(o => o.WithFetchSize(5));

            session.Run("UNWIND range(1, $size) AS x RETURN x", new {size})
                .Records()
                .Select(r => r[0].As<int>())
                .Buffer(10)
                .SelectMany(x =>
                    session.Run("UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id", new {x}).Records())
                .Select(r => r[0].As<int>())
                .OnErrorResumeNext(session.Close<int>())
                .WaitForCompletion()
                .AssertEqual(OnCompleted<int>(0));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldErrorToRunNestedQueriesWithTransactionFunctions()
        {
            const int size = 1024;
            var session = Server.Driver.RxSession(o => o.WithFetchSize(5));

            session.ReadTransaction(txc =>
                    txc.Run("UNWIND range(1, $size) AS x RETURN x", new {size})
                        .Records()
                        .Select(r => r[0].As<int>())
                        .Buffer(10)
                        .SelectMany(x =>
                            session.WriteTransaction(txc2 =>
                                txc2.Run("UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id", new {x}).Records()))
                        .Select(r => r[0].As<int>()))
                .WaitForCompletion()
                .AssertEqual(
                    OnError<int>(0,
                        MatchesException<ClientException>(e =>
                            e.Message.Contains("close the currently open transaction object before"))));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldCloseSessionWhenRunNestedQueriesWithTransactionFunctions()
        {
            const int size = 1024;
            var session = Server.Driver.RxSession(o => o.WithFetchSize(5));

            session.ReadTransaction(txc =>
                    txc.Run("UNWIND range(1, $size) AS x RETURN x", new {size})
                        .Records()
                        .Select(r => r[0].As<int>())
                        .Buffer(10)
                        .SelectMany(x =>
                            session.WriteTransaction(txc2 =>
                                txc2.Run("UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id", new {x}).Records()))
                        .Select(r => r[0].As<int>()))
                .OnErrorResumeNext(session.Close<int>())
                .WaitForCompletion()
                .AssertEqual(OnCompleted<int>(0));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldErrorToRunNestedQueriesWithSessionRunAndTransactionFunction()
        {
            const int size = 1024;
            var session = Server.Driver.RxSession(o => o.WithFetchSize(5));

            session.Run("UNWIND range(1, $size) AS x RETURN x", new {size})
                .Records()
                .Select(r => r[0].As<int>())
                .Buffer(10)
                .SelectMany(x =>
                    session.WriteTransaction(txc2 =>
                        txc2.Run("UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id", new {x}).Records()))
                .Select(r => r[0].As<int>())
                .WaitForCompletion()
                .AssertEqual(
                    OnError<int>(0,
                        MatchesException<ClientException>(e =>
                            e.Message.Contains("consume the current query result before"))));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldCloseSessionWhenRunNestedQueriesWithSessionRunAndTransactionFunction()
        {
            const int size = 1024;
            var session = Server.Driver.RxSession(o => o.WithFetchSize(5));

            session.Run("UNWIND range(1, $size) AS x RETURN x", new {size})
                .Records()
                .Select(r => r[0].As<int>())
                .Buffer(10)
                .SelectMany(x =>
                    session.WriteTransaction(txc2 =>
                        txc2.Run("UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id", new {x}).Records()))
                .Select(r => r[0].As<int>())
                .OnErrorResumeNext(session.Close<int>())
                .WaitForCompletion()
                .AssertEqual(OnCompleted<int>(0));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldErrorToRunNestedQueriesWithTransactionFunctionAndSessionRun()
        {
            const int size = 1024;
            var session = Server.Driver.RxSession(o => o.WithFetchSize(5));

            session.ReadTransaction(txc => txc.Run("UNWIND range(1, $size) AS x RETURN x", new {size})
                    .Records()
                    .Select(r => r[0].As<int>())
                    .Buffer(10)
                    .SelectMany(x =>
                        session.Run("UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id", new {x}).Records()))
                .Select(r => r[0].As<int>())
                .WaitForCompletion()
                .AssertEqual(
                    OnError<int>(0,
                        MatchesException<ClientException>(e =>
                            e.Message.Contains("close the currently open transaction object before"))));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public void ShouldCloseSessionWhenRunNestedQueriesWithTransactionFunctionAndSessionRun()
        {
            const int size = 1024;
            var session = Server.Driver.RxSession(o => o.WithFetchSize(5));

            session.ReadTransaction(txc => txc.Run("UNWIND range(1, $size) AS x RETURN x", new {size})
                    .Records()
                    .Select(r => r[0].As<int>())
                    .Buffer(10)
                    .SelectMany(x =>
                        session.Run("UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id", new {x}).Records()))
                .Select(r => r[0].As<int>())
                .OnErrorResumeNext(session.Close<int>())
                .WaitForCompletion()
                .AssertEqual(OnCompleted<int>(0));
        }
    }
}