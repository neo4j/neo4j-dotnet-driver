// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct;

public sealed class EncryptionIT : DirectDriverTestBase
{
    public EncryptionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
        : base(output, fixture)
    {
    }

    [ShouldNotRunInTestKitRequireServerFact]
    public async Task ShouldBeAbleToConnectWithInsecureConfig()
    {
        await using var driver = GraphDatabase.Driver(
            ServerEndPoint,
            AuthToken,
            o => o
                .WithEncryptionLevel(EncryptionLevel.Encrypted)
                .WithTrustManager(TrustManager.CreateInsecure()));

        await VerifyConnectivity(driver);
    }

    [ShouldNotRunInTestKitRequireServerFact]
    public async Task ShouldBeAbleToConnectUsingInsecureUri()
    {
        var builder = new UriBuilder("bolt+ssc", ServerEndPoint.Host, ServerEndPoint.Port);
        await using var driver = GraphDatabase.Driver(builder.Uri, AuthToken);
        await VerifyConnectivity(driver);
    }

    private static async Task VerifyConnectivity(IDriver driver)
    {
        await using var session = driver.AsyncSession();

            var cursor = await session.RunAsync("RETURN 2 as Number");
            var records = await cursor.ToListAsync(r => r["Number"].As<int>());

            records.Should().BeEquivalentTo(new []{2});
    }
}
