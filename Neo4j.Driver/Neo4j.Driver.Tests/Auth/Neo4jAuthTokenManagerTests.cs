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
using Moq;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.Services;
using Xunit;

namespace Neo4j.Driver.Tests.Auth;

public class Neo4jAuthTokenManagerTests
{
    [Fact]
    public async Task ShouldRequestToken()
    {
        var basicToken = AuthTokens.Basic("uid", "pwd");
        var authData = new AuthTokenAndExpiration(basicToken, new DateTime(2023, 02, 28));
        var subject = new Neo4jAuthTokenManager(() => ValueTask.FromResult(authData));

        var returnedToken = await subject.GetTokenAsync();

        returnedToken.Should().Be(basicToken);
    }

    [Fact]
    public async Task ShouldCacheToken()
    {
        var (authData, _) = GetTwoAuthTokens();

        var callCount = 0;

        ValueTask<AuthTokenAndExpiration> TokenProvider()
        {
            callCount++;
            return ValueTask.FromResult(authData);
        }

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.Now())
            .Returns(new DateTime(2023, 02, 28, 10, 0, 0)); // it is currently 10am

        var subject = new Neo4jAuthTokenManager(dateTimeProvider.Object, TokenProvider);

        // call twice
        var _ = await subject.GetTokenAsync();
        var returnedToken = await subject.GetTokenAsync();

        returnedToken.Should().Be(authData.Token);
        callCount.Should().Be(1); // only called once
    }

    [Fact]
    public async Task ShouldRenewTokenAfterExpiryTime()
    {
        var (firstAuthData, secondAuthData) = GetTwoAuthTokens();
        var tokenProvider = SequenceTokenProvider(firstAuthData, secondAuthData);

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider
            .SetupSequence(x => x.Now())
            .Returns(new DateTime(2023, 02, 28, 10, 0, 0)) // first time, 10am
            .Returns(new DateTime(2023, 02, 28, 16, 0, 0)); // after that, 4pm (expired)

        var subject = new Neo4jAuthTokenManager(dateTimeProvider.Object, tokenProvider);

        var firstReturnedToken = await subject.GetTokenAsync();
        await subject.GetTokenAsync(); // do a couple more times so that a new one should be requested
        await subject.GetTokenAsync();
        var secondReturnedToken = await subject.GetTokenAsync();

        firstReturnedToken.Should().Be(firstAuthData.Token);
        secondReturnedToken.Should().Be(secondAuthData.Token);
    }

    [Fact]
    public async Task ShouldRefreshTokenOnExpiry()
    {
        var (firstAuthData, secondAuthData) = GetTwoAuthTokens();
        var tokenProvider = SequenceTokenProvider(firstAuthData, secondAuthData);

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider
            .Setup(x => x.Now())
            .Returns(new DateTime(2023, 01, 01, 09, 00, 00)); // before expiry of first token

        var subject = new Neo4jAuthTokenManager(
            dateTimeProvider.Object,
            tokenProvider,
            typeof(TokenExpiredException));

        var firstReturnedToken = await subject.GetTokenAsync();
        await subject.HandleSecurityExceptionAsync(firstReturnedToken, new TokenExpiredException("token expired"));
        var secondReturnedToken = await subject.GetTokenAsync();

        secondReturnedToken.Should().Be(secondAuthData.Token);
    }

    [Fact]
    public async Task ShouldIgnoreUnhandledSecurityExceptions()
    {
        var (firstAuthData, secondAuthData) = GetTwoAuthTokens();
        var tokenProvider = SequenceTokenProvider(firstAuthData, secondAuthData);

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider
            .Setup(x => x.Now())
            .Returns(new DateTime(2023, 01, 01, 09, 00, 00)); // before expiry of first token

        var subject = new Neo4jAuthTokenManager(
            dateTimeProvider.Object,
            tokenProvider,
            typeof(AuthenticationException));

        var firstReturnedToken = await subject.GetTokenAsync();

        // ask to handle an exception that is not in the list of exceptions the token manager should handle
        await subject.HandleSecurityExceptionAsync(firstReturnedToken, new TokenExpiredException("token expired"));

        var secondReturnedToken = await subject.GetTokenAsync();
        secondReturnedToken.Should().Be(firstAuthData.Token); // should not have changed
    }

    private static (AuthTokenAndExpiration, AuthTokenAndExpiration) GetTwoAuthTokens()
    {
        var firstToken = AuthTokens.Basic("first", "token");
        var firstAuthData = new AuthTokenAndExpiration(
            firstToken,
            new DateTime(2023, 02, 28, 15, 0, 0)); // expires at 3pm

        var secondToken = AuthTokens.Basic("second", "token");
        var secondAuthData = new AuthTokenAndExpiration(
            secondToken,
            new DateTime(2023, 02, 28, 16, 0, 0));

        return (firstAuthData, secondAuthData);
    }

    private Func<ValueTask<AuthTokenAndExpiration>> SequenceTokenProvider(params AuthTokenAndExpiration[] authData)
    {
        var index = 0;
        return () => ValueTask.FromResult(authData[index++] ?? throw new InvalidOperationException());
    }
}
