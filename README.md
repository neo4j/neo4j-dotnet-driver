# Neo4j .NET Driver

This repository contains the official [Neo4j](https://neo4j.com/) driver for .NET.

### [API Docs](https://neo4j.com/docs/api/dotnet-driver/current/) | [Driver Manual](https://neo4j.com/docs/dotnet-manual/current/) | [Change Log](https://github.com/neo4j/neo4j-dotnet-driver/wiki/6.X-Change-Log)

For contribution guidance, see [Contributing](./CONTRIBUTING.md).

## Installation

```
dotnet add package Neo4j.Driver
```

The package targets `.NET 8`, `.NET 9`, and `.NET 10`.

`Neo4j.Driver` is strong-named by default. Unlike previous major versions, there is no separate `Neo4j.Driver.Signed` package.

### Additional packages

[Neo4j.Driver.Reactive](https://www.nuget.org/packages/Neo4j.Driver.Reactive/) provides reactive (`IObservable`-based) APIs built on top of the core driver:

```
dotnet add package Neo4j.Driver.Reactive
```

### Versioning

Driver upgrades within a major version will not contain breaking API changes, with the exception of the `Neo4j.Driver.Preview` namespace, which is reserved for feature previews.

## Getting Started

### URI schemes

Use `bolt://` for a direct connection to a single server, or `neo4j://` for a routing connection (required for clusters and recommended for [Neo4j Aura](https://neo4j.com/cloud/platform/aura-graph-database/)). Append `+s` to require TLS (e.g. `neo4j+s://`), or `+ssc` to allow self-signed certificates.

### Creating a driver

```csharp
using Neo4j.Driver;

await using var driver = GraphDatabase.Driver(
    "neo4j+s://<dbid>.databases.neo4j.io",
    AuthTokens.Basic("neo4j", "<password>"));
```

`IDriver` maintains a connection pool internally. Create a single instance per application and share it across threads — `IDriver` is thread-safe. Sessions and transactions are not thread-safe and should not be shared across threads.

### Verifying connectivity

```csharp
await driver.VerifyConnectivityAsync();
```

Throws an exception if the server is unreachable or the credentials are invalid.

### Always specify a database

```csharp
var config = new QueryConfig(database: "neo4j");
```

Specifying the database avoids a server round-trip to determine the home database. Omitting it is only appropriate when working against a single-database deployment where the database name is genuinely unknown.

## Querying

### ExecutableQuery — single-query transactions

`ExecutableQuery` is the most concise API for running a single query in its own transaction. It handles retries and result materialisation automatically.

`ExecuteAsync` returns an `EagerResult<IReadOnlyList<IRecord>>` containing all records (`Result`), the returned column names (`Keys`), and a query summary (`Summary`). It can be destructured directly:

```csharp
var (records, summary, keys) = await driver
    .ExecutableQuery("MATCH (n:Movie) RETURN n.title, n.released")
    .WithConfig(new QueryConfig(database: "neo4j"))
    .ExecuteAsync();
```

To map results directly to a type, chain `AsObjectsAsync<T>()`:

```csharp
record Movie(string title, int released);

var movies = await driver
    .ExecutableQuery("MATCH (n:Movie) RETURN n.title, n.released")
    .WithConfig(new QueryConfig(database: "neo4j"))
    .ExecuteAsync()
    .AsObjectsAsync<Movie>();

foreach (var movie in movies)
    Console.WriteLine(movie);
```

### Managed transaction functions

For multi-query transactions, or when you need explicit control over read/write routing, use `ExecuteReadAsync` or `ExecuteWriteAsync` on a session. The driver automatically retries the work on transient failures.

```csharp
await using var session = driver.AsyncSession(o => o.WithDatabase("neo4j"));

// Read transaction
var names = await session.ExecuteReadAsync(async tx =>
{
    var cursor = await tx.RunAsync("MATCH (p:Person) RETURN p.name AS name");
    return await cursor.ToListAsync(r => r["name"].As<string>());
});

// Write transaction
await session.ExecuteWriteAsync(tx =>
    tx.RunAsync("CREATE (:Person {name: $name})", new { name = "Alice" }));
```

### Manual transactions

Use `BeginTransactionAsync` when you need to co-ordinate a transaction with external work, such as committing only after a side-effect succeeds.

```csharp
await using var session = driver.AsyncSession(o => o.WithDatabase("neo4j"));
await using var tx = await session.BeginTransactionAsync();
try
{
    await tx.RunAsync("CREATE (:Person {name: $name})", new { name = "Bob" });
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

## Types

Values in a record are exposed as `object`. Use the `As<T>()` extension method to convert to the expected type:

```csharp
string name = record["name"].As<string>();
long count = record["count"].As<long>();
```

### Cypher to .NET type mapping

| Cypher Type | .NET Driver Type |
|---:|:---|
| *null* | `null` |
| List | `IList<object>` |
| Map | `IDictionary<string, object>` |
| Boolean | `bool` |
| Integer | `long` |
| Float | `double` |
| String | `string` |
| ByteArray | `byte[]` |
| Point | `Point` |
| Node | `INode` |
| Relationship | `IRelationship` |
| Path | `IPath` |

### Temporal types

| Cypher Type | Driver Type | Convertible CLR Types |
|:---:|:---:|:---:|
| Date | `LocalDate` | `DateTime`, `DateOnly` (.NET 6+) |
| Time | `OffsetTime` | `TimeOnly` (.NET 6+) |
| LocalTime | `LocalTime` | `TimeSpan`, `DateTime` |
| DateTime | `ZonedDateTime` | `DateTimeOffset` |
| LocalDateTime | `LocalDateTime` | `DateTime` |
| Duration | `Duration` | — |

## Support

- **Documentation**: [neo4j.com/docs/dotnet-manual](https://neo4j.com/docs/dotnet-manual/current/)
- **Community forum**: [community.neo4j.com](https://community.neo4j.com/)
- **Stack Overflow**: [stackoverflow.com/questions/tagged/neo4j](https://stackoverflow.com/questions/tagged/neo4j)
- **Bug reports / feature requests**: [GitHub Issues](https://github.com/neo4j/neo4j-dotnet-driver/issues)
- **Enterprise support**: [support.neo4j.com](https://support.neo4j.com/)
