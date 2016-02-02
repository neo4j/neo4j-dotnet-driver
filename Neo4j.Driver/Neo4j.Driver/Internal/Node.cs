using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal
{
    public class Node : INode, IEquatable<INode>
    {
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

        public IIdentity Identity { get; }
        public IReadOnlyList<string> Labels { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }

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

        public IIdentity Identity { get; }

        public string Type { get; }

        public IIdentity Start { get; internal set; }

        public IIdentity End { get; internal set; }

        public IReadOnlyDictionary<string, object> Properties { get; }

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
        public Identity(long id)
        {
            Id = id;
        }

        public bool Equals(IIdentity other)
        {
            return Id == other.Id;
        }

        public long Id { get; }

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

    public class Segment : ISegment
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
    }

    public class Path : IPath
    {
        private readonly IReadOnlyList<ISegment> _segments;

        public Path(IReadOnlyList<ISegment> segments, IReadOnlyList<INode> nodes,
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
    }
}