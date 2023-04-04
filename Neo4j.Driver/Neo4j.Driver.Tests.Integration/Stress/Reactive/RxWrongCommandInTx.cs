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

using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Neo4j.Driver.Internal;
using static Neo4j.Driver.Reactive.Utils;

namespace Neo4j.Driver.IntegrationTests.Stress;

public sealed class RxWrongCommandInTx : RxCommand
{
    public RxWrongCommandInTx(IDriver driver)
        : base(driver, false)
    {
    }

    public override async Task ExecuteAsync(StressTestContext context)
    {
        var session = NewSession(AccessMode.Read, context);

        var result = await
            BeginTransaction(session, context)
                .SelectMany(
                    txc => txc
                        .Run("RETURN")
                        .Records()
                        .CatchAndThrow(exc => txc.Rollback<IRecord>())
                        .Concat(txc.Commit<IRecord>()))
                .CatchAndThrow(_ => session.Close<IRecord>())
                .Concat(session.Close<IRecord>())
                .Materialize()
                .Select(r => new Recorded<Notification<IRecord>>(0, r))
                .ToList();

        result.AssertEqual(
            OnError<IRecord>(
                0,
                MatchesException<ClientException>(exc => exc.Code.Equals("Neo.ClientError.Statement.SyntaxError"))));
    }
}
