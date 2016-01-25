using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal
{
    public class InternalNode : INode, IEquatable<INode>
    {
        public InternalNode(long id, IReadOnlyList<string> lables, IReadOnlyDictionary<string, object> prop)
        {
            Identity = new InternalIdentity(id);
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
            return Equals((InternalNode) obj);
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

    public class InternalRelationship : IRelationship, IEquatable<IRelationship>
    {
        public InternalRelationship(long id, long startId, long endId, string relType,
            IReadOnlyDictionary<string, object> props)
        {
            Identity = new InternalIdentity(id);
            Start = new InternalIdentity(startId);
            End = new InternalIdentity(endId);
            Type = relType;
            Properties = props;
        }

        public InternalRelationship(IIdentity id, IIdentity start, IIdentity end, string relType,
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
            return Equals((InternalRelationship) obj);
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

    public class InternalIdentity : IIdentity, IEquatable<IIdentity>
    {
        public InternalIdentity(long id)
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
            return Equals((InternalIdentity) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public class InternalSegment : ISegment
    {
        public InternalSegment(INode start, IRelationship rel, INode end)
        {
            Start = start;
            Relationship = rel;
            End = end;
        }

        public INode Start { get; }
        public INode End { get; }
        public IRelationship Relationship { get; }
    }

    public class InternalPath : IPath
    {
        private readonly IReadOnlyList<ISegment> _segments;

        public InternalPath(IReadOnlyList<ISegment> segments, IReadOnlyList<INode> nodes,
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

    public static class CollectionExtensions
    {
        public static bool ContentEqual<T>(this IReadOnlyCollection<T> collection, IReadOnlyCollection<T> other)
        {
            if (collection == null && other == null)
                return true;

            if (collection == null || other == null || collection.Count != other.Count)
                return false;

            if (collection.Any(item => !other.Contains(item)))
            {
                return false;
            }
            return true;
        }

        public static bool ContentEqual<T, V>(this IReadOnlyDictionary<T, V> dict, IReadOnlyDictionary<T, V> other)
        {
            if (dict == null && other == null)
                return true;

            if (dict == null || other == null || dict.Count != other.Count)
                return false;

            foreach (var item in dict)
            {
                if (!other.ContainsKey(item.Key))
                    return false;

                if (!other[item.Key].Equals(item.Value))
                    return false;
            }
            return true;
        }
    }
}