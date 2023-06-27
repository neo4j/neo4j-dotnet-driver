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
using Neo4j.Driver.Internal.Services;

namespace Neo4j.Driver.Auth;

/// <summary>
/// Represents an auth token and its expiration.
/// </summary>
public record AuthTokenAndExpiration
{
    /// <summary>
    /// Initializes a new instance of <see cref="AuthTokenAndExpiration"/>.
    /// </summary>
    /// <param name="token">The auth token.</param>
    /// <param name="expiry">The date and time when the token expires.</param>
    public AuthTokenAndExpiration(IAuthToken token, DateTime? expiry = default)
    {
        Token = token;
        Expiry = expiry ?? DateTime.MaxValue;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AuthTokenAndExpiration"/>.
    /// </summary>
    /// <param name="token">The auth token.</param>
    /// <param name="expiresInMs">The number of milliseconds after which the token expires.</param>
    public AuthTokenAndExpiration(IAuthToken token, int expiresInMs)
    {
        this.Token = token;
        this.Expiry = DateTimeProvider.StaticInstance.Now().AddMilliseconds(expiresInMs);
    }

    /// <summary>The auth token.</summary>
    public IAuthToken Token { get; init; }

    /// <summary>The date and time when the token expires.</summary>
    public DateTime Expiry { get; init; }

    /// <summary>
    /// Deconstructs the <see cref="AuthTokenAndExpiration"/> into its components.
    /// </summary>
    /// <param name="Token">The auth token.</param>
    /// <param name="Expiry">The date and time when the token expires.</param>
    public void Deconstruct(out IAuthToken Token, out DateTime? Expiry)
    {
        Token = this.Token;
        Expiry = this.Expiry;
    }
}
