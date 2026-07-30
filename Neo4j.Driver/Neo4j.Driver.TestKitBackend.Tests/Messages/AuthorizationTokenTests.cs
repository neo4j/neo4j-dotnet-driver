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
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class AuthorizationTokenTests
{
    [Fact]
    public void Basic_scheme_maps_to_a_basic_auth_token()
    {
        var token = new AuthorizationToken("basic", "neo4j", "secret", "myrealm");
        token.ToAuthToken().Should().Be(AuthTokens.Basic("neo4j", "secret", "myrealm"));
    }
}
