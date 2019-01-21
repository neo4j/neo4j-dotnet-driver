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
using System.Collections.Generic;

namespace Neo4j.Driver
{
    /// <summary>
    /// Represents an <c>Entity</c> in the Neo4j graph database. An <c>Entity</c> could be a <c>Node</c> or a <c>Relationship</c>.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Gets the value that has the specified key in <see cref="Properties"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value specified by the given key in <see cref="Properties"/>.</returns>
        object this[string key] { get; }
        /// <summary>
        /// Gets the properties of the entity.
        /// </summary>
        IReadOnlyDictionary<string, object> Properties { get; }
        /// <summary>
        /// Get the identity as a <see cref="long"/> number.
        /// </summary>
        long Id { get; }
    }

    /// <summary>
    /// Represents a <c>Node</c> in the Neo4j graph database.
    /// </summary>
    public interface INode: IEntity, IEquatable<INode>
    {
        /// <summary>
        /// Gets the lables of the node.
        /// </summary>
        IReadOnlyList<string> Labels { get; }
    }

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

    /// <summary>
    ///     A <c>Path</c> is a directed sequence of relationships between two nodes. This generally
    ///     represents a <em>traversal</em> or <em>walk</em> through a graph and maintains a direction separate
    ///     from that of any relationships traversed.
    ///     It is allowed to be of size 0, meaning there are no relationships in it. In this case,
    ///     it contains only a single node which is both the start and the end of the path.
    /// </summary>
    public interface IPath : IEquatable<IPath>
    {
        /// <summary>
        /// Gets the start <see cref="INode"/> in the path.
        /// </summary>
        INode Start { get; }
        /// <summary>
        /// Gets the end <see cref="INode"/> in the path.
        /// </summary>
        INode End { get; }
        /// <summary>
        /// Gets all the nodes in the path.
        /// </summary>
        IReadOnlyList<INode> Nodes { get; }
        /// <summary>
        /// Gets all the relationships in the path.
        /// </summary>
        IReadOnlyList<IRelationship> Relationships { get; }
    }
}
