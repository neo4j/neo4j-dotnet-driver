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
using System.Text;
using FluentAssertions;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>Spec: https://neo4j.com/docs/query-api/current/#_authentication</summary>
public class QueryApiAuthApplicatorTests
{
    private static QueryApiAuthApplicator Applicator => new();

    [Fact]
    public void Apply_SetsBasicAuthorizationHeader_WithBase64EncodedCredentials()
    {
        // Basic auth: Authorization: Basic base64(username:password)
        var request = new HttpRequestMessage();
        var token = AuthTokens.Basic("alice", "s3cret");

        Applicator.Apply(request, token);

        request.Headers.Authorization!.Scheme.Should().Be("Basic");
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!));
        decoded.Should().Be("alice:s3cret");
    }

    [Fact]
    public void Apply_SetsBearerAuthorizationHeader_WithCredentialsAsToken()
    {
        // Bearer auth: Authorization: Bearer <token>
        var request = new HttpRequestMessage();
        var token = AuthTokens.Bearer("my-jwt-token");

        Applicator.Apply(request, token);

        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("my-jwt-token");
    }

    [Fact]
    public void Apply_SetsNullAuthorizationHeader_WhenSchemeIsNone()
    {
        // No auth: Authorization header must be absent
        var request = new HttpRequestMessage();
        var token = AuthTokens.None;

        Applicator.Apply(request, token);

        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public void Apply_Throws_WhenSchemeIsNotSupportedByQueryApi()
    {
        var request = new HttpRequestMessage();
        var kerberosToken = AuthTokens.Kerberos("ticket");

        var act = () => Applicator.Apply(request, kerberosToken);

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("*kerberos*");
    }
}
