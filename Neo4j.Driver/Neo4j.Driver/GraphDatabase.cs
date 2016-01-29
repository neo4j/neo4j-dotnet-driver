//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
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
        /// <param name="uri">The URL to the Neo4j instance</param>
        /// <param name="config">Configuration for the driver instance to use, if <c>null</c> default configuration is used</param>
        /// <returns>A new <see cref="Neo4j.Driver.Driver"/> instance specified by the <paramref name="uri"/></returns>
        /// <remarks>Ensure you provide the protocol for the <paramref name="uri"/></remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c></exception>
        public static Driver Driver(Uri uri, Config config = null)
        {
            return new Driver(uri, config ?? Config.DefaultConfig);
        }

        /// <summary>
        ///     Return a driver for a Neo4j instance with default configuration settings
        /// </summary>
        /// <param name="uri">The URL to the Neo4j instance</param>
        /// <param name="config">Configuration for the driver instance to use, if <c>null</c> default configuration is used</param>
        /// <returns>A new <see cref="Neo4j.Driver.Driver"/> instance specified by the <paramref name="uri"/></returns>
        /// <remarks>Ensure you provide the protocol for the <paramref name="uri"/></remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c></exception>
        public static Driver Driver(string uri, Config config = null)
        {
            return Driver(new Uri(uri), config ?? Config.DefaultConfig);
        }
    }
}