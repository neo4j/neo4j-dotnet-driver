# Neo4j dotnet Driver Change Log

## Version 5.0
- Remove `Version` from `IServerInfo.cs` and `SummaryBuilder.cs` previously deprecated.
    - use `IServerInfo.Agent`, `IServerInfo.ProtocolVersion` or call the `dbms.components` procedure instead.
## Version 5.0
- Change Transaction config timeout to accept null, Timeout.Zero explicitly.
    - Zero removes timout.
    - null uses server default timeout.
