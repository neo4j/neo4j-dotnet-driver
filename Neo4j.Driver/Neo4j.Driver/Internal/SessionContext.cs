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

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

/// <summary>
/// Carries session-scoped state and services. Registered at session scope in the resolver so
/// that handlers receive it via constructor injection rather than per-call parameters.
/// General-purpose — not protocol-specific — so the same type works for Query API and Bolt.
/// </summary>
internal class SessionContext : ISessionContext
{
    private readonly Func<CancellationToken, ValueTask<IAuthToken>> _getAuthToken;
    private readonly Func<IAuthToken, SecurityException, CancellationToken, ValueTask<bool>> _handleSecurityException;

    /// <param name="database">The target database for this session.</param>
    /// <param name="getAuthToken">
    /// Delegate to retrieve a valid token. Pass <c>authTokenManager.GetTokenAsync</c> for the
    /// default case, or <c>_ => ValueTask.FromResult(overrideToken)</c> for a per-session override.
    /// </param>
    /// <param name="handleSecurityException">
    /// Delegate to notify the auth provider of a security failure. Pass
    /// <c>authTokenManager.HandleSecurityExceptionAsync</c> for the default case, or
    /// <c>(_, _, _) => ValueTask.FromResult(false)</c> for a static override (no refresh possible).
    /// </param>
    public SessionContext(
        string database,
        Func<CancellationToken, ValueTask<IAuthToken>> getAuthToken,
        Func<IAuthToken, SecurityException, CancellationToken, ValueTask<bool>> handleSecurityException)
    {
        Database = database;
        _getAuthToken = getAuthToken;
        _handleSecurityException = handleSecurityException;
    }

    public string Database { get; }

    public ValueTask<IAuthToken> GetAuthTokenAsync(CancellationToken cancellationToken = default)
    {
        return _getAuthToken(cancellationToken);
    }

    public ValueTask<bool> HandleSecurityExceptionAsync(
        IAuthToken token,
        SecurityException exception,
        CancellationToken cancellationToken = default)
    {
        return _handleSecurityException(token, exception, cancellationToken);
    }
}
