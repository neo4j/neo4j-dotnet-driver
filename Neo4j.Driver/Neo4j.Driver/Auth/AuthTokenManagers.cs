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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Auth;

namespace Neo4j.Driver.Auth;

/// <summary>
/// This class provides common implementations of <see cref="IAuthTokenManager"/> without needing
/// to create a new class that implements that interface.
/// </summary>
public static class AuthTokenManagers
{
    /// <summary>
    /// An implementation of <see cref="IAuthTokenManager"/> that allows connection with auth disabled. This will only
    /// work if authentication is disabled on the Neo4j Instance we are connecting to.
    /// </summary>
    public static IAuthTokenManager None => Static(AuthTokens.None);

    /// <summary>
    /// An implementation of <see cref="IAuthTokenManager"/> that allows connection using a static token
    /// that never changes.
    /// </summary>
    /// <param name="token">The token that will be used to connect.</param>
    public static IAuthTokenManager Static(IAuthToken token) => new StaticAuthTokenManager(token);

    /// <summary>
    /// An implementation of <see cref="IAuthTokenManager"/> that will call the provided async function
    /// whenever a new token is required. It will handle caching of the token and will only call the
    /// function when a new token is needed or the existing cached token has expired.
    /// </summary>
    /// <param name="tokenProviderAsync">An async function that will obtain a new auth token and expiry time.</param>
    /// <returns></returns>
    public static IAuthTokenManager ExpirationBased(Func<Task<AuthTokenAndExpiration>> tokenProviderAsync)
    {
        return new ExpirationBasedAuthTokenManager(tokenProviderAsync);
    }
}
