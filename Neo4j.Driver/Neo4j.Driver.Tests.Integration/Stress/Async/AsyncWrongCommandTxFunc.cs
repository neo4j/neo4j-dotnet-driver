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

public sealed class AsyncWrongCommandTxFunc : AsyncCommand
{
    public AsyncWrongCommandTxFunc(IDriver driver)
        : base(driver, false)
    {
    }

    public override async Task ExecuteAsync(StressTestContext context)
    {
        await using var session = NewSession(AccessMode.Read, context);
        try
        {
            var result = await session.ExecuteReadAsync(
                    async tx =>
                    {
                        var cursor = await tx.RunAsync("RETURN").ConfigureAwait(false);
                        await cursor.ConsumeAsync().ConfigureAwait(false);
                        return false;
                    })
                .ConfigureAwait(false);

            result.Should().BeTrue("If this case is met, the code didn't throw an expected exception in TXFunc");
        }
        catch (Exception ex)
        {
            ex.Should()
                .BeOfType<ClientException>()
                .Which.Code.Should()
                .Be("Neo.ClientError.Statement.SyntaxError");
        }
    }
}
