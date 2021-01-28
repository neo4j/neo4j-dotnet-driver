// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Collections.Generic;

namespace Neo4j.Driver
{
    /// <summary>
    ///  Common interface for components that can execute Neo4j queries.
    /// </summary>
    /// <remarks>
    /// <see cref="IAsyncSession"/> and <see cref="IAsyncTransaction"/>
    /// </remarks>
    public interface IQueryRunner : IDisposable
    {
        /// <summary>
        /// 
        /// Run a query and return a result stream.
        ///
        /// This method accepts a String representing a Cypher query which will be 
        /// compiled into a query object that can be used to efficiently execute this
        /// query multiple times. 
        /// </summary>
        /// <param name="query">A Cypher query.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IResult Run(string query);

        /// <summary>
        /// Execute a query and return a result stream.
        /// </summary>
        /// <param name="query">A Cypher query.</param>
        /// <param name="parameters">A parameter dictionary which is made of prop.Name=prop.Value pairs would be created.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IResult Run(string query, object parameters);

        /// <summary>
        /// 
        /// Run a query and return a result stream.
        ///
        /// This method accepts a String representing a Cypher query which will be 
        /// compiled into a query object that can be used to efficiently execute this
        /// query multiple times. This method optionally accepts a set of parameters
        /// which will be injected into the query object query by Neo4j. 
        ///
        /// </summary>
        /// <param name="query">A Cypher query.</param>
        /// <param name="parameters">Input parameters for the query.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IResult Run(string query, IDictionary<string, object> parameters);

        /// <summary>
        ///
        /// Execute a query and return a result stream.
        ///
        /// </summary>
        /// <param name="query">A Cypher query, <see cref="Query"/>.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IResult Run(Query query);
    }
}