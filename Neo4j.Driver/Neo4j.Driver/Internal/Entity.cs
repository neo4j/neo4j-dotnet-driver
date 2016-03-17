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
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal
{
    public class Node : INode, IEquatable<INode>
    {
        public long Id { get; }
        public IReadOnlyList<string> Labels { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }
        public object this[string key] => Properties[key];

        public Node(long id, IReadOnlyList<string> lables, IReadOnlyDictionary<string, object> prop)
        {
            Id = id;
            Labels = lables;
            Properties = prop;
        }

        public bool Equals(INode other)
        {
            return Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Node) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public class Relationship : IRelationship, IEquatable<IRelationship>
    {
        public long Id { get; }
        public string Type { get; }
        public long StartNodeId { get; internal set; }
        public long EndNodeId { get; internal set; }
        public IReadOnlyDictionary<string, object> Properties { get; }
        public object this[string key] => Properties[key];

        public Relationship(long id, long startId, long endId, string relType,
            IReadOnlyDictionary<string, object> props)
        {
            Id = id;
            StartNodeId = startId;
            EndNodeId = endId;
            Type = relType;
            Properties = props;
        }

        public bool Equals(IRelationship other)
        {
            return Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Relationship) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        internal void SetStartAndEnd(long start, long end)
        {
            StartNodeId = start;
            EndNodeId = end;
        }
    }

    /// <summary>
    ///    
    /// A <c>Segment</c> combines a relationship in a path with a start and end node that describe the traversal direction
    /// for that relationship.This exists because the relationship has a direction between the two nodes that is
    /// separate and potentially different from the direction of the path.
    /// </summary>
    public interface ISegment
    {
        /// <summary>
        /// Gets the start node underlying this path segment.
        /// </summary>
        INode Start { get; }
        /// <summary>
        /// Gets the end node underlying this path segment.
        /// </summary>
        INode End { get; }
        /// <summary>
        /// Gets the relationship underlying this path segment.
        /// </summary>
        IRelationship Relationship { get; }
    }

    public class Segment : ISegment, IEquatable<ISegment>
    {
        public Segment(INode start, IRelationship rel, INode end)
        {
            Start = start;
            Relationship = rel;
            End = end;
        }

        public INode Start { get; }
        public INode End { get; }
        public IRelationship Relationship { get; }

        public bool Equals(ISegment other)
        {
            return Equals(Start, other.Start) && Equals(End, other.End) && Equals(Relationship, other.Relationship);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ISegment)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Start?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (End?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (Relationship?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }

    public class Path : IPath, IEquatable<IPath>
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
            return Equals(Nodes, other.Nodes) && Equals(Relationships, other.Relationships);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((IPath)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Nodes?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (Relationships?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        
    }
}