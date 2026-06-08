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

#nullable enable

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiAuthEnricherTests
{
    private readonly AutoMocker _autoMocker = new();

    public QueryApiAuthEnricherTests()
    {

    }

    [Fact]
    public async Task Enrich_SetsBasicAuthorizationHeader_UsingEncoder()
    {
        var request = new HttpRequestMessage();

        _autoMocker.GetMock<IAuthTokenManager>()
            .Setup(m => m.GetTokenAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(AuthTokens.Basic("alice", "s3cret"));

        _autoMocker.GetMock<IBase64Encoder>()
            .Setup(e => e.Encode("alice:s3cret"))
            .Returns("encoded");

        var subject = _autoMocker.CreateInstance<QueryApiAuthEnricher>();
        await subject.Enrich(request, TestContext.Current.CancellationToken);

        request.Headers.Authorization!.Scheme.Should().Be("Basic");
        request.Headers.Authorization.Parameter.Should().Be("encoded");
    }

    [Fact]
    public async Task Enrich_SetsBearerAuthorizationHeader_UsingEncoder()
    {
        var request = new HttpRequestMessage();

        _autoMocker.GetMock<IAuthTokenManager>()
            .Setup(m => m.GetTokenAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(AuthTokens.Bearer("my-jwt-token"));

        _autoMocker.GetMock<IBase64Encoder>()
            .Setup(e => e.Encode("my-jwt-token"))
            .Returns("encoded");

        var subject = _autoMocker.CreateInstance<QueryApiAuthEnricher>();
        await subject.Enrich(request, TestContext.Current.CancellationToken);

        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("encoded");
    }

    [Fact]
    public async Task Enrich_SetsNullAuthorizationHeader_WhenSchemeIsNone()
    {
        var request = new HttpRequestMessage();

        _autoMocker.GetMock<IAuthTokenManager>()
            .Setup(m => m.GetTokenAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(AuthTokens.None);

        var subject = _autoMocker.CreateInstance<QueryApiAuthEnricher>();

        await subject.Enrich(request, TestContext.Current.CancellationToken);

        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Enrich_Throws_WhenSchemeIsNotSupportedByQueryApi()
    {
        var request = new HttpRequestMessage();

        _autoMocker.GetMock<IAuthTokenManager>()
            .Setup(m => m.GetTokenAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(AuthTokens.Kerberos("ticket"));

        var subject = _autoMocker.CreateInstance<QueryApiAuthEnricher>();

        var act = async () => await subject.Enrich(request, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotSupportedException>();
    }
}
