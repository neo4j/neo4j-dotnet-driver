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

using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Preview.Auth;

/// <summary>Common interface for components that can provide auth tokens.</summary>
public interface IAuthTokenManager
{
    /// <summary>
    /// Asynchronously retrieves a valid token. This method will be called often; the implementer should cache the
    /// token until a new one is required.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles an error notification thrown by the server if a security error happened.
    /// <p/>
    /// This will be called when driver throws a <see cref="SecurityException"/>.
    /// </summary>
    /// <returns>
    /// <param name="token">The token.</param>
    /// <param name="exception">The security exception thrown by the server.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task<bool> HandleSecurityExceptionAsync(
        IAuthToken token,
        SecurityException exception,
        CancellationToken cancellationToken = default);
}
