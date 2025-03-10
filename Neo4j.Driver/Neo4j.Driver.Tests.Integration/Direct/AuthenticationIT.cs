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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal.Auth;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct;

public sealed class AuthenticationIT : DirectDriverTestBase
{
    public AuthenticationIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
        : base(output, fixture)
    {
    }

    [RequireServerFact]
    public async Task AuthenticationErrorIfWrongAuthToken()
    {
        await using var driver = GraphDatabase.Driver(ServerEndPoint, AuthTokens.Basic("fake", "fake"));
        await using var session = driver.AsyncSession();
        var exc = await Record.ExceptionAsync(() => session.RunAsync("Return 1"));

        exc.Should()
            .BeOfType<AuthenticationException>()
            .Which
            .Message.Should()
            .Contain("The client is unauthorized due to authentication failure.");
    }

    [RequireServerFact]
    public async Task ShouldProvideRealmWithBasicAuthToken()
    {
        var oldAuthToken = AuthToken.AsDictionary();
        var newAuthToken = AuthTokens.Basic(
            oldAuthToken["principal"].As<string>(),
            oldAuthToken["credentials"].As<string>(),
            "native");

        await VerifyConnectivity(ServerEndPoint, newAuthToken);
    }

    [RequireServerFact]
    public async Task ShouldCreateCustomAuthToken()
    {
        var oldAuthToken = AuthToken.AsDictionary();
        var newAuthToken = AuthTokens.Custom(
            oldAuthToken["principal"].As<string>(),
            oldAuthToken["credentials"].As<string>(),
            "native",
            AuthSchemes.Basic);

        await VerifyConnectivity(ServerEndPoint, newAuthToken);
    }

    [RequireServerFact]
    public async Task ShouldCreateCustomAuthTokenWithAdditionalParameters()
    {
        var oldAuthToken = AuthToken.AsDictionary();
        var newAuthToken = AuthTokens.Custom(
            oldAuthToken["principal"].As<string>(),
            oldAuthToken["credentials"].As<string>(),
            "native",
            AuthSchemes.Basic,
            new Dictionary<string, object> { { "secret", 42 } });

        await VerifyConnectivity(ServerEndPoint, newAuthToken);
    }

    private static async Task VerifyConnectivity(Uri address, IAuthToken token)
    {
        await using var driver = GraphDatabase.Driver(address, token);
        await using var session = driver.AsyncSession();

        var cursor = await session.RunAsync("RETURN 2 as Number");
        var records = await cursor.ToListAsync(r => r["Number"].As<int>());

        records.Should().BeEquivalentTo(new []{2});
    }
}
