// Copyright (c) 2002-2020 "Neo4j,"
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
using FluentAssertions;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    public class BlockingWriteCommandInTx<TContext> : BlockingCommand<TContext>
        where TContext : StressTestContext
    {
        private readonly StressTest<TContext> _test;

        public BlockingWriteCommandInTx(StressTest<TContext> test, IDriver driver, bool useBookmark)
            : base(driver, useBookmark)
        {
            _test = test ?? throw new ArgumentNullException(nameof(test));
        }

        public override void Execute(TContext context)
        {
            var summary = default(IResultSummary);
            var error = default(Exception);

            try
            {
                using (var session = NewSession(AccessMode.Write, context))
                {
                    using (var txc = BeginTransaction(session, context))
                    {
                        summary = txc.Run("CREATE ()").Consume();

                        txc.Commit();
                    }

                    context.Bookmark = session.LastBookmark;
                }
            }
            catch (Exception exc)
            {
                error = exc;
                if (!_test.HandleWriteFailure(error, context))
                {
                    throw;
                }
            }

            if (error == null && summary != null)
            {
                summary.Counters.NodesCreated.Should().Be(1);
                context.NodeCreated();
            }
        }
    }
}