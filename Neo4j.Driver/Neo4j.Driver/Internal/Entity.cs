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
using Neo4j.Driver.Extensions;

namespace Neo4j.Driver.Internal
{
    public class Node : INode, IEquatable<INode>
    {
        public IIdentity Identity { get; }
        public IReadOnlyList<string> Labels { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }
        public object this[string key] => Properties[key];

        public Node(long id, IReadOnlyList<string> lables, IReadOnlyDictionary<string, object> prop)
        {
            Identity = new Identity(id);
            Labels = lables;
            Properties = prop;
        }

        public bool Equals(INode other)
        {
            var x = Equals(Identity, other.Identity);
            var y = Labels.ContentEqual(other.Labels);
            var z = Properties.ContentEqual(other.Properties);
            return x && y && z;
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
            unchecked
            {
                var hashCode = Identity != null ? Identity.GetHashCode() : 0;
                hashCode = (hashCode*397) ^ (Labels != null ? Labels.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Properties != null ? Properties.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Relationship : IRelationship, IEquatable<IRelationship>
    {
        public IIdentity Identity { get; }
        public string Type { get; }
        public IIdentity Start { get; internal set; }
        public IIdentity End { get; internal set; }
        public IReadOnlyDictionary<string, object> Properties { get; }
        public object this[string key] => Properties[key];

        public Relationship(long id, long startId, long endId, string relType,
            IReadOnlyDictionary<string, object> props)
        {
            Identity = new Identity(id);
            Start = new Identity(startId);
            End = new Identity(endId);
            Type = relType;
            Properties = props;
        }

        public Relationship(IIdentity id, IIdentity start, IIdentity end, string relType,
            IReadOnlyDictionary<string, object> props)
        {
            Identity = id;
            Start = start;
            End = end;
            Type = relType;
            Properties = props;
        }

        public bool Equals(IRelationship other)
        {
            if (!(Equals(Identity, other.Identity) && string.Equals(Type, other.Type) && Equals(Start, other.Start) &&
                  Equals(End, other.End)))
                return false;

            // map
            return Properties.ContentEqual(other.Properties);
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
            unchecked
            {
                var hashCode = Identity != null ? Identity.GetHashCode() : 0;
                hashCode = (hashCode*397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Start != null ? Start.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (End != null ? End.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Properties != null ? Properties.GetHashCode() : 0);
                return hashCode;
            }
        }

        internal void SetStartAndEnd(IIdentity start, IIdentity end)
        {
            Start = start;
            End = end;
        }
    }

    public class Identity : IIdentity, IEquatable<IIdentity>
    {
        public long Id { get; }

        public Identity(long id)
        {
            Id = id;
        }

        public bool Equals(IIdentity other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Identity) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public interface ISegment
    {
        INode Start { get; }
        INode End { get; }
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
        private readonly IReadOnlyList<ISegment> _segments; // TODO: do I need to expose this or not

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
                hashCode = (hashCode * 397) ^ (_segments?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string str = "<";
            INode start, end = null;
            IRelationship rel;
            int i = 0;
            foreach (var segment in _segments)
            {
                start = Nodes[i];
                end = Nodes[i + 1];
                rel = Relationships[i];

                if (segment.Start.Equals(start))
                {
                    str += start + "-" + rel + "->";
                }
                else
                {
                    str += start + "<-" + rel + "-";
                }
            }

            str += end + ">";
            return str;
        }
    }
}