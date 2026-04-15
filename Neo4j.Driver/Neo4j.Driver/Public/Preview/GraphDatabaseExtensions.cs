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

namespace Neo4j.Driver.Preview;

/// <summary>
/// Preview API extensions for <see cref="GraphDatabase"/>.
/// Add <c>using Neo4j.Driver.Preview;</c> to access these members.
/// </summary>
/// <remarks>
/// APIs in this namespace are preview features. They may change or be removed in a future release.
/// Once stabilised they will be promoted to the main <c>Neo4j.Driver</c> namespace.
/// </remarks>
public static class GraphDatabaseExtensions
{
    extension(GraphDatabase)
    {
        /// <summary>
        /// Returns a driver configured with multiple initial addresses for higher resilience during discovery.
        /// </summary>
        /// <param name="multiAddress">
        /// The <see cref="MultiAddress"/> specifying the scheme, optional routing context, and the list of
        /// initial addresses to try.
        /// </param>
        /// <param name="authToken">Authentication to use, <see cref="AuthTokens"/>.</param>
        /// <returns>A new <see cref="IDriver"/> instance.</returns>
        public static IDriver Driver(MultiAddress multiAddress, IAuthToken authToken)
        {
            return GraphDatabase.Driver(multiAddress, authToken, null);
        }

        /// <summary>
        /// Returns a driver configured with multiple initial addresses for higher resilience during discovery.
        /// </summary>
        /// <param name="multiAddress">
        /// The <see cref="MultiAddress"/> specifying the scheme, optional routing context, and the list of
        /// initial addresses to try.
        /// </param>
        /// <param name="authToken">Authentication to use, <see cref="AuthTokens"/>.</param>
        /// <param name="action">
        /// Specifies how to build a driver configuration <see cref="Config"/> using <see cref="ConfigBuilder"/>.
        /// If set to <c>null</c>, default configuration is used.
        /// </param>
        /// <returns>A new <see cref="IDriver"/> instance.</returns>
        public static IDriver Driver(MultiAddress multiAddress, IAuthToken authToken, Action<ConfigBuilder> action)
        {
            return GraphDatabase.CreateMultiAddressDriver(
                multiAddress,
                AuthTokenManagers.Static(authToken),
                action);
        }

        /// <summary>
        /// Returns a driver configured with multiple initial addresses for higher resilience during discovery.
        /// </summary>
        /// <param name="multiAddress">
        /// The <see cref="MultiAddress"/> specifying the scheme, optional routing context, and the list of
        /// initial addresses to try.
        /// </param>
        /// <param name="authTokenManager">The <see cref="IAuthTokenManager"/> to use for authentication.</param>
        /// <param name="action">
        /// Specifies how to build a driver configuration <see cref="Config"/> using <see cref="ConfigBuilder"/>.
        /// If set to <c>null</c>, default configuration is used.
        /// </param>
        /// <returns>A new <see cref="IDriver"/> instance.</returns>
        public static IDriver Driver(
            MultiAddress multiAddress,
            IAuthTokenManager authTokenManager,
            Action<ConfigBuilder> action)
        {
            return GraphDatabase.CreateMultiAddressDriver(multiAddress, authTokenManager, action);
        }
    }
}
