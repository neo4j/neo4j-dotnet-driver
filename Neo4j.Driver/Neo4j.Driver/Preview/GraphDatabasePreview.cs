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
using Neo4j.Driver.Auth;

namespace Neo4j.Driver.Preview;

/// <summary>
/// Contains preview methods for <see cref="GraphDatabase"/>. These methods are subject to change or removal in
/// future versions.
/// </summary>
public class GraphDatabasePreview
{
    /// <summary>
    /// Returns a new <see cref="IDriver"/> instance connected to the provided <paramref name="uri"/>.
    /// </summary>
    /// <param name="uri">The URI to connect to.</param>
    /// <param name="authTokenManager">The <see cref="IAuthTokenManager"/> to use for authentication.</param>
    /// <returns>A new <see cref="IDriver"/> instance.</returns>
    public static IDriver Driver(string uri, IAuthTokenManager authTokenManager)
    {
        return GraphDatabase.Driver(uri, authTokenManager);
    }

    /// <summary>
    /// Returns a new <see cref="IDriver"/> instance connected to the provided <paramref name="uri"/>.
    /// </summary>
    /// <param name="uri">The URI to connect to.</param>
    /// <param name="authTokenManager">The <see cref="IAuthTokenManager"/> to use for authentication.</param>
    /// <param name="action">An action to configure the <see cref="ConfigBuilder"/>.</param>
    /// <returns>A new <see cref="IDriver"/> instance.</returns>
    public static IDriver Driver(string uri, IAuthTokenManager authTokenManager, Action<ConfigBuilder> action)
    {
        return GraphDatabase.Driver(uri, authTokenManager, action);
    }

    /// <summary>
    /// Returns a new <see cref="IDriver"/> instance connected to the provided <paramref name="uri"/>.
    /// </summary>
    /// <param name="uri">The URI to connect to.</param>
    /// <param name="authTokenManager">The <see cref="IAuthTokenManager"/> to use for authentication.</param>
    /// <param name="action">An action to configure the <see cref="ConfigBuilder"/>.</param>
    /// <returns>A new <see cref="IDriver"/> instance.</returns>
    public static IDriver Driver(Uri uri, IAuthTokenManager authTokenManager, Action<ConfigBuilder> action)
    {
        return GraphDatabase.Driver(uri, authTokenManager, action);
    }
}
