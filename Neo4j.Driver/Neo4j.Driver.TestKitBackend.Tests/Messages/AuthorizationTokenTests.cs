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

using System.Text.Json;
using FluentAssertions;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class AuthorizationTokenTests
{
    private static readonly JsonSerializerOptions Options =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void A_null_principal_or_credentials_is_omitted_from_the_wire_rather_than_written_as_null()
    {
        var token = new AuthorizationToken { Scheme = "bearer", Principal = null, Credentials = "token-value" };

        var json = JsonSerializer.Serialize(token, Options);

        json.Should().NotContain("principal");
        json.Should().Contain("\"credentials\":\"token-value\"");
    }

    [Fact]
    public void Basic_scheme_maps_to_a_basic_auth_token()
    {
        var token = new AuthorizationToken
        {
            Scheme = "basic",
            Principal = "neo4j",
            Credentials = "secret",
            Realm = "myrealm"
        };
        token.ToAuthToken().Should().Be(AuthTokens.Basic("neo4j", "secret", "myrealm"));
    }

    [Fact]
    public void Kerberos_scheme_maps_to_a_dedicated_kerberos_auth_token()
    {
        var token = new AuthorizationToken { Scheme = "kerberos", Principal = "", Credentials = "a-ticket" };
        token.ToAuthToken().Should().Be(AuthTokens.Kerberos("a-ticket"));
    }

    [Fact]
    public void Custom_scheme_without_parameters_omits_the_parameters_key()
    {
        var token = new AuthorizationToken
        {
            Scheme = "wild-scheme",
            Principal = "principal",
            Credentials = "credentials",
            Realm = "realm"
        };

        token.ToAuthToken().Should().Be(AuthTokens.Custom("principal", "credentials", "realm", "wild-scheme"));
    }

    [Fact]
    public void Custom_scheme_with_parameters_passes_them_through_untouched()
    {
        var token = new AuthorizationToken
        {
            Scheme = "wild-scheme",
            Principal = "principal",
            Credentials = "credentials",
            Realm = "realm",
            Parameters = new Dictionary<string, object> { ["sky?"] = "no" }
        };

        var authToken = (AuthToken)token.ToAuthToken();

        authToken.Content["parameters"].Should().BeEquivalentTo(new Dictionary<string, object> { ["sky?"] = "no" });
    }
}
