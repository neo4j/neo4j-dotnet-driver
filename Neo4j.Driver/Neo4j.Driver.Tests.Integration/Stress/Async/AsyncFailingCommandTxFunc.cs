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

using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Neo4j.Driver.IntegrationTests.Stress;

public sealed class AsyncFailingCommandTxFunc : AsyncCommand
{
    public AsyncFailingCommandTxFunc(IDriver driver)
        : base(driver, false)
    {
    }

    public override async Task ExecuteAsync(StressTestContext context)
    {
        await using var session = NewSession(AccessMode.Read, context);

        try
        {
            var succeeded = await session.ExecuteReadAsync(
                    async tx =>
                    {
                        var cursor = await tx.RunAsync("UNWIND [10, 5, 0] AS x RETURN 10 / x").ConfigureAwait(false);
                        await cursor.ConsumeAsync().ConfigureAwait(false);
                        return true;
                    })
                .ConfigureAwait(false);

            succeeded.Should().BeFalse("Test should have thrown");
        }
        catch (Exception exc)
        {
            exc.Should()
                .BeOfType<ClientException>()
                .Which.Message.Should()
                .Contain("/ by zero");
        }
    }
}
