# Neo4j .NET Driver
This repository contains the official Neo4j driver for .NET.  
Resources to get you started:
* [Nuget](https://www.nuget.org/profiles/Neo4j) for getting the latest driver.
* [Neo4j Docs](https://neo4j.com/docs/) for other important Neo4j documentations.
* [Driver Manual](https://neo4j.com/docs/dotnet-manual/current/) Neo4j's official .NET Driver manual.
* [Movies Example Application](https://github.com/neo4j-examples/movies-dotnetcore-bolt) a sample small project using
  the driver.
* [Driver Wiki](https://github.com/neo4j/neo4j-dotnet-driver/wiki) for changelogs, developer manual, and API documentation.
* [API Docs](https://neo4j.com/docs/api/dotnet-driver/current/) for detailed API Docs.

## Installation
The projects are released with the following targets:
- [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/api/?view=netstandard-2.0), for more info: https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0
- [.NET 6.0](https://learn.microsoft.com/en-us/dotnet/api/?view=net-6.0)

To add the latest [NuGet package](https://www.nuget.org/packages/Neo4j.Driver), use:
```posh
PM> dotnet add package Neo4j.Driver
```

### Versions
Starting with 5.0, the Neo4j Drivers moved to a monthly release cadence. A minor version is released on
the last Thursday of each month to maintain versioning consistency with the core product (Neo4j DBMS), which also has
moved to a monthly cadence.

As a policy, patch versions will not be released except on rare occasions. Bug fixes and updates will go into the latest
minor version; users should upgrade to that. Driver upgrades within a major version will never contain breaking API
changes, excluding the `Neo4j.Driver.Preview` namespace reserved for the preview of features.

See also: https://neo4j.com/developer/kb/neo4j-supported-versions/

### Synchronous and Reactive drivers
* [Neo4j.Driver.Simple](https://www.nuget.org/packages/Neo4j.Driver.Simple/) for accessing Neo4j via synchronous API.
* [Neo4j.Driver.Reactive](https://www.nuget.org/packages/Neo4j.Driver.Reactive/) for accessing Neo4j via reactive API.

### Strong-named
A [strong-named](https://learn.microsoft.com/en-us/dotnet/standard/assembly/strong-named) version of each driver package is available on Nuget [Neo4j.Driver.Signed](https://www.nuget.org/packages/Neo4j.Driver.Signed). The strong-named packages contain the same version of
their respective packages with strong-name compliance. _Consider using the strong-named version only if your project is strong-named and/or you are forced to use strong-named dependencies._

To add the strong-named version of the driver to your project using the NuGet Package Manager:
```posh
PM> Install-Package Neo4j.Driver.Signed
```

## Getting started
### Connecting to a Neo4j database:
```csharp
using Neo4j.Driver;

using var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "password"));
```
There are a few points that need to be highlighted when adding the driver to your project:
* Each `IDriver` instance maintains a pool of connections inside; as a result, it is recommended that you use **only one driver per application**.
* It is considerably cheap to create new sessions and transactions, as sessions and transactions do not make new connections as long as free connections are available in the connection pool.
* The driver is thread-safe, while sessions and transactions are not thread-safe.

### Verifying connectivity:
```csharp
await driver.VerifyConnectivityAsync();
```
The driver exposes a simple method to allow users to verify that the URI and credentials can open a connection.

### Executing your first query:
```csharp
var result = await driver.ExecutableQuery("CREATE (n) RETURN n").ExecuteAsync();
```
From 5.10, The .NET driver has a fluent query API from the driver interface. This is very useful for executing single query transactions while avoiding the boilerplate that comes with handling complex problems such as results that exceed memory or multi-query transactions.
