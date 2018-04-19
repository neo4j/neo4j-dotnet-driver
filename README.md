# Neo4j .NET Driver
This is the official Neo4j .NET driver for connecting to Neo4j 3.0.0+ databases via in-house binary protocol Bolt.

Resources to get you started:
* [Nuget](https://www.nuget.org/packages/Neo4j.Driver/) for getting the latest driver.
* [Driver Wiki](https://github.com/neo4j/neo4j-dotnet-driver/wiki) for changelogs, developer manual and API documents of this driver.
* [Neo4j Docs](https://neo4j.com/docs/) for other important Neo4j documentations.
* [Movies Example Application](https://github.com/neo4j-examples/movies-dotnet-bolt) a sample small project using the driver.

## For Application Developers
This section targeting at application developers who would like to use this driver in appliation projects for connecting to a Neo4j instance or a Neo4j cluster.

### Getting the Driver

The Neo4j Driver is distributed exclusively via [Nuget](https://www.nuget.org/packages/Neo4j.Driver).

Add the driver to your project using the Nuget Package Manager:
```
PM> Install-Package Neo4j.Driver
```
### Minimum Viable Snippet

Connect to a Neo4j database
```csharp
IDriver driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("username", "pasSW0rd"));
using(ISession session = driver.Session())
{
    IStatementResult result = session.Run("CREATE (n) RETURN n");
}
driver.Dispose();
```
There are a few points that need to be highlighted when adding this driver into your project:
* Each `IDriver` instance maintains a pool of connections inside, as a result, it is recommended to only use **one driver per application**.
* It is considerably cheap to create new sessions and transactions,
as sessions and transactions do not create new connections as long as there are free connections available in the connection pool.
* The driver is thread-safe, while the session or the transaction is not thread-safe.

### Parsing Result Values
#### Record Stream
A cypher execution result is comprised of a stream records followed by a result summary.
The records inside the result are accessible via `IEnumerable` interface on `IStatementResult`.
Our recommended way to access these result records is to make use of `Linq` methods such as `Single`, `ToList`, `Select`.

Process result record using `Linq`:
```csharp
IStatementResult result = tx.Run("MATCH (a:Person) RETURN a.name as name");
List<string> people = result.Select(record => record["name"].As<string>()).ToList();
```
The records are given as a record stream in the sense that:
* A record is accessible once it arrives at the client. The record does not need to wait for the while result to complete before it can be visited.
* Each record could only be visited (a.k.a. consumed) once.

For example, given a record stream in a result:

| Keys | "name" |
| -------: | :----- |
| Record 0 | "Bruce Wayne" |
| Record 1 | "Selina Kyle" |

Visiting the record stream:
```csharp
result.First(); // Bruce Wayne
result.First(); // Selina Kyle as you already consumed the previous "first" record!
```

#### Value Types

The driver currently exposes value types in the record all as `object`.
The real types of the returned values are Cypher types.

The mapping between Cypher types and the types used by this driver (to represent the Cypher type):

| Cypher Type | Driver Type
| ---: | :--- |
| *null* | null |
| List | IList< object > |
| Map  | IDictionary<string, object> |
| Boolean| boolean |
| Integer| long |
| Float| float |
| String| string |
| ByteArray| byte[] |
| Point| Point |
| Node| INode |
| Relationship| IRelationship |
| Path| IPath |

To convert from `object` to the real local type, an helper method `ValueExtensions#As<T>` is available for this purpose:
```csharp
IRecord record = result.First();
string name = record["name"].As<string>();
```

#### Temporal Types - Date and Time

Since 1.6 series, the driver start to support the new temporal Cypher types introduced in Neo4j 3.4 series. 

The mapping among the Cypher temporal types, driver types, and driver type convertable C# built-in types:

| Cypher Type | Driver Type | Convertable C# Type |
| :----------: | :-----------: | :-------: |
| Date | LocalDate | DateTime |
| Time | OffsetTime | --- |
| LocalTime| LocalTime | TimeSpan, DateTime |
| DateTime | ZonedDateTime | DateTimeOffset |
| LocalDateTime | LocalDateTime | DateTime |
| Duration | Duration | --- |


Receiving a temporal value:
```csharp
IRecord record = result.Single();
ZonedDateTime datetime = record["datetime"].As<ZonedDateTime>();
```

Convert to C# built-in temporal type:
```csharp
object record = result.Single()["datetime"];

DateTimeOffset datetime = record["datetime"].As<DateTimeOffset>();
// which is equivalent to
// ZonedDateTime cyDatetime = record["datetime"].As<ZonedDateTime>();
// DateTimeOffset datetime = Convert.ToDateTimeOffset(cyDatetime)
```

Note:
* The conversion to C# System types is possible only when the driver temporal value could fit in the range of the targeting System type.
* The Cypher temporal types (excluding `Date`) provide nanosecond precision. However C# types could only give 100 nanosecond precision.
So a temporal type created via Cypher might not be able to convert to a C# type.     


## For Driver Developers
This section targets at people who would like to compile the source code on their own machine for the purpose of, for example,
contributing a PR to this repository.
Before contributing to this project, please take a few minutes and read our [Contributing Criteria](https://github.com/neo4j/neo4j-dotnet-driver/blob/1.6/CONTRIBUTING.md).


### Snapshots

Snapshot builds are available at our [MyGet feed](https://www.myget.org/feed/neo4j-driver-snapshots/package/nuget/Neo4j.Driver), add the feed to your Nuget Sources to access snapshot artifacts.

* [https://www.myget.org/F/neo4j-driver-snapshots/api/v3/index.json](https://www.myget.org/F/neo4j-driver-snapshots/api/v3/index.json)

### Building the Source Code

#### Visual Studio Version

The driver is written in C# 7 so will require Visual Studio 2017 (community edition).

#### Integration Tests

The integration tests will use [boltkit](https://github.com/neo4j-contrib/boltkit) to download and install a database instance on your local machine.
They can fail for three main reasons:

1. Python.exe and Python scripts folder is not installed and added in the system PATH variable
2. The tests aren't run as Administrator (you'll need to run Visual Studio as administrator)
3. You have an instance of Neo4j already installed / running on your local machine.

The database installation uses boltkit `neoctrl-install` command to install the database.
The integration tests could pass parameters to this command by setting environment variable `NeoctrlArgs`.

#### Run tests
The simplest way to run all tests from command line is to run `runTests.ps1` powershell script:

	.\Neo4j.Driver\runTests.ps1

Any parameter to this powershell script will be used to reset environment variable `NeoctrlArgs`:

	.\Neo4j.Driver\runTests.ps1 -e 3.3.0
