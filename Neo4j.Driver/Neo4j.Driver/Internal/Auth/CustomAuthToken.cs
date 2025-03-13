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

using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal.Auth;

internal class CustomAuthToken : AuthToken
{
    public CustomAuthToken(
        string principal,
        string credentials,
        string realm,
        string scheme,
        Dictionary<string, object> parameters = null)
    {
        if (principal != null)
        {
            Content[PrincipalKey] = principal;
        }

        if (!string.IsNullOrEmpty(scheme))
        {
            Content[SchemeKey] = scheme;
        }

        if (!string.IsNullOrEmpty(credentials))
        {
            Content[CredentialsKey] = credentials;
        }

        if (!string.IsNullOrEmpty(realm))
        {
            Content[RealmKey] = realm;
        }

        if (parameters != null)
        {
            Content[ParametersKey] = parameters;
        }
    }

    public override string ToString()
    {
        var scheme = Content.ContainsKey(SchemeKey) ? Content[SchemeKey] ?? "(null)" : "(none)";
        var principal = Content.ContainsKey(PrincipalKey) ? Content[PrincipalKey] ?? "(null)" : "(none)";
        var realm = Content.ContainsKey(RealmKey) ? Content[RealmKey] ?? "(null)" : "(none)";
        return $"CustomAuthToken[scheme: {scheme}, principal: {principal}, realm: {realm}" +
            // list of other keys present in Content
            Content.Keys
                .Where(key => key != SchemeKey && key != PrincipalKey && key != RealmKey)
                .Select(key => $", {key}: {Content[key] ?? "(null)"}")
                .Aggregate("", (acc, next) => acc + next) +
            "]";
    }
}
