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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct
{
    public class EncryptionIT : DirectDriverTestBase
    {
        public EncryptionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        [RequireServerFact]
        public async Task ShouldBeAbleToConnectWithInsecureConfig()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken,
                o => o
                    .WithEncryptionLevel(EncryptionLevel.Encrypted)
                    .WithTrustManager(TrustManager.CreateInsecure())))
            {
                await VerifyConnectivity(driver);
            }
        }

        [RequireServerFact]
        public async Task ShouldBeAbleToConnectUsingInsecureUri()
        {
            var builder = new UriBuilder("bolt+ssc", ServerEndPoint.Host, ServerEndPoint.Port);
            using (var driver = GraphDatabase.Driver(builder.Uri, AuthToken))
            {
                await VerifyConnectivity(driver);
            }
        }

        private static async Task VerifyConnectivity(IDriver driver)
        {
            var session = driver.AsyncSession();

            try
            {
                var cursor = await session.RunAsync("RETURN 2 as Number");
                var records = await cursor.ToListAsync(r => r["Number"].As<int>());

                records.Should().BeEquivalentTo(2);
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}