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

using System.Reactive;
using System.Reactive.Linq;
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
                .SubscribeAndWait(CreateObserver<int>())
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
                .SubscribeAndWait(CreateObserver<IRecord>())
                .AssertEqual(
                    OnError<IRecord>(0, MatchesException<ClientException>()));

            session.Run("RETURN 1")
                .Records()
                .SubscribeAndWait(CreateObserver<IRecord>())
                .AssertEqual(
                    OnNext(0, MatchesRecord(new[] {"1"}, 1)),
                    OnCompleted<IRecord>(0));
        }
    }
}