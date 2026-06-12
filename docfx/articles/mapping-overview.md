---
uid: mapping-overview
---

# Mapping query results to objects

The Neo4j .NET driver includes a built-in object mapping system that converts Cypher query result records
directly to C# objects. No external libraries or code generation are required.

## Quick start

The simplest case requires no configuration at all — the Cypher column aliases just need to match the C#
property names exactly (the match is **case-sensitive**):

```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

var people = await driver
    .ExecutableQuery("MATCH (p:Person) RETURN p.name AS Name, p.age AS Age")
    .ExecuteAsync()
    .AsObjectsAsync<Person>();

foreach (var person in people)
{
    Console.WriteLine($"{person.Name} is {person.Age} years old.");
}
```

If your database and C# code use different naming conventions (for example camelCase fields and PascalCase
properties), see
[`RecordObjectMapping.TranslateIdentifiers()`](xref:Neo4j.Driver.Mapping.RecordObjectMapping.TranslateIdentifiers(System.Boolean))
to enable automatic translation.

## How the default mapper works

When no custom mapper is registered for a type, the **default mapper** is used. It:

1. Selects a constructor — by default the one with the **fewest parameters**. Mark a specific constructor with
   [`[MappingConstructor]`](xref:Neo4j.Driver.Mapping.MappingConstructorAttribute) to override this.
2. Matches each constructor parameter to a record field by name (**case-sensitive**).
3. After construction, sets any remaining **writable** properties that were not covered by the constructor
   (**case-sensitive** name match). Properties without a setter are always skipped.

For C# `record` types, the primary positional constructor is used automatically, so records work out of the
box.

## Attributes

Attributes in the `Neo4j.Driver.Mapping` namespace let you customise the default mapper without writing any
mapping code.

### [`[MappingSource]`](xref:Neo4j.Driver.Mapping.MappingSourceAttribute) — use a different field name

```csharp
public class Person
{
    [MappingSource("first_name")]
    public string FirstName { get; set; }
}
```

You can also use a **dot-separated path** to read a property from a nested node or dictionary column:

```csharp
public class Person
{
    // Reads the 'Name' property from the 'person' node column in the record
    [MappingSource("person.Name")]
    public string Name { get; set; }
}
```

### [`[MappingOptional]`](xref:Neo4j.Driver.Mapping.MappingOptionalAttribute) — suppress missing-field exceptions

By default the mapper throws if a required field is absent. Mark a property optional to use the type's
default value instead:

```csharp
public class Person
{
    [MappingOptional]
    public string? MiddleName { get; set; }
}
```

### [`[MappingDefaultValue]`](xref:Neo4j.Driver.Mapping.MappingDefaultValueAttribute) — specify a fallback value

Combines optional with a specific fallback value. Implies
[`[MappingOptional]`](xref:Neo4j.Driver.Mapping.MappingOptionalAttribute).

```csharp
public class Person
{
    [MappingDefaultValue("Unknown")]
    public string Name { get; set; }
}
```

### [`[MappingIgnored]`](xref:Neo4j.Driver.Mapping.MappingIgnoredAttribute) — skip a writable property

Properties without a setter are already skipped automatically. Apply
[`[MappingIgnored]`](xref:Neo4j.Driver.Mapping.MappingIgnoredAttribute) to a **writable** property when its
value should not be overwritten by the mapper — for example when it is populated by other means:

```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }

    // Populated after mapping via a separate lookup; must not be overwritten
    [MappingIgnored]
    public IReadOnlyList<string> Roles { get; set; } = [];

    // No setter — already ignored automatically, no attribute needed
    public string DisplayName => $"{Name} (age {Age})";
}
```

### [`[MappingConstructor]`](xref:Neo4j.Driver.Mapping.MappingConstructorAttribute) — choose a constructor

When your type has multiple constructors, mark the one the mapper should use:

```csharp
public class Person
{
    [MappingConstructor]
    public Person(string name, int age) { ... }

    public Person(string name) { ... } // would otherwise be selected (fewer parameters)
}
```

## Mapping graph-native values

Use the [`MappingSource`](xref:Neo4j.Driver.Mapping.MappingSource) enum to read structural graph metadata
rather than a stored property value.

### Node labels

[`MappingSource.NodeLabel`](xref:Neo4j.Driver.Mapping.MappingSource.NodeLabel) reads the labels of a node
column. If the target property is a `string`, the labels are joined with a comma separator. If it is a
collection, each label becomes a separate element.

```csharp
// MATCH (n:Person:Employee) RETURN n

public class GraphNode
{
    [MappingSource("n", MappingSource.NodeLabel)]
    public string Labels { get; set; }           // "Person,Employee"

    [MappingSource("n", MappingSource.NodeLabel)]
    public List<string> LabelList { get; set; }  // ["Person", "Employee"]
}
```

### Relationship type

[`MappingSource.RelationshipType`](xref:Neo4j.Driver.Mapping.MappingSource.RelationshipType) reads the type
string of a relationship column.

```csharp
// MATCH (a)-[r]->(b) RETURN r

public class GraphRelationship
{
    [MappingSource("r", MappingSource.RelationshipType)]
    public string Type { get; set; }  // e.g. "KNOWS"
}
```

## Nested types

If a record field contains a node, relationship, or dictionary, the mapper automatically recurses into it
and maps it to the target property type. No extra configuration is needed — just declare the nested class and
return the whole entity as a column in the query:

```csharp
// MATCH (m:Movie)-[:DIRECTED_BY]->(d:Director)
// RETURN m.Title AS Title, d AS Director

public class Director
{
    public string Name { get; set; }
}

public class Movie
{
    public string Title { get; set; }
    public Director Director { get; set; }
}

var movies = await driver
    .ExecutableQuery(
        "MATCH (m:Movie)-[:DIRECTED_BY]->(d:Director) RETURN m.Title AS Title, d AS Director")
    .ExecuteAsync()
    .AsObjectsAsync<Movie>();
```

The `Director` column is a node, so the mapper wraps its properties in a virtual record and maps them to
`Director` using the same rules as a top-level record. Lists of entities work the same way — each element
is mapped individually to the list's element type.

## Anonymous types — blueprint mapping

To map to an anonymous type, pass an instance as a **blueprint**. The type is inferred from the instance;
its property values are ignored. This gives the benefits of compile-time strong typing, without having to
create a new class for every query executed.

```csharp
var blueprint = new { Name = default(string), Age = default(int) };

var results = await driver
    .ExecutableQuery("MATCH (p:Person) RETURN p.name AS Name, p.age AS Age")
    .ExecuteAsync()
    .AsObjectsFromBlueprintAsync(blueprint);
// results is IReadOnlyList<{ string Name, int Age }>
```

Blueprint variants exist for every mapping method — see [Mapping API methods](#mapping-api-methods) below.

## Inline delegate mapping

For a one-off mapping without any type registration, use a delegate whose **parameter names** match the
record field keys. The driver resolves each parameter by name (case-sensitive) and invokes the delegate.
Overloads are available for 1 to 10 parameters.

```csharp
public class Movie
{
    public string Title { get; set; }
    public string Tagline { get; set; }
}

// Query returns a string, an int, and a Movie node
var lines = await driver
    .ExecutableQuery(
        "MATCH (m:Movie)<-[:DIRECTED]-(d:Person) " +
        "RETURN d.Name AS Director, m.Released AS Released, m AS Movie")
    .ExecuteAsync()
    .AsObjectsAsync(
        (string Director, int Released, Movie Movie) =>
            $"{Movie.Title} ({Released}), directed by {Director}");
// e.g. "The Matrix (1999), directed by Lana Wachowski"
```

If a parameter's type is a class (or any other type the mapping system knows how to map), the value is
fully mapped to that type before the delegate is called. In the example above, `Movie` is a node column
that the driver maps to the `Movie` class automatically — the same rules apply as for any other mapped type.

The same delegate pattern is also available on individual records and on `IAsyncEnumerable<IRecord>` cursors
— see [Mapping API methods](#mapping-api-methods) below.

## Mapping API methods

The mapping API is spread across several extension classes depending on what you are mapping from. All
methods use the global mapping configuration registered with
[`RecordObjectMapping`](xref:Neo4j.Driver.Mapping.RecordObjectMapping).

### Single record — [`RecordExtensions`](xref:Neo4j.Driver.Mapping.RecordExtensions)

Use these when iterating records manually inside a session or transaction:

| Method | Returns | Notes |
|--------|---------|-------|
| [`AsObject<T>()`](xref:Neo4j.Driver.Mapping.RecordExtensions.AsObject``1(Neo4j.Driver.IRecord)) | `T` | Maps the record to type `T` |
| [`AsObjectFromBlueprint<T>(T)`](xref:Neo4j.Driver.Mapping.RecordExtensions.AsObjectFromBlueprint``1(Neo4j.Driver.IRecord,``0)) | `T` | Blueprint variant — infers type from instance |
| [`AsObject<TResult,T1,…>(Func<…>)`](xref:Neo4j.Driver.Mapping.DelegateMappingRecordExtensions) | `TResult` | Delegate variant — 1–10 typed parameters |

### ExecutableQuery results — [`ExecutableQueryMappingExtensions`](xref:Neo4j.Driver.Mapping.ExecutableQueryMappingExtensions)

Chain these after `.ExecuteAsync()` to map all results at once:

```csharp
// Typed mapping
var people = await driver
    .ExecutableQuery("MATCH (p:Person) RETURN p.name AS Name, p.age AS Age")
    .ExecuteAsync()
    .AsObjectsAsync<Person>();

// Blueprint mapping (anonymous types)
var blueprint = new { Name = default(string), Age = default(int) };
var anon = await driver
    .ExecutableQuery("MATCH (p:Person) RETURN p.name AS Name, p.age AS Age")
    .ExecuteAsync()
    .AsObjectsFromBlueprintAsync(blueprint);
```

[`DelegateExecutableQueryMappingExtensions`](xref:Neo4j.Driver.Mapping.DelegateExecutableQueryMappingExtensions)
provides delegate overloads (1–10 parameters) on the same `Task` target — see the [inline delegate
mapping](#inline-delegate-mapping) section above for a usage example.

### Session/transaction cursor — [`AsyncEnumerableExtensions`](xref:Neo4j.Driver.Mapping.AsyncEnumerableExtensions)

Use these on the `IAsyncEnumerable<IRecord>` returned by `RunAsync` or a transaction function:

```csharp
await session.ExecuteReadAsync(async tx =>
{
    var cursor = await tx.RunAsync(
        "MATCH (p:Person) RETURN p.name AS Name, p.age AS Age");

    // Buffer all results into a list
    IReadOnlyList<Person> people = await cursor.ToListAsync<Person>();

    // Or stream lazily without buffering
    await foreach (var person in cursor.AsObjectsAsync<Person>())
    {
        Console.WriteLine(person.Name);
    }
});
```

`ToListAsync<T>()` materialises all records into an `IReadOnlyList<T>`. `AsObjectsAsync<T>()` returns a
lazy `IAsyncEnumerable<T>` that maps each record on demand — useful for large result sets where you do not
want to buffer everything in memory.

Blueprint variants
([`ToListFromBlueprintAsync<T>()`](xref:Neo4j.Driver.Mapping.AsyncEnumerableExtensions.ToListFromBlueprintAsync``1(System.Collections.Generic.IAsyncEnumerable{Neo4j.Driver.IRecord},``0,System.Threading.CancellationToken))
and
[`AsObjectsFromBlueprintAsync<T>()`](xref:Neo4j.Driver.Mapping.AsyncEnumerableExtensions.AsObjectsFromBlueprintAsync``1(System.Collections.Generic.IAsyncEnumerable{Neo4j.Driver.IRecord},``0,System.Threading.CancellationToken))
and delegate overloads
([`DelegateAsyncEnumerableExtensions`](xref:Neo4j.Driver.Mapping.DelegateAsyncEnumerableExtensions))
follow the same patterns described above.

## See also

- [Configuring the mapping system](mapping-configuration.md) — naming convention translation, custom mappers,
  type converters, and the fluent builder API
