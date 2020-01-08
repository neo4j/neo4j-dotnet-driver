// Copyright (c) 2002-2020 "Neo4j,"
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
namespace Neo4j.Driver
{
    /// <summary>
    /// Provides basic information of the server where the cypher query was executed.
    /// </summary>
    public interface IServerInfo
    {
        /// <summary>
        /// Get the address of the server
        /// </summary>
        string Address { get; }
        /// <summary>
        /// Get the version of Neo4j running at the server.
        /// </summary>
        /// <remarks>
        /// Introduced since Neo4j 3.1. Default to <c>null</c> if not supported by server
        /// </remarks>
        string Version { get; }
    }
}
