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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal.Auth;

/// <summary>A simple common token for authentication schemes that easily convert to an auth token map</summary>
internal sealed class AuthToken : IAuthToken
{
    public const string SchemeKey = "scheme";
    public const string PrincipalKey = "principal";
    public const string CredentialsKey = "credentials";
    public const string RealmKey = "realm";
    public const string ParametersKey = "parameters";

    public AuthToken(IDictionary<string, object> content)
    {
        content = content ?? throw new ArgumentNullException(nameof(content));
        Content = new Dictionary<string, object>();
        foreach (var (key, value) in content)
        {
            if (value is not null)
            {
                Content[key] = value;
            }
        }
    }

    public IDictionary<string, object> Content { get; }

    public override bool Equals(object obj)
    {
        return obj is AuthToken a && Equals(a);
    }

    private bool Equals(AuthToken other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        var equal =
            Content.Count == other.Content.Count &&
            Content.All(kvp => other.Content[kvp.Key].Equals(kvp.Value));

        return equal;
    }

    public override int GetHashCode()
    {
        return Content != null ? Content.GetHashCode() : 0;
    }
}

internal static class AuthTokenExtensions
{
    public static IDictionary<string, object> AsDictionary(this IAuthToken authToken)
    {
        if (authToken is not AuthToken token)
        {
            throw new ClientException(
                $"Unknown authentication token, `{authToken}`. Please use one of the supported " +
                $"tokens from `{nameof(AuthTokens)}`.");
        }

        return token.Content
            .Where(kvp => kvp.Value is not null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
