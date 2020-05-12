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

using System.Threading.Tasks;
using static Neo4j.Driver.SessionConfigBuilder;

namespace Neo4j.Driver.IntegrationTests
{
    public class DatabaseExtensions
    {
        public static async Task CreateDatabase(IDriver driver, string name)
        {
            var session = driver.AsyncSession(ForDatabase("system"));

            try
            {
                //TODO: Remove - test for teamcity build only.
                //await driver.VerifyConnectivityAsync(); // - Test only  
                System.Threading.Thread.Sleep(2000); // - Test only

                var cursor = await session.RunAsync($"CREATE DATABASE {name}");
                await cursor.ConsumeAsync();
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        public static async Task DropDatabase(IDriver driver, string name)
        {
            var session = driver.AsyncSession(ForDatabase("system"));
            try
            {
                //TODO: Remove - test for teamcity build only.
                //await driver.VerifyConnectivityAsync(); // - Test only
                System.Threading.Thread.Sleep(2000); // - Test only

                var cursor = await session.RunAsync($"DROP DATABASE {name}");
                await cursor.ConsumeAsync();
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}