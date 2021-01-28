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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    public class AsyncReadCommand<TContext> : AsyncCommand<TContext>
        where TContext : StressTestContext
    {
        public AsyncReadCommand(IDriver driver, bool useBookmark)
            : base(driver, useBookmark)
        {
        }

        public override async Task ExecuteAsync(TContext context)
        {
            var session = NewSession(AccessMode.Read, context);
            try
            {
                var cursor = await session.RunAsync("MATCH (n) RETURN n LIMIT 1");
                var records = await cursor.ToListAsync();
                if (records.Count > 0)
                {
                    records[0][0].Should().BeAssignableTo<INode>();
                    context.NodeRead(await cursor.ConsumeAsync());
                }
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}