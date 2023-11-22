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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.Services;

namespace Neo4j.Driver.Auth;

/// <summary>
/// This class provides common implementations of <see cref="IAuthTokenManager"/> for various types of
/// authentication.
/// </summary>
public static class AuthTokenManagers
{
    /// <summary>
    /// An implementation of <see cref="IAuthTokenManager"/> that allows connection with auth disabled. This will only
    /// work if authentication is disabled on the Neo4j Instance we are connecting to.
    /// </summary>
    public static IAuthTokenManager None { get; } = Static(AuthTokens.None);

    /// <summary>
    /// An implementation of <see cref="IAuthTokenManager"/> that allows connection using a static token that never
    /// changes.
    /// </summary>
    /// <param name="token">The token that will be used to connect.</param>
    public static IAuthTokenManager Static(IAuthToken token)
    {
        return new StaticAuthTokenManager(token);
    }

    /// <summary>
    /// An implementation of <see cref="IAuthTokenManager"/> that will call the provided async function whenever a new
    /// token is required. It will handle caching of the token and will only call the function when a new token is needed.
    /// </summary>
    /// <param name="tokenProviderAsync">A function that will be called when a new token is needed.</param>
    /// <returns>The <see cref="IAuthTokenManager"/> that will call the provided function when a new token is needed.</returns>
    public static IAuthTokenManager Basic(Func<ValueTask<IAuthToken>> tokenProviderAsync)
    {
        return Basic(DateTimeProvider.Instance, tokenProviderAsync);
    }

    internal static IAuthTokenManager Basic(
        IDateTimeProvider dateTimeProvider,
        Func<ValueTask<IAuthToken>> tokenProviderAsync)
    {
        async ValueTask<AuthTokenAndExpiration> TokenProviderAsync()
        {
            var authToken = await tokenProviderAsync().ConfigureAwait(false);
            return new AuthTokenAndExpiration(authToken, DateTime.MaxValue);
        }

        return new Neo4jAuthTokenManager(dateTimeProvider, TokenProviderAsync, typeof(AuthenticationException));
    }

    /// <summary>
    /// An implementation of <see cref="IAuthTokenManager"/> that will call the provided async function whenever
    /// a token is needed. It will cache the token and will only call the function when a new token is needed or
    /// the existing cached token has expired.
    /// </summary>
    /// <param name="tokenProviderAsync">A function that will be called when a new token is needed.</param>
    /// <returns>The <see cref="IAuthTokenManager"/> that will call the provided function when a new token is needed.</returns>
    public static IAuthTokenManager Bearer(Func<ValueTask<AuthTokenAndExpiration>> tokenProviderAsync)
    {
        return Bearer(DateTimeProvider.Instance, tokenProviderAsync);
    }

    internal static IAuthTokenManager Bearer(
        IDateTimeProvider dateTimeProvider,
        Func<ValueTask<AuthTokenAndExpiration>> tokenProviderAsync)
    {
        return new Neo4jAuthTokenManager(
            dateTimeProvider,
            tokenProviderAsync,
            typeof(TokenExpiredException),
            typeof(AuthenticationException));
    }
}
