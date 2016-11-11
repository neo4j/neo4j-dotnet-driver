using System;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal interface IConnectionErrorHandler
    {
        Exception OnConnectionError(Exception e);
        Neo4jException OnNeo4jError(Neo4jException e);
    }
}