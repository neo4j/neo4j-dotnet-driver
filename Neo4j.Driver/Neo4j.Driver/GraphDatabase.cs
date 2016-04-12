﻿// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Neo4j.Driver
{
    /// <summary>
    ///     Creates <see cref="Neo4j.Driver.Driver" /> instances, optionally letting you
    ///     configure them
    /// </summary>
    public static class GraphDatabase
    {
        /// <summary>
        ///     Return a driver for a Neo4j instance with default configuration settings
        /// </summary>
        /// <param name="uri">
        ///     The <see cref="Uri" /> to the Neo4j instance. Should be in the form
        ///     <c>bolt://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.
        /// </param>
        /// <param name="config">
        ///     Configuration for the driver instance to use, if <c>null</c> <see cref="Config.DefaultConfig" />
        ///     is used
        /// </param>
        /// <returns>A new <see cref="Neo4j.Driver.Driver" /> instance specified by the <paramref name="uri" /></returns>
        /// <remarks>Ensure you provide the protocol for the <paramref name="uri" /></remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri" /> is <c>null</c></exception>
        public static IDriver Driver(Uri uri, Config config = null)
        {
            return Driver(uri, AuthTokens.None, config ?? Config.DefaultConfig);
        }

        /// <summary>
        ///     Return a driver for a Neo4j instance with default configuration settings
        /// </summary>
        /// <param name="uri">
        ///     The URI to the Neo4j instance. Should be in the form
        ///     <c>bolt://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.
        /// </param>
        /// <param name="config">
        ///     Configuration for the driver instance to use, if <c>null</c> <see cref="Config.DefaultConfig" />
        ///     is used
        /// </param>
        /// <returns>A new <see cref="Neo4j.Driver.Driver" /> instance specified by the <paramref name="uri" /></returns>
        /// <remarks>Ensure you provide the protocol for the <paramref name="uri" /></remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri" /> is <c>null</c></exception>
        public static IDriver Driver(string uri, Config config = null)
        {
            return Driver(new Uri(uri), AuthTokens.None, config ?? Config.DefaultConfig);
        }

        /// <summary>
        ///     Return a driver for a Neo4j instance with custom configuration.
        /// </summary>
        /// <param name="uri">
        ///     The <see cref="Uri" /> to the Neo4j instance. Should be in the form
        ///     <c>bolt://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.</param>
        /// <param name="authToken">Authentication to use, <see cref="AuthTokens" /></param>
        /// <param name="config">User defined configuration</param>
        /// <returns>A new driver to the database instance specified by the URI</returns>
        public static IDriver Driver(string uri, IAuthToken authToken, Config config = null)
        {
            return Driver(new Uri(uri), authToken, config ?? Config.DefaultConfig);
        }

        /// <summary>
        ///     Return a driver for a Neo4j instance with custom configuration.
        /// </summary>
        /// <param name="uri">        
        ///     The URI to the Neo4j instance. Should be in the form
        ///     <c>bolt://&lt;server location&gt;:&lt;port&gt;</c>. If <c>port</c> is not supplied the default of <c>7687</c> will
        ///     be used.</param>
        /// <param name="authToken">Authentication to use, <see cref="AuthTokens" /></param>
        /// <param name="config">User defined configuration</param>
        /// <returns>A new driver to the database instance specified by the URI</returns>
        public static IDriver Driver(Uri uri, IAuthToken authToken, Config config = null)
        {
            return new Driver(uri, authToken, config ?? Config.DefaultConfig);
        }
    }
}