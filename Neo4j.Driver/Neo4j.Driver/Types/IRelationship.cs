// Copyright (c) 2002-2019 "Neo4j,"
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

namespace Neo4j.Driver
{
    /// <summary>
    /// Represents a <c>Relationship</c> in the Neo4j graph database.
    /// </summary>
    public interface IRelationship : IEntity, IEquatable<IRelationship>
    {
        /// <summary>
        /// Gets the type of the relationship.
        /// </summary>
        string Type { get; }

        //bool HasType(string type);
        /// <summary>
        /// Gets the id of the start node of the relationship.
        /// </summary>
        long StartNodeId { get; }

        /// <summary>
        /// Gets the id of the end node of the relationship.
        /// </summary>
        long EndNodeId { get; }
    }
}