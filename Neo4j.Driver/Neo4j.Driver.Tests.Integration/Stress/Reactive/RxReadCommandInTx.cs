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
using FluentAssertions;
using Neo4j.Driver.Internal;
using static Neo4j.Driver.Tests.Assertions;

namespace Neo4j.Driver.IntegrationTests.Stress;

public sealed class RxReadCommandInTx : RxCommand
{
    public RxReadCommandInTx(IDriver driver, bool useBookmark)
        : base(driver, useBookmark)
    {
    }

    public override async Task ExecuteAsync(StressTestContext context)
    {
        var session = NewSession(AccessMode.Read, context);

        var summary = await BeginTransaction(session, context)
            .SelectMany(
                txc =>
                {
                    var result = txc.Run("MATCH (n) RETURN n LIMIT 1");

                    return result
                        .Records()
                        .SingleOrDefaultAsync()
                        .All(r => Matches(() => r?[0].Should().BeAssignableTo<INode>()))
                        .SelectMany(_ => result.Consume())
                        .CatchAndThrow(_ => txc.Rollback<IResultSummary>())
                        .Concat(txc.Commit<IResultSummary>());
                })
            .CatchAndThrow(_ => session.Close<IResultSummary>())
            .Concat(session.Close<IResultSummary>());

        if (summary != null)
        {
            context.NodeRead(summary);
        }
    }
}
