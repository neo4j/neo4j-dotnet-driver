// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
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

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiSessionContextTests
{
    private readonly AutoMocker _mocker = new();

    private QueryApiSessionContext CreateContext(SessionConfig? config = null)
    {
        if (config is not null)
        {
            _mocker.Use(config);
        }

        return _mocker.CreateInstance<QueryApiSessionContext>();
    }

    [Fact]
    public void Database_ReturnsSessionConfigDatabase()
    {
        var config = SessionConfig.Builder.WithDatabase("mydb").Build();
        CreateContext(config).Database.Should().Be("mydb");
    }

    [Fact]
    public void Database_DefaultsToNeo4j_WhenConfigDatabaseIsNull()
    {
        CreateContext(SessionConfig.Builder.Build()).Database.Should().Be("neo4j");
    }

    [Fact]
    public async Task GetAuthTokenAsync_ReturnsSessionToken_WhenSessionOverrideIsSet()
    {
        var sessionToken = AuthTokens.Basic("user", "pass");
        var config = SessionConfig.Builder.WithAuthToken(sessionToken).Build();

        var token = await CreateContext(config).GetAuthTokenAsync();

        token.Should().BeSameAs(sessionToken);
        _mocker.GetMock<IAuthTokenManager>().Verify(m => m.GetTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAuthTokenAsync_DelegatesToManager_WhenNoSessionOverride()
    {
        var driverToken = AuthTokens.Basic("driver", "secret");
        _mocker.GetMock<IAuthTokenManager>()
            .Setup(m => m.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(driverToken);

        var token = await CreateContext(SessionConfig.Builder.Build()).GetAuthTokenAsync();

        token.Should().BeSameAs(driverToken);
    }

    [Fact]
    public async Task HandleSecurityExceptionAsync_ReturnsFalse_WhenSessionOverrideIsSet()
    {
        var sessionToken = AuthTokens.Basic("user", "pass");
        var config = SessionConfig.Builder.WithAuthToken(sessionToken).Build();

        var result = await CreateContext(config).HandleSecurityExceptionAsync(
            sessionToken,
            new AuthenticationException("401"));

        result.Should().BeFalse();
        _mocker.GetMock<IAuthTokenManager>().Verify(
            m => m.HandleSecurityExceptionAsync(It.IsAny<IAuthToken>(), It.IsAny<SecurityException>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleSecurityExceptionAsync_DelegatesToManager_WhenNoSessionOverride()
    {
        var driverToken = AuthTokens.Basic("driver", "secret");
        var exception = new AuthenticationException("401");

        _mocker.GetMock<IAuthTokenManager>()
            .Setup(m => m.HandleSecurityExceptionAsync(driverToken, exception, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateContext(SessionConfig.Builder.Build())
            .HandleSecurityExceptionAsync(driverToken, exception);

        result.Should().BeTrue();
    }
}
