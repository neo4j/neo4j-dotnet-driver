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

using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class VerifyAuthentication : IProtocolObject
{
    public VerifyAuthenticationDTO data { get; set; } = null!;
    [JsonIgnore]
    public bool Authenticated { get; set; }

    public override async Task Process()
    {
        var driver = ObjManager.GetObject<NewDriver>(data.driverId).Driver;
        Authenticated = await driver.VerifyAuthenticationAsync(data.authorizationToken.AsToken());
    }

    public override string Respond()
    {
        return new ProtocolResponse("DriverIsAuthenticated", new { id = uniqueId, authenticated = Authenticated })
            .Encode();
    }
}

internal class VerifyAuthenticationDTO
{
    public string driverId { get; set; }
    public AuthorizationToken authorizationToken { get; set; }
}
