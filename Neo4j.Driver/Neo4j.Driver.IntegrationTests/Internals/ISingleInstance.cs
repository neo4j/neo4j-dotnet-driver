using System;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public interface ISingleInstance
    {
        Uri HttpUri { get; }
        Uri BoltUri { get; }
        Uri BoltRoutingUri { get; }
        string HomePath { get; }
        IAuthToken AuthToken { get; }
    }
}