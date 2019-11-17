# Neo4j .NET Driver
This is the official Neo4j .NET driver for connecting to Neo4j 4.0.0+ databases via in-house binary protocol Bolt.

Resources to get you started:
* [Nuget](https://www.nuget.org/profiles/Neo4j) for getting the latest driver.
* [Driver Wiki](https://github.com/neo4j/neo4j-dotnet-driver/wiki) for changelogs, developer manual and API documents of this driver.
* [Neo4j Docs](https://neo4j.com/docs/) for other important Neo4j documentations.
* [Movies Example Application](https://github.com/neo4j-examples/movies-dotnet-bolt) a sample small project using the driver.

## For Application Developers
This section is prepared for application developers who would like to use this driver in application projects for connecting to a Neo4j instance or a Neo4j cluster.

### Getting the Driver

The Neo4j Driver is distributed exclusively via [Nuget](https://www.nuget.org/packages/Neo4j.Driver). 

Add the driver to your project using the Nuget Package Manager:
```posh
PM> Install-Package Neo4j.Driver
```

There is also a strong named version of the driver available on Nuget as [Neo4j.Driver.Signed](https://www.nuget.org/packages/Neo4j.Driver.Signed). Both packages contain the same version of the driver, only the latter is strong named. _Consider using the strong named version only if your project is strong named and/or you are forced to use strong named dependencies._

Add the strong named version of the driver to your project using the Nuget Package Manager:
```posh
PM> Install-Package Neo4j.Driver.Signed
```
### Minimum Viable Snippet

Connect to a Neo4j database
```csharp
IDriver driver = GraphDatabase.Driver("neo4j://localhost:7687", AuthTokens.Basic("username", "pasSW0rd"));
IAsyncSession session = driver.AsyncSession(o => o.WithDatabase("neo4j"));
try
{
    IStatementResultCursor cursor = await session.RunAsync("CREATE (n) RETURN n");
    await cursor.ConsumeAsync();
}
finally
{
    await session.CloseAsync();
}

...
await driver.CloseAsync();
```
There are a few points that need to be highlighted when adding this driver into your project:
* Each `IDriver` instance maintains a pool of connections inside, as a result, it is recommended to only use **one driver per application**.
* It is considerably cheap to create new sessions and transactions, as sessions and transactions do not create new connections as long as there are free connections available in the connection pool.
* The driver is thread-safe, while the session or the transaction is not thread-safe.

### Parsing Result Values
#### Record Stream
A cypher execution result is comprised of a stream records followed by a result summary.
The records inside the result are accessible via `FetchAsync` and `Current` methods on `IStatementResultCursor`.
Our recommended way to access these result records is to make use of methods provided by `StatementResultCursorExtensions` such as `SingleAsync`, `ToListAsync`, and `ForEachAsync`.

Process result records using `StatementResultCursorExtensions`:
```csharp
IStatementResultCursor cursor = await session.RunAsync("MATCH (a:Person) RETURN a.name as name");
List<string> people = await cursor.ToListAsync(record => record["name"].As<string>());
```
The records are exposed as a record stream in the sense that:
* A record is accessible once it is received by the client. It is not needed for the whole result set to be received before it can be visited.
* Each record can only be visited (a.k.a. consumed) once.

Records on a result cannot be accessed if the session or transaction where the result is created has been closed.

#### Value Types
Values in a record are currently exposed as of `object` type.
The underlying types of these values are determined by their Cypher types.

The mapping between driver types and Cypher types are listed in the table bellow:

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

To convert from `object` to the driver type, a helper method `ValueExtensions#As<T>` can be used:
```csharp
IRecord record = await result.SingleAsync();
string name = record["name"].As<string>();
```

#### Temporal Types - Date and Time

The new temporal types in Neo4j 3.4 series are introduced with the 1.6 series of the driver. Considering the nanosecond precision and large range of supported values, 
all temporal types are backed by custom types at the driver level.   

The mapping among the Cypher temporal types, driver types, and convertible CLR temporal types - DateTime, TimeSpan and DateTimeOffset - (via `IConvertible` interface) are as follows:

| Cypher Type | Driver Type | Convertible CLR Type |
| :----------: | :-----------: | :-------: |
| Date | LocalDate | DateTime |
| Time | OffsetTime | --- |
| LocalTime| LocalTime | TimeSpan, DateTime |
| DateTime | ZonedDateTime | DateTimeOffset |
| LocalDateTime | LocalDateTime | DateTime |
| Duration | Duration | --- |


Receiving a temporal value as driver type:
```csharp
IRecord record = await result.SingleAsync();
ZonedDateTime datetime = record["datetime"].As<ZonedDateTime>();
```

Converting a temporal value to the CLR type:
```csharp
object record = await result.SingleAsync()["datetime"];

DateTimeOffset datetime = record["datetime"].As<DateTimeOffset>();
// which is equivalent to
// ZonedDateTime cyDatetime = record["datetime"].As<ZonedDateTime>();
// DateTimeOffset datetime = cyDatetime.ToDateTimeOffset();
```

Note:
* The conversion to CLR types is possible only when the value fits in the range of the target built-in type. A `ValueOverflowException` is thrown 
when the conversion is not possible.
* The Cypher temporal types (excluding `Date`) provide nanosecond precision. However CLR types only support ticks (100 nanosecond) precision.
So a temporal type created via Cypher might not be convertible to the CLR type (a `ValueTruncationException` is thrown when a conversion is requested in this case).
* `ZonedDateTime` represents date and times with either offset or time zone information. Time zone names adhere to the [IANA system](https://www.iana.org/time-zones), 
rather than the [Windows system](https://docs.microsoft.com/en-us/windows-hardware/manufacture/desktop/default-time-zones). Although there is no support for inbound 
time zone name conversions, a conversion from IANA system to Windows system may be necessary if a conversion to `DateTimeOffset` or an access to `Offset` is 
requested by the user. [Unicode CLDR mapping](http://cldr.unicode.org/development/development-process/design-proposals/extended-windows-olson-zid-mapping) 
is used for this conversion. Please bear in mind that Windows time zone database do not provide precise historical data, so you may end up with inaccurate 
`DateTimeOffset` values for past values. _It is recommended that you use driver level temporal types to avoid these inaccuracies._    

### Logging

The driver accepts a logger that implements `ILogger` interface.
To pass a `ILogger` to this driver:
```c#
IDriver driver = GraphDatabase.Driver("neo4j://localhost:7687", AuthTokens.Basic("username", "pasSW0rd"), o => o.WithLogger(logger));
```

In this `ILogger` interface, each logging method takes a message format string and message arguments as input.
The full log messages can be restored using `string.format(message_format, message_argument)`.

An example of implementing `ILogger` with `Microsoft.Extensions.Logging`:
```c#
public class DriverLogger : ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _delegator;
    public DriverLogger(Microsoft.Extensions.Logging.ILogger delegator)
    {
       _delegator = delegator;
    }
    public void Error(Exception cause, string format, params object[] args)
    {
        _delegator.LogError(default(EventId), cause, format, args);
    }
    ...
}
```

## For Driver Developers
This section targets at people who would like to compile the source code on their own machines for the purpose of, for example,
contributing a PR to this repository.
Before contributing to this project, please take a few minutes and read our [Contributing Criteria](https://github.com/neo4j/neo4j-dotnet-driver/blob/1.6/CONTRIBUTING.md).


### Snapshots

Snapshot builds are available at our [MyGet feed](https://www.myget.org/gallery/neo4j-driver-snapshots), add the feed to your Nuget Sources to access snapshot artifacts.

* [https://www.myget.org/F/neo4j-driver-snapshots/api/v3/index.json](https://www.myget.org/F/neo4j-driver-snapshots/api/v3/index.json)

### Building the Source Code

#### Visual Studio Version

The driver is written in C# 7 so will require Visual Studio 2017.

#### Integration Tests

The integration tests use [boltkit](https://github.com/neo4j-contrib/boltkit) to download and install a database instance on your local machine.
They can fail for three main reasons:

1. Python.exe and Python scripts folder is not installed and added in the system PATH variable
2. The tests aren't run as Administrator (you'll need to run Visual Studio as administrator)
3. You have an instance of Neo4j already installed / running on your local machine.

The database installation uses boltkit `neoctrl-install` command to install the database.
It is possible to run the integration tests against a specific version by setting environment variable `NEOCTRL_ARGS`.

#### Run tests
The simplest way to run all tests from command line is to run `runTests.ps1` powershell script:

	.\Neo4j.Driver\runTests.ps1

Any parameter to this powershell script will be used to reset environment variable `NEOCTRL_ARGS`:

	.\Neo4j.Driver\runTests.ps1 -e 4.0.0
