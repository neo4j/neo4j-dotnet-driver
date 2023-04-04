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

public sealed class AsyncWriteCommandTxFunc : AsyncCommand
{
    private readonly StressTest _test;

    public AsyncWriteCommandTxFunc(StressTest test, IDriver driver, bool useBookmark)
        : base(driver, useBookmark)
    {
        _test = test ?? throw new ArgumentNullException(nameof(test));
    }

    public override async Task ExecuteAsync(StressTestContext context)
    {
        await using var session = NewSession(AccessMode.Write, context);

        try
        {
            var summary = await session.ExecuteWriteAsync(
                    async tx =>
                    {
                        var cursor = await tx.RunAsync("CREATE ()").ConfigureAwait(false);
                        return await cursor.ConsumeAsync().ConfigureAwait(false);
                    })
                .ConfigureAwait(false);

            context.Bookmarks = session.LastBookmarks;

            summary.Counters.NodesCreated.Should().Be(1);
            context.NodeCreated();
        }
        catch (Exception ex)
        {
            if (!_test.HandleWriteFailure(ex, context))
            {
                throw;
            }
        }
    }
}
