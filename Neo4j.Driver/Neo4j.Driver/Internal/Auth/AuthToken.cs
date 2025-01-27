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
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal.Auth;

/// <summary>A simple common token for authentication schemes that easily convert to an auth token map</summary>
internal class AuthToken : IAuthToken
{
    public const string SchemeKey = "scheme";
    public const string PrincipalKey = "principal";
    public const string CredentialsKey = "credentials";
    public const string RealmKey = "realm";
    public const string ParametersKey = "parameters";

    internal AuthToken(IDictionary<string, object> content)
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

    protected AuthToken() : this(new Dictionary<string, object>())
    {
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

internal abstract class FullTokenCacheKeyAuthToken : AuthToken
{
    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // combine hash codes of all key-value pairs in the dictionary. We sort the dictionary by key to ensure
        // that the hash code is the same for all dictionaries that have the same key-value pairs but in different
        // order.

        var hash = 17;
        foreach (var kvp in Content.OrderBy(kvp => kvp.Key))
        {
            hash = hash * 31 + (kvp.Key?.GetHashCode() ?? 0);
            hash = hash * 31 + (kvp.Value?.GetHashCode() ?? 0);
        }

        return hash;
    }
}
