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

using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
/// Common interface for components that can provide auth tokens. For pre-baked implementations of this interface,
/// see <see cref="AuthTokenManagers"/>.
/// </summary>
/// <remarks>
/// An implementation is expected to supply credentials for a <b>single identity</b>, renewing that identity's
/// credentials as they expire. It must not be used to switch between different users: the driver caches per-identity
/// state, such as the home database resolved for a session that did not name one, without regard to the token
/// returned here. Rotating identities through this interface can therefore route one user's session to another
/// user's home database. To connect as more than one user from a single driver, give each session its own
/// credentials via <see cref="SessionConfigBuilder.WithAuthToken"/>, or use
/// <see cref="SessionConfigBuilder.WithImpersonatedUser"/>.
/// </remarks>
public interface IAuthTokenManager
{
    /// <summary>
    /// Asynchronously retrieves a valid token. This method will be called often; the implementer should cache the
    /// token until a new one is required.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    ValueTask<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles an error notification thrown by the server if a security error happened. <p/> This will be called when
    /// driver throws a <see cref="SecurityException"/>.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="exception">The security exception thrown by the server.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    ValueTask<bool> HandleSecurityExceptionAsync(
        IAuthToken token,
        SecurityException exception,
        CancellationToken cancellationToken = default);
}
