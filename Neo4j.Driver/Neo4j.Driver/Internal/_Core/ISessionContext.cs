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

using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

/// <summary>
/// Provides session-scoped state and services to protocol handlers.
/// Not protocol-specific — intended to be used by both Query API and Bolt stacks.
/// </summary>
internal interface ISessionContext
{
    string Database { get; }
    string? ImpersonatedUser { get; }
    AccessMode AccessMode { get; }

    /// <summary>
    /// Returns a valid auth token for the current session. The implementation handles
    /// expiry, refresh, and per-session overrides transparently.
    /// </summary>
    ValueTask<IAuthToken> GetAuthTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies the auth provider that a security exception was thrown using the given token.
    /// Returns <c>true</c> if the token has been refreshed and the request should be retried.
    /// </summary>
    ValueTask<bool> HandleSecurityExceptionAsync(
        IAuthToken token,
        SecurityException exception,
        CancellationToken cancellationToken = default);
}
