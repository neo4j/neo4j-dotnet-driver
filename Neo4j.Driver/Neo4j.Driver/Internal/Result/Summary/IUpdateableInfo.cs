using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.Result;

internal interface IUpdateableInfo
{
    void Update(BoltProtocolVersion boltVersion, string agent);
}