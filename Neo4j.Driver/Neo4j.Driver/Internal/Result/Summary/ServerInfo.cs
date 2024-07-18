using System;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.Result;

internal class ServerInfo : IServerInfo, IUpdateableInfo
{
    public ServerInfo(Uri uri)
    {
        Address = $"{uri.Host}:{uri.Port}";
    }

    internal BoltProtocolVersion Protocol { get; set; }

    public string ProtocolVersion => Protocol?.ToString() ?? "0.0";

    public string Agent { get; set; }
    
    public string Address { get; }

    public void Update(BoltProtocolVersion boltVersion, string agent)
    {
        Protocol = boltVersion;
        Agent = agent;
    }

    public override string ToString()
    {
        return $"{GetType().Name}{{{nameof(Address)}={Address}, " +
            $"{nameof(Agent)}={Agent}, " +
            $"{nameof(ProtocolVersion)}={ProtocolVersion}}}";
    }
}
