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

using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    public class BlockingWriteCommandUsingReadSession<TContext> : BlockingCommand<TContext>
        where TContext : StressTestContext
    {
        public BlockingWriteCommandUsingReadSession(IDriver driver, bool useBookmark)
            : base(driver, useBookmark)
        {
        }

        public override void Execute(TContext context)
        {
            var result = default(IStatementResult);

            using (var session = NewSession(AccessMode.Read, context))
            {
                var exc = Record.Exception(() =>
                {
                    result = session.Run("CREATE ()");
                    result.Consume();
                    return result;
                });

                exc.Should().BeOfType<ClientException>();
            }

            result.Should().NotBeNull();
            result.Consume().Counters.NodesCreated.Should().Be(0);
        }
    }
}