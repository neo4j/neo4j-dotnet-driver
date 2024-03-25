# Neo4j .NET Driver
This repository contains the official [Neo4j](https://neo4j.com/) driver for .NET.  

### [API Docs](https://neo4j.com/docs/api/dotnet-driver/current/) | [Driver Manual](https://neo4j.com/docs/dotnet-manual/current/) | [Example Web App](https://github.com/neo4j-examples/movies-dotnetcore-bolt) | [Change Log](https://github.com/neo4j/neo4j-dotnet-driver/wiki/5.X-Change-Log)
This document covers the usage of the driver; for contribution guidance, see [Contributing](./CONTRIBUTING.md).

## Installation
Neo4j publishes its .NET libraries to NuGet with the following targets:
- [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/api/?view=netstandard-2.0), for more info: https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0
- [.NET 6.0](https://learn.microsoft.com/en-us/dotnet/api/?view=net-6.0)

To add the latest [NuGet package](https://www.nuget.org/packages/Neo4j.Driver):
```posh
PM> dotnet add package Neo4j.Driver
```

### Versions
Starting with 5.0, the Neo4j drivers moved to a monthly release cadence. A new minor version is released on the last Thursday of each month to maintain versioning consistency with the core product (Neo4j DBMS), which also has moved to a monthly cadence.

As a policy, Neo4j will not release patch versions except on rare occasions. Bug fixes and updates will go into the latest minor version; users should upgrade to a later version to patch bug fixes. Driver upgrades within a major version will never contain breaking API changes, excluding the `Neo4j.Driver.Preview` namespace reserved for the preview of features.

See also: https://neo4j.com/developer/kb/neo4j-supported-versions/

### Synchronous and Reactive driver extensions
* [Neo4j.Driver.Simple](https://www.nuget.org/packages/Neo4j.Driver.Simple/) exposes synchronous APIs on the driver's IDriver interface.
* [Neo4j.Driver.Reactive](https://www.nuget.org/packages/Neo4j.Driver.Reactive/) exposes reactive APIs on the driver's IDriver interface.

### Strong-named
A [strong-named](https://learn.microsoft.com/en-us/dotnet/standard/assembly/strong-named) version of each driver package is available on NuGet [Neo4j.Driver.Signed](https://www.nuget.org/packages/Neo4j.Driver.Signed). The strong-named packages contain the same version of
their respective packages with strong-name compliance. _Consider using the strong-named version only if your project is strong-named or requires strong-named dependencies._

To add the strong-named version of the driver to your project using the NuGet Package Manager:
```posh
PM> Install-Package Neo4j.Driver.Signed
```

## Getting started
### Connecting to a Neo4j database:
```csharp
using Neo4j.Driver;

await using var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "password"));
```
There are a few points to highlight when adding the driver to your project:
* Each `IDriver` instance maintains a pool of connections inside; as a result, use a single driver instance per application.
* Sessions and transactions do not open new connections if a free one is in the driver's connection pool; this makes both resources cheap to create and close.
* The driver is thread-safe and made to be used across an application. Sessions and transactions are not thread-safe; using a session or transaction concurrently will result in undefined behavior.

### Verifying connectivity:
```csharp
await driver.VerifyConnectivityAsync();
```
To ensure the credentials and URLs specified when creating the driver, you can call `VerifyConnectivityAsync` on the driver instance. If either configuration is wrong, the Task will result in an exception.

### Executing a single query transaction:
```csharp
await driver.ExecutableQuery("CREATE (:Node{id: 0})")
    .WithConfig(new QueryConfig(database:"neo4j"))
    .ExecuteAsync();
```
As of version 5.10, The .NET driver includes a fluent querying API on the driver's IDriver interface. The fluent API is the most concise API for executing single query transactions. It avoids the boilerplate that comes with handling complex problems, such as results that exceed memory or multi-query transactions.

### Remember to specify a database.
```csharp
    .WithConfig(new QueryConfig(database:"neo4j"))
```
Always specify the database when you know which database the transaction should execute against. By setting the database parameter, the driver avoids a roundtrip and concurrency machinery associated with negotiating a home database. 

### Getting Results
```csharp
var response = await driver.ExecutableQuery("MATCH (n:Node) RETURN n.id as id")
    .WithConfig(dbConfig)
    .ExecuteAsync();
```
The response from the fluent APIs is an [EagerResult](https://neo4j.com/docs/api/dotnet-driver/current/api/Neo4j.Driver.EagerResult-1.html)<IReadOnlyList<[IRecord](https://neo4j.com/docs/api/dotnet-driver/current/api/Neo4j.Driver.IRecord.html)>> unless we use other APIs; more on that later. 
EagerResult comprises of the following:
- All records materialized(`Result`).
- keys returned from the query(`Keys`).
- a [query summary](https://neo4j.com/docs/api/dotnet-driver/current/api/Neo4j.Driver.IResultSummary.html)(`Summary`). 

#### Decomposing EagerResult
```csharp
var (result, _, _) = await driver.ExecutableQuery(query)
    .WithConfig(dbConfig)
    .ExecuteAsync();
foreach (var record in result)
    Console.WriteLine($"node: {record["id"]}")
```
EagerResult allows you to discard unneeded values with decomposition for an expressive API. 

#### Mapping
```csharp
var (result, _, _) = await driver.ExecutableQuery(query)
    .WithConfig(dbConfig)
    .WithMap(record => new EntityDTO { id = record["id"].As<long>() })
    .ExecuteAsync();
```
