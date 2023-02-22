// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Reactive;
using static Neo4j.Driver.Reactive.Utils;

namespace Neo4j.Driver.IntegrationTests.Stress;

public sealed class RxFailingCommandInTx: RxCommand
{
    public RxFailingCommandInTx(IDriver driver)
        : base(driver, false)
    {
    }

    public override Task ExecuteAsync(StressTestContext context)
    {
        var session = NewSession(AccessMode.Read, context);

        BeginTransaction(session, context)
            .SelectMany(
                txc => txc
                    .Run("UNWIND [10, 5, 0] AS x RETURN 10 / x")
                    .Records()
                    .Select(r => r[0].As<int>())
                    .CatchAndThrow(exc => txc.Rollback<int>())
                    .Concat(txc.Commit<int>()))
            .CatchAndThrow(_ => session.Close<int>())
            .Concat(session.Close<int>())
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, 1),
                OnNext(0, 2),
                OnError<int>(0, MatchesException<ClientException>(exc => exc.Message.Contains("/ by zero"))));

        return Task.CompletedTask;
    }
}
