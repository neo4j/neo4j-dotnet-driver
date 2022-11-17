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
using System.Collections.Generic;

namespace Neo4j.Driver;

/// <summary>
/// A <c>Path</c> is a directed sequence of relationships between two nodes. This generally represents a
/// <em>traversal</em> or <em>walk</em> through a graph and maintains a direction separate from that of any relationships
/// traversed. It is allowed to be of size 0, meaning there are no relationships in it. In this case, it contains only a
/// single node which is both the start and the end of the path.
/// </summary>
public interface IPath : IEquatable<IPath>
{
    /// <summary>Gets the start <see cref="INode"/> in the path.</summary>
    INode Start { get; }

    /// <summary>Gets the end <see cref="INode"/> in the path.</summary>
    INode End { get; }

    /// <summary>Gets all the nodes in the path.</summary>
    IReadOnlyList<INode> Nodes { get; }

    /// <summary>Gets all the relationships in the path.</summary>
    IReadOnlyList<IRelationship> Relationships { get; }
}
