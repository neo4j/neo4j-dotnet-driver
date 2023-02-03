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

using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.IntegrationTests.Stress;

public class BlockingWriteCommandUsingReadSessionInTx<TContext> : BlockingCommand<TContext>
    where TContext : StressTestContext
{
    public BlockingWriteCommandUsingReadSessionInTx(IDriver driver, bool useBookmark)
        : base(driver, useBookmark)
    {
    }

    public override void Execute(TContext context)
    {
        var result = default(IResult);

        using (var session = NewSession(AccessMode.Read, context))
        using (var txc = BeginTransaction(session, context))
        {
            var exc = Record.Exception(
                () =>
                {
                    result = txc.Run("CREATE ()");
                    result.Consume();
                    return result;
                });

            exc.Should().BeOfType<ClientException>();
        }

        result.Should().NotBeNull();
        result.Consume().Counters.NodesCreated.Should().Be(0);
    }
}
