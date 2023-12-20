// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal.Types;

internal class Path : IPath
{
    private readonly IReadOnlyList<ISegment> _segments;

    public Path(
        IReadOnlyList<ISegment> segments,
        IReadOnlyList<INode> nodes,
        IReadOnlyList<IRelationship> relationships)
    {
        _segments = segments;
        Nodes = nodes;
        Relationships = relationships;
    }

    public INode Start => Nodes.First();
    public INode End => Nodes.Last();
    public IReadOnlyList<INode> Nodes { get; }
    public IReadOnlyList<IRelationship> Relationships { get; }

    public bool Equals(IPath other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Equals(Start, other.Start) && Relationships.SequenceEqual(other.Relationships);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as IPath);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Start?.GetHashCode() ?? 0;
            hashCode = Relationships?.Aggregate(
                    hashCode,
                    (current, relationship) => (current * 397) ^ relationship.GetHashCode()) ??
                hashCode;

            return hashCode;
        }
    }

    public override string ToString()
    {
        return
            $"Path with {Relationships.Count} relationships and {Nodes.Count} nodes, starting at {Start}, " +
            $"ending at {End}, segments: {string.Join(", ", _segments)}";
    }
}
