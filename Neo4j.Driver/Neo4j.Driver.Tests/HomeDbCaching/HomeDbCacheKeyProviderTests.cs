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

using FluentAssertions;
using Neo4j.Driver.Internal.HomeDbCaching;
using Xunit;

namespace Neo4j.Driver.Tests.HomeDbCaching;

public class HomeDbCacheKeyProviderTests
{
    [Fact]
    public void ShouldTreatAllDriverLevelAuthTokensAsEqual()
    {
        var aliceKey = HomeDbCacheKeyProvider.GetCacheKey(AuthTokens.Basic("alice", "alicepw"), null);
        var bobKey = HomeDbCacheKeyProvider.GetCacheKey(AuthTokens.Basic("bob", "bobpw"), null);

        aliceKey.Should().Be(HomeDbCacheKey.Default);
        bobKey.Should().Be(HomeDbCacheKey.Default);
    }

    [Fact]
    public void ShouldPartitionBySessionLevelAuthToken()
    {
        var aliceConfig = SessionConfig.Builder.WithAuthToken(AuthTokens.Basic("alice", "alicepw")).Build();
        var bobConfig = SessionConfig.Builder.WithAuthToken(AuthTokens.Basic("bob", "bobpw")).Build();

        var aliceKey = HomeDbCacheKeyProvider.GetCacheKey(null, aliceConfig);
        var bobKey = HomeDbCacheKeyProvider.GetCacheKey(null, bobConfig);

        aliceKey.Should().NotBe(bobKey);
        aliceKey.Should().NotBe(HomeDbCacheKey.Default);
    }

    [Fact]
    public void ShouldPartitionByImpersonatedUser()
    {
        var aliceConfig = SessionConfig.Builder.WithImpersonatedUser("alice").Build();
        var bobConfig = SessionConfig.Builder.WithImpersonatedUser("bob").Build();

        var aliceKey = HomeDbCacheKeyProvider.GetCacheKey(null, aliceConfig);
        var bobKey = HomeDbCacheKeyProvider.GetCacheKey(null, bobConfig);

        aliceKey.Should().NotBe(bobKey);
        aliceKey.Should().NotBe(HomeDbCacheKey.Default);
    }

    [Fact]
    public void ShouldPreferSessionLevelAuthTokenOverDriverLevelAuthToken()
    {
        var sessionConfig = SessionConfig.Builder.WithAuthToken(AuthTokens.Basic("alice", "alicepw")).Build();

        var withDriverToken = HomeDbCacheKeyProvider.GetCacheKey(AuthTokens.Basic("bob", "bobpw"), sessionConfig);
        var withoutDriverToken = HomeDbCacheKeyProvider.GetCacheKey(null, sessionConfig);

        withDriverToken.Should().Be(withoutDriverToken);
    }
}
