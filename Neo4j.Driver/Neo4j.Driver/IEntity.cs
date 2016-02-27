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
using System.Collections.Generic;

namespace Neo4j.Driver
{
    /// <summary>
    /// The base interface for <see cref="INode"/> and <see cref="IRelationship"/>
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Gets the value that has the specified key in <see cref="Properties"/> 
        /// </summary>
        /// <param name="key">the key</param>
        /// <returns>the value specified by the given key in <see cref="Properties"/></returns>
        object this[string key] { get; }

        /// <summary>
        /// Gets the unique <see cref="IIdentity"/> of the <c>Entity</c>.
        /// </summary>
        IIdentity Identity { get; }
        /// <summary>
        /// Gets the properties of the <c>Entity</c>.
        /// </summary>
        IReadOnlyDictionary<string, object> Properties { get; }
    }

    /// <summary>
    /// Represents a <c>Node</c> in the Neo4j graph database 
    /// </summary>
    public interface INode: IEntity
    {
        /// <summary>
        /// Gets the lables of the node.
        /// </summary>
        IReadOnlyList<string> Labels { get; }
        //bool HasLabel(string label);
    }

    /// <summary>
    /// Represents a <c>Relationship</c> in the Neo4j graph database
    /// </summary>
    public interface IRelationship:IEntity
    {
        /// <summary>
        /// Gets the type of the relationship
        /// </summary>
        string Type { get; }
        //bool HasType(string type);
        /// <summary>
        /// Gets the <see cref="IIdentity"/> of the start node of the relationship.
        /// </summary>
        IIdentity Start { get; }
        /// <summary>
        /// Gets the <see cref="IIdentity"/> of the end node of the relationship.
        /// </summary>
        IIdentity End { get; }
    }

    /// <summary>
    ///     A <c>Path</c> is a directed sequence of relationships between two nodes. This generally
    ///     represents a <em>traversal</em> or <em>walk</em> through a graph and maintains a direction separate
    ///     from that of any relationships traversed.
    ///     It is allowed to be of size 0, meaning there are no relationships in it.In this case,
    ///     it contains only a single node which is both the start and the end of the path.
    /// </summary>
    public interface IPath
    {
        /// <summary>
        /// Gets the start <see cref="INode"/> in the path.
        /// </summary>
        INode Start { get; }
        /// <summary>
        /// Gets the end <see cref="INode"/> in the path.
        /// </summary>
        INode End { get; }
        //int Length {get;}
        //bool Contains(INode node);
        //bool Contains(IRelationship rel);
        /// <summary>
        /// Gets all the nodes in the path.
        /// </summary>
        IReadOnlyList<INode> Nodes { get; }
        /// <summary>
        /// Gets all the relationships in the path.
        /// </summary>
        IReadOnlyList<IRelationship> Relationships { get; }
    }

    /// <summary>
    /// A unique identifer
    /// </summary>
    public interface IIdentity
    {
        // bool equal(object id)
        // int hashCode();
        /// <summary>
        /// Get the identity as a <see cref="long"/> number
        /// </summary>
        long Id { get; }
    }
}
