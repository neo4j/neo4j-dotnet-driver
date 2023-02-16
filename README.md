# Neo4j .NET Driver

This is the official Neo4j driver for .NET.

Resources to get you started:

* [Nuget](https://www.nuget.org/profiles/Neo4j) for getting the latest driver.
* [Driver Wiki](https://github.com/neo4j/neo4j-dotnet-driver/wiki) for changelogs, developer manual and API documents of
  this driver.
* [Neo4j Docs](https://neo4j.com/docs/) for other important Neo4j documentations.
* [Movies Example Application](https://github.com/neo4j-examples/movies-dotnetcore-bolt) a sample small project using
  the driver.
* [Discussions](https://github.com/neo4j/neo4j-dotnet-driver/discussions/677) Have your say on improving the API.

## For Application Developers

This section is prepared for application developers who would like to use this driver in application projects for
connecting to a Neo4j instance or a Neo4j cluster.

For users who wish to migrate from 1.7 series to 4.0, checkout our [migration guide](#migrating-from-17-to-40).

## Versions

Starting with 5.0, the Neo4j Drivers will be moving to a monthly release cadence. A minor version will be released on
the last Friday of each month so as to maintain versioning consistency with the core product (Neo4j DBMS) which has also
moved to a monthly cadence.

As a policy, patch versions will not be released except on rare occasions. Bug fixes and updates will go into the latest
minor version and users should upgrade to that. Driver upgrades within a major version will never contain breaking API
changes(Excluding the Neo4j.Driver.Experimental namespace).

See also: https://neo4j.com/developer/kb/neo4j-supported-versions/

### Getting the Driver

The Neo4j driver is distributed under three packages:

* [Neo4j.Driver](https://www.nuget.org/packages/Neo4j.Driver/) provides an independent asynchronous driver.
* [Neo4j.Driver.Simple](https://www.nuget.org/packages/Neo4j.Driver.Simple/) for accessing Neo4j via synchronous API.
* [Neo4j.Driver.Reactive](https://www.nuget.org/packages/Neo4j.Driver.Reactive/) for accessing Neo4j via reactive API.

Add the asynchronous driver to your project using the Nuget Package Manager:

```posh
PM> Install-Package Neo4j.Driver
```

There is also a strong named version of each driver package available on Nuget such
as [Neo4j.Driver.Signed](https://www.nuget.org/packages/Neo4j.Driver.Signed). Both packages contain the same version of
the driver, only the latter is strong named. _Consider using the strong named version only if your project is strong
named and/or you are forced to use strong named dependencies._

Add the strong named version of the asynchronous driver to your project using the Nuget Package Manager:

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
    IResultCursor cursor = await session.RunAsync("CREATE (n) RETURN n");
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

* Each `IDriver` instance maintains a pool of connections inside, as a result, it is recommended to only use **one
  driver per application**.
* It is considerably cheap to create new sessions and transactions, as sessions and transactions do not create new
  connections as long as there are free connections available in the connection pool.
* The driver is thread-safe, while the session or the transaction is not thread-safe.

### Parsing Result Values

#### Record Stream

A cypher execution result is comprised of a stream records followed by a result summary.
The records inside the result are accessible via `FetchAsync` and `Current` methods on `IResultCursor`.
Our recommended way to access these result records is to make use of methods provided by `ResultCursorExtensions` such
as `SingleAsync`, `ToListAsync`, and `ForEachAsync`.

Process result records using `ResultCursorExtensions`:

```csharp
IResultCursor cursor = await session.RunAsync("MATCH (a:Person) RETURN a.name as name");
List<string> people = await cursor.ToListAsync(record => record["name"].As<string>());
```

The records are exposed as a record stream in the sense that:

* A record is accessible once it is received by the client. It is not needed for the whole result set to be received
  before it can be visited.
* Each record can only be visited (a.k.a. consumed) once.

Records on a result cannot be accessed if the session or transaction where the result is created has been closed.

#### Value Types

Values in a record are currently exposed as of `object` type.
The underlying types of these values are determined by their Cypher types.

The mapping between driver types and Cypher types are listed in the table bellow:

|  Cypher Type | Driver Type                 
|-------------:|:----------------------------|
|       *null* | null                        |
|         List | IList< object >             |
|          Map | IDictionary<string, object> |
|      Boolean | boolean                     |
|      Integer | long                        |
|        Float | float                       |
|       String | string                      |
|    ByteArray | byte[]                      |
|        Point | Point                       |
|         Node | INode                       |
| Relationship | IRelationship               |
|         Path | IPath                       |

To convert from `object` to the driver type, a helper method `ValueExtensions#As<T>` can be used:

```csharp
IRecord record = await result.SingleAsync();
string name = record["name"].As<string>();
```

#### Temporal Types - Date and Time

The new temporal types in Neo4j 3.4 series are introduced with the 1.6 series of the driver. Considering the nanosecond
precision and large range of supported values,
all temporal types are backed by custom types at the driver level.

The mapping among the Cypher temporal types, driver types, and convertible CLR temporal types - DateTime, TimeSpan and
DateTimeOffset - (via `IConvertible` interface) are as follows:

|  Cypher Type  |  Driver Type  | Convertible CLR Type |
|:-------------:|:-------------:|:--------------------:|
|     Date      |   LocalDate   |       DateTime       |
|     Time      |  OffsetTime   |         ---          |
|   LocalTime   |   LocalTime   |  TimeSpan, DateTime  |
|   DateTime    | ZonedDateTime |    DateTimeOffset    |
| LocalDateTime | LocalDateTime |       DateTime       |
|   Duration    |   Duration    |         ---          |

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

* The conversion to CLR types is possible only when the value fits in the range of the target built-in type.
  A `ValueOverflowException` is thrown
  when the conversion is not possible.
* The Cypher temporal types (excluding `Date`) provide nanosecond precision. However CLR types only support ticks (100
  nanosecond) precision.
  So a temporal type created via Cypher might not be convertible to the CLR type (a `ValueTruncationException` is thrown
  when a conversion is requested in this case).
* `ZonedDateTime` represents date and times with either offset or time zone information. Time zone names adhere to
  the [IANA system](https://www.iana.org/time-zones),
  rather than
  the [Windows system](https://docs.microsoft.com/en-us/windows-hardware/manufacture/desktop/default-time-zones).
  Although there is no support for inbound
  time zone name conversions, a conversion from IANA system to Windows system may be necessary if a conversion
  to `DateTimeOffset` or an access to `Offset` is
  requested by the
  user. [Unicode CLDR mapping](http://cldr.unicode.org/development/development-process/design-proposals/extended-windows-olson-zid-mapping)
  is used for this conversion. Please bear in mind that Windows time zone database do not provide precise historical
  data, so you may end up with inaccurate
  `DateTimeOffset` values for past values. _It is recommended that you use driver level temporal types to avoid these
  inaccuracies._

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

### Migrating from 1.7 to 4.0

This section is for users who would like to migrate their 1.7 driver to 4.0.
The following sections serve as a quick look at what have been added and/or changed in 4.0 .NET driver.
For more information, also checkout
our [Driver Migration Guide](https://neo4j.com/docs/migration-guide/4.0/upgrade-driver/).

#### What's New

* Upcoming version is now named as 4.0.0 instead of 2.0.0 to better align with server versions.
* Bolt V4.0 is implemented in the 4.0.0 driver.
* Reactive API is available under namespace `Neo4j.Driver.Reactive` when using together with Neo4j 4.0 databases.
* Multi-databases support is added. Database can be selected for each session on creation
  with `SessionConfig#ForDatabase`.
* A new feature detection method `IDriver#SupportsMultiDbAsync` is added for querying if the remote database supports
  multi-databases.
* A new `IDriver#VerifyConnectivityAsync` method is introduced for verify the availability of remote DBMS.

#### Breaking Changes

* Encrypted is turned off by default. When encryption is explicitly enabled, the default trust mode is to trust the
  certificates that are trusted by underlying operating system, and hostname verification is enforced by default.
* v1 is removed from drivers' package name. All public APIs are under the namespace `Neo4j.Driver` instead of the
  old `Neo4j.Driver.V1`.
* The `Neo4j.Driver` package contains only the asynchronous API. Synchronous session API has been moved to the
  namespace `Neo4j.Driver.Simple`.
* A new `neo4j` scheme is added and designed to work with all possible 4.0 server deployments. `bolt` scheme is still
  available for explicit direct connections with a single instance and/or a single member in a cluster. For 3.x
  servers, `neo4j` replaces `bolt+routing`.
* Asynchronous methods have been extracted out and put in interfaces prefixed with `IAsync`, whereas synchronous methods
  are kept under the old interface but live in package `Neo4j.Driver.Simple`. This change ensures that blocking and
  no-blocking APIs can never be mixed together.
* `IDriver#Session` methods now make use of a session option builder rather than method arguments.
* Bookmark has changed from a `string` and/or a list of strings to a `Bookmark` object.
* `ITransaction#Success` is replaced with `ITransaction#Commit`.
  However unlike `ITransaction#Success` which only marks the transaction to be successful and then waits
  for `ITransaction#Dispose` to actually perform the real commit, `ITransaction#Commit` commits the transaction
  immediately.
  Similarly, `ITransaction#Failure` is replaced with `ITransaction#Rollback`. A transaction in 4.0 can only be committed
  OR rolled back once.
  If a transaction is not committed explicitly using `ITransaction#Commit`, `ITransaction#Dispose` will roll back the
  transaction.
* `Statement` has been renamed to `Query`. `IStatementResult` has been simplified to `IResult`.
  Similarly, `IStatementResultCursor` has been renamed to `IResultCursor`.
* A result can only be consumed once. A result is consumed if either the query result has been discarded by
  invoking `IResult#Consume` and/or the outer scope where the result is created, such as a transaction or a session, has
  been closed.
  Attempts to access consumed results will be responded with a `ResultConsumedException`.
* `LoadBalancingStrategy` is removed from `Config` class and the drivers always default to `LeastConnectedStrategy`.
* The `IDriverLogger` has been renamed to `ILogger`.
* `TrustStrategy` is replaced with `TrustManager`.

## For Driver Developers

This section targets at people who would like to compile the source code on their own machines for the purpose of, for
example,
contributing a PR to this repository.
Before contributing to this project, please take a few minutes and read
our [Contributing Criteria](https://github.com/neo4j/neo4j-dotnet-driver/blob/1.6/CONTRIBUTING.md).

### Snapshots

Snapshot builds are available at our [MyGet feed](https://www.myget.org/gallery/neo4j-driver-snapshots), add the feed to
your Nuget Sources to access snapshot artifacts.

* [https://www.myget.org/F/neo4j-driver-snapshots/api/v3/index.json](https://www.myget.org/F/neo4j-driver-snapshots/api/v3/index.json)

### Testing

Tests **require** the latest [Testkit 4.3](https://github.com/neo4j-drivers/testkit/tree/4.3), Python3 and Docker.

Testkit is needed to be cloned and configured to run against the Dotnet Driver. Use the following steps to configure
Testkit.

1. Clone the Testkit repository

```
git clone https://github.com/neo4j-drivers/testkit.git
```

2. Under the Testkit folder, install the requirements.

```
pip3 install -r requirements.txt
```

3. Define some enviroment variables to configure Testkit

```
export TEST_DRIVER_NAME=dotnet
export TEST_DRIVER_REPO=<path for the root folder of driver repository>
```

To run test against against some Neo4j version:

```
python3 main.py
```

More details about how to use Teskit could be found
on [its repository](https://github.com/neo4j-drivers/testkit/tree/4.3)

### Building the Source Code on Windows

#### Visual Studio Version

The driver is written in C# 7 so will require Visual Studio 2017.

#### Integration Tests

The integration tests use [boltkit](https://github.com/neo4j-contrib/boltkit) to download and install a database
instance on your local machine.
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

### Building the Source Code on MacOS and/or Linux

The driver targets at .NET Standard 2.0. and .NET 5.0
As a result, it can be compiled and run on linux machines after installing for example .NET Core 2.0 library.
As for IDE, we recommend Rider for daily development.
The integration tests require [boltkit](https://github.com/neo4j-contrib/boltkit) to be installed and accessible via
command line.
If any problem to start a Neo4j Server on your machine, you can start the test Bolt Server yourself at `localhost:7687`
and then set environment variable `DOTNET_DRIVER_USING_LOCAL_SERVER=true`
