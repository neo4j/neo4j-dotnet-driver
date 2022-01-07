# Neo4j dotnet Driver Change Log

## Version 5.0
- Remove `Version` from `IServerInfo.cs` and `SummaryBuilder.cs` previously deprecated.
    - use `IServerInfo.Agent`, `IServerInfo.ProtocolVersion` or call the `dbms.components` procedure instead.