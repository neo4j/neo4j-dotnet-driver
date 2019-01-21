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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Types
{
    internal class Path : IPath
    {
        private readonly IReadOnlyList<ISegment> _segments;

        public INode Start => Nodes.First();
        public INode End => Nodes.Last();
        public IReadOnlyList<INode> Nodes { get; }
        public IReadOnlyList<IRelationship> Relationships { get; }

        public Path(IReadOnlyList<ISegment> segments, IReadOnlyList<INode> nodes,
            IReadOnlyList<IRelationship> relationships)
        {
            _segments = segments;
            Nodes = nodes;
            Relationships = relationships;
        }

        public bool Equals(IPath other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
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
                hashCode = Relationships?.Aggregate(hashCode, (current, relationship) => (current * 397) ^ relationship.GetHashCode()) ?? hashCode;
                return hashCode;
            }
        }
    }
}
