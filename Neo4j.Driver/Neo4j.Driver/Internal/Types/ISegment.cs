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

namespace Neo4j.Driver.Internal.Types;

/// <summary>
/// A <c>Segment</c> combines a relationship in a path with a start and end node that describe the traversal
/// direction for that relationship. This exists because the relationship has a direction between the two nodes that is
/// separate and potentially different from the direction of the path.
/// </summary>
internal interface ISegment : IEquatable<ISegment>
{
    /// <summary>Gets the start node underlying this path segment.</summary>
    INode Start { get; }

    /// <summary>Gets the end node underlying this path segment.</summary>
    INode End { get; }

    /// <summary>Gets the relationship underlying this path segment.</summary>
    IRelationship Relationship { get; }
}
