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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    public class AsyncWriteCommandInTx<TContext> : AsyncCommand<TContext>
        where TContext : StressTestContext
    {
        private readonly StressTest<TContext> _test;

        public AsyncWriteCommandInTx(StressTest<TContext> test, IDriver driver, bool useBookmark)
            : base(driver, useBookmark)
        {
            _test = test ?? throw new ArgumentNullException(nameof(test));
        }

        public override async Task ExecuteAsync(TContext context)
        {
            var summary = default(IResultSummary);
            var error = default(Exception);

            var session = NewSession(AccessMode.Write, context);
            try
            {
                var txc = await BeginTransaction(session, context);
                try
                {
                    var cursor = await txc.RunAsync("CREATE ()");
                    summary = await cursor.ConsumeAsync();

                    await txc.CommitAsync();
                }
                catch
                {
                    await txc.RollbackAsync();
                    throw;
                }

                context.Bookmark = session.LastBookmark;
            }
            catch (Exception exc)
            {
                error = exc;
                if (!_test.HandleWriteFailure(error, context))
                {
                    throw;
                }
            }
            finally
            {
                await session.CloseAsync();
            }

            if (error == null && summary != null)
            {
                summary.Counters.NodesCreated.Should().Be(1);
                context.NodeCreated();
            }
        }
    }
}