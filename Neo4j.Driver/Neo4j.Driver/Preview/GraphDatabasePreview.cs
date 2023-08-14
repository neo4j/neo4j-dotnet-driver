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
using Neo4j.Driver.Preview.Auth;

namespace Neo4j.Driver.Preview;

/// <summary>
/// Contains preview methods for <see cref="GraphDatabase"/>. These methods are subject to change or removal in
/// future versions.
/// </summary>
public class GraphDatabasePreview
{
    /// <summary>Returns a driver for a Neo4j instance with default configuration settings.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
    /// be used. The supported protocols in URI could either be <c>bolt</c> or <c>neo4j</c>. The protocol <c>bolt</c> should be
    /// used when creating a driver connecting to the Neo4j instance directly. The protocol <c>neo4j</c> should be used when
    /// creating a driver with built-in routing.
    /// </param>
    /// <param name="authTokenManager">The <see cref="IAuthTokenManager"/> to use for authentication.</param>
    /// <returns>A new <see cref="IDriver"/> instance.</returns>
    public static IDriver Driver(string uri, IAuthTokenManager authTokenManager)
    {
        return GraphDatabase.Driver(uri, authTokenManager);
    }

    /// <summary>Returns a driver for a Neo4j instance with custom configuration.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
    /// be used. The supported protocols in URI could either be <c>bolt</c> or <c>neo4j</c>. The protocol <c>bolt</c> should be
    /// used when creating a driver connecting to the Neo4j instance directly. The protocol <c>neo4j</c> should be used when
    /// creating a driver with built-in routing.
    /// </param>
    /// <param name="authTokenManager">The <see cref="IAuthTokenManager"/> to use for authentication.</param>
    /// <param name="action">
    /// Specifies how to build a driver configuration <see cref="Config"/>, using
    /// <see cref="ConfigBuilder"/>. If set to <c>null</c>, then no modification will be carried out and the default driver
    /// configurations <see cref="Config"/> will be used when creating the driver.
    /// </param>
    /// <returns>A new <see cref="IDriver"/> instance.</returns>
    public static IDriver Driver(string uri, IAuthTokenManager authTokenManager, Action<ConfigBuilder> action)
    {
        return GraphDatabase.Driver(uri, authTokenManager, action);
    }

    /// <summary>Returns a driver for a Neo4j instance with custom configuration.</summary>
    /// <param name="uri">
    /// The URI to the Neo4j instance. Should be in the form
    /// <c>protocol://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
    /// be used. The supported protocols in URI could either be <c>bolt</c> or <c>neo4j</c>. The protocol <c>bolt</c> should be
    /// used when creating a driver connecting to the Neo4j instance directly. The protocol <c>neo4j</c> should be used when
    /// creating a driver with built-in routing.
    /// </param>
    /// <param name="authTokenManager">The <see cref="IAuthTokenManager"/> to use for authentication.</param>
    /// <param name="action">
    /// Specifies how to build a driver configuration <see cref="Config"/>, using
    /// <see cref="ConfigBuilder"/>. If set to <c>null</c>, then no modification will be carried out and the default driver
    /// configurations <see cref="Config"/> will be used when creating the driver.
    /// </param>
    /// <returns>A new <see cref="IDriver"/> instance.</returns>
    public static IDriver Driver(Uri uri, IAuthTokenManager authTokenManager, Action<ConfigBuilder> action)
    {
        return GraphDatabase.Driver(uri, authTokenManager, action);
    }
}
