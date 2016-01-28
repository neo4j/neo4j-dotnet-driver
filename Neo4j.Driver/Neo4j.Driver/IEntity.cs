using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver
{
    /// <summary>
    /// The base class for Node and Relationship
    /// </summary>
    public interface IEntity
    {
        IIdentity Identity { get; }
        IReadOnlyDictionary<string, object> Properties { get; }
    }

    public interface INode: IEntity
    {
        IReadOnlyList<string> Labels { get; }
        //bool HasLabel(string label);
    }

    public interface IRelationship:IEntity
    {
        string Type { get; }
        //bool HasType(string type);
        IIdentity Start { get; }
        IIdentity End { get; }
    }

    public interface IPath
    {
        INode Start { get; }
        INode End { get; }
        //int Length();
        //bool Contains(INode node);
        //bool Contains(IRelationship rel);
        IReadOnlyList<INode> Nodes { get; }
        IReadOnlyList<IRelationship> Relationships { get; }
    }

    public interface ISegment
    {
        INode Start { get; }
        INode End { get; }
        IRelationship Relationship { get; }
    }

    public interface IIdentity
    {
        // bool equal(object id)
        // int hashCode();
        long Id { get; }
    }
}
