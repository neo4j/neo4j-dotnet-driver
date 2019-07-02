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
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    public class AsyncWrongCommand<TContext> : AsyncCommand<TContext>
        where TContext : StressTestContext
    {
        public AsyncWrongCommand(IDriver driver)
            : base(driver, false)
        {
        }

        public override async Task ExecuteAsync(TContext context)
        {
            var session = NewSession(AccessMode.Read, context);
            try
            {
                var exc = await Record.ExceptionAsync(async () =>
                {
                    var cursor = await session.RunAsync("RETURN");
                    await cursor.ConsumeAsync();
                });

                exc.Should().BeOfType<ClientException>().Which.Message.Should().Contain("Unexpected end of input");
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}