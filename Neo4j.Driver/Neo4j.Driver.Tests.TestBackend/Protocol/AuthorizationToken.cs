// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class AuthorizationToken : IProtocolObject
{
    public AuthorizationTokenType data { get; set; } = new();

    public override Task Process()
    {
        return Task.CompletedTask;
    }

    public class AuthorizationTokenType
    {
        public string scheme { get; set; }
        public string principal { get; set; }
        public string credentials { get; set; }
        public string realm { get; set; }
        public Dictionary<string, object> parameters { get; set; }
    }

    public IAuthToken AsToken()
    {
        var authTokenData = data;
        return authTokenData.scheme switch
        {
            AuthSchemes.Bearer => AuthTokens.Bearer(authTokenData.credentials),
            AuthSchemes.Kerberos => AuthTokens.Kerberos(authTokenData.credentials),
            _ => AuthTokens.Custom(
                authTokenData.principal,
                authTokenData.credentials,
                authTokenData.realm,
                authTokenData.scheme,
                authTokenData.parameters)
        };
    }
}
