---
uid: mapping-configuration
---

# Configuring the mapping system

This article covers startup configuration for the object mapping system: naming convention translation, the
fluent builder API, custom mapper implementations, and type converters.

For day-to-day usage and a feature overview, see [Mapping query results to objects](mapping-overview.md).

## Naming convention translation

By default the mapper matches C# property names to record field names by an **exact, case-sensitive** comparison.
If your database uses a different naming convention — for example camelCase fields vs. PascalCase properties —
you can configure automatic translation once at startup using
[`RecordObjectMapping.TranslateIdentifiers`](xref:Neo4j.Driver.Mapping.RecordObjectMapping.TranslateIdentifiers(System.Boolean)).

The most common case (C# PascalCase or camelCase → database camelCase) requires no arguments:

```csharp
// At startup, before any queries are run
RecordObjectMapping.TranslateIdentifiers();
```

After this call, a property named `FirstName` automatically looks up the record field `firstName`, and so on.

### Choosing a convention

Use the overloads that accept
[`IdentifierCaseConvention`](xref:Neo4j.Driver.Mapping.ConventionTranslation.IdentifierCaseConvention) and/or
[`FieldCaseConvention`](xref:Neo4j.Driver.Mapping.ConventionTranslation.FieldCaseConvention) for other
combinations. `IdentifierCaseConvention` describes how **C# identifiers** (property and parameter names) are
split into tokens; `FieldCaseConvention` describes how those tokens are joined to form a **database field
name** for record lookup.

| Scenario | C# convention | DB convention | Example call |
|---|---|---|---|
| Typical C# property names (PascalCase or camelCase) | `CSharpIdentifier` (built into the parameterless overload) | `camelCase` | `TranslateIdentifiers()` |
| Typical C# property names | `CSharpIdentifier` (built into the overload) | `snake_case` | `TranslateIdentifiers(FieldCaseConvention.SnakeCase)` |
| PascalCase property names | `PascalCase` | `SCREAMING_SNAKE` | `TranslateIdentifiers(IdentifierCaseConvention.PascalCase, FieldCaseConvention.ScreamingSnakeCase)` |
| camelCase property names | `camelCase` | `kebab-case` | `TranslateIdentifiers(IdentifierCaseConvention.CamelCase, FieldCaseConvention.KebabCase)` |

`IdentifierCaseConvention` values: `CSharpIdentifier`, `CamelCase`, `PascalCase`, `SnakeCase`,
`ScreamingSnakeCase`, `KebabCase`.

`FieldCaseConvention` values: `CamelCase`, `PascalCase`, `SnakeCase`, `ScreamingSnakeCase`, `KebabCase`.

### Applying translation to Cypher parameters

When a C# object is passed as a query parameter, each property name becomes a Cypher parameter key. Pass
`translateCypherParameters: true` so those keys are produced with the same **C# identifier → database field
name** translation as record mapping:

```csharp
RecordObjectMapping.TranslateIdentifiers(translateCypherParameters: true);
```

This keeps naming consistent when reading results and when binding parameters, without extra attributes.

### Bypassing translation for specific members

Properties and parameters decorated with
[`[MappingSource]`](xref:Neo4j.Driver.Mapping.MappingSourceAttribute) are treated as explicit
database field names and bypass global translation entirely.

### Custom convention translator

If none of the built-in conventions cover your naming scheme, compose
[`IIdentifierParser<T>`](xref:Neo4j.Driver.Mapping.ConventionTranslation.IIdentifierParser`1) and
[`IFieldFormatter<T>`](xref:Neo4j.Driver.Mapping.ConventionTranslation.IFieldFormatter`1) and register them with
[`TranslateIdentifiers<TParseResult>(...)`](xref:Neo4j.Driver.Mapping.RecordObjectMapping.TranslateIdentifiers``1(Neo4j.Driver.Mapping.ConventionTranslation.IIdentifierParser{``0},Neo4j.Driver.Mapping.ConventionTranslation.IFieldFormatter{``0},System.Boolean)).
[`ConventionTranslator<T>`](xref:Neo4j.Driver.Mapping.ConventionTranslation.ConventionTranslator`1) implements
[`IConventionTranslator`](xref:Neo4j.Driver.Mapping.ConventionTranslation.IConventionTranslator) and is what the
built-in overloads use internally; you only need that type if you want to wrap or test translation in isolation.

Example: every column in the database uses a `db_` prefix and `snake_case`, while C# properties stay in normal
PascalCase (`PersonId` → `db_person_id`). Delegate tokenization and casing to the built-in snake-case path,
then add the prefix:

```csharp
using System.Collections.Generic;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Mapping.ConventionTranslation;

public sealed class DbPrefixedSnakeCaseFormatter : IFieldFormatter<IEnumerable<string>>
{
    private readonly IFieldFormatter<IEnumerable<string>> _inner =
        new StandardCaseFormatter(FieldCaseConvention.SnakeCase);

    public string Format(IEnumerable<string> data) => "db_" + _inner.Format(data);
}

// At startup:
RecordObjectMapping.TranslateIdentifiers(
    new StandardCaseParser(IdentifierCaseConvention.CSharpIdentifier),
    new DbPrefixedSnakeCaseFormatter());
```

---

## Fluent builder API

The fluent builder is the recommended way to configure multiple type mappings without implementing
`IRecordMapper<T>` by hand.

### Implementing a mapping provider

Create a class that implements
[`IMappingProvider`](xref:Neo4j.Driver.Mapping.IMappingProvider) and call
[`IMappingRegistry.RegisterMapping<T>`](xref:Neo4j.Driver.Mapping.IMappingRegistry.RegisterMapping``1(System.Action{Neo4j.Driver.Mapping.IMappingBuilder{``0}}))
for each type:

```csharp
public class MyMappingProvider : IMappingProvider
{
    public void CreateMappers(IMappingRegistry registry)
    {
        registry.RegisterMapping<Person>(b => b
            .UseDefaultMapping()
            .Map(p => p.Labels, "person", MappingSource.NodeLabel));

        registry.RegisterMapping<Address>(b => b
            .Map(a => a.Street, "street")
            .Map(a => a.City, "city")
            .Map(a => a.PostCode, "postcode"));
    }
}
```

Register the provider once at startup:

```csharp
RecordObjectMapping.RegisterProvider<MyMappingProvider>();
// or, if construction arguments are needed:
RecordObjectMapping.RegisterProvider(new MyMappingProvider(someArg));
```

### Builder methods

#### `UseDefaultMapping()`

Applies the default mapper first and then lets subsequent `Map(...)` calls override specific properties:

```csharp
registry.RegisterMapping<Person>(b => b
    .UseDefaultMapping()
    .Map(p => p.Labels, "person", MappingSource.NodeLabel));
```

#### `Map(property, recordKey, ...)`

Maps a specific property from a named record field, with optional `MappingSource`, inline converter, and
optionality flag:

```csharp
registry.RegisterMapping<Movie>(b => b
    .Map(m => m.Title, "title")
    .Map(m => m.ReleaseYear, "released")
    .Map(m => m.Genre, "genre", optional: true));
```

#### `Map(property, Func<IRecord, object>)`

Maps a property from an arbitrary function over the record, giving full access to all fields:

```csharp
registry.RegisterMapping<Movie>(b => b
    .Map(m => m.DisplayTitle, r => $"{r["title"].As<string>()} ({r["released"].As<int>()})"));
```

#### `MapWholeObject(Func<IRecord, T>)`

Replaces the default construction logic entirely. Use when the object's constructor arguments require complex
derivation from the record:

```csharp
registry.RegisterMapping<Address>(b => b
    .MapWholeObject(r => new Address(
        r["street"].As<string>(),
        r["city"].As<string>(),
        r["postcode"].As<string>())));
```

Do not combine `MapWholeObject` with `UseDefaultMapping` — `MapWholeObject` replaces the entire construction
pipeline.

---

## Custom mapper (`IRecordMapper<T>`)

For complete programmatic control, implement
[`IRecordMapper<T>`](xref:Neo4j.Driver.Mapping.IRecordMapper`1) and register it directly:

```csharp
public class PersonMapper : IRecordMapper<Person>
{
    public Person Map(IRecord record)
    {
        return new Person
        {
            Name = record["name"].As<string>(),
            Age = record["age"].As<int>()
        };
    }
}

// At startup:
RecordObjectMapping.Register(new PersonMapper());
```

The registered mapper replaces the default mapper for `Person` globally. Prefer this approach when the
mapping logic is complex, needs to be tested in isolation, or needs to share state across records.

---

## Type converters

Register a global conversion function when a record field value's runtime type does not match the target
property type and the driver's built-in type coercion (via
[`ValueExtensions.As<T>`](xref:Neo4j.Driver.ValueExtensions)) does not handle it:

```csharp
// Convert a string record value to a Uri
RecordObjectMapping.RegisterTypeConverter<string, Uri>(s => new Uri(s));
```

Type converters apply globally to all type mappings. If multiple converters are registered for the same pair
of types, the most recently registered one wins.

### Built-in converters

The following converters are registered automatically:

- `string` → `Guid` (via `Guid.Parse`)
- `Vector` → `List<T>`, `IList<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`,
  `ICollection<T>` for supported vector types

---

## Configuration order

Call all startup configuration before running any queries. The recommended order is:

1. `RecordObjectMapping.TranslateIdentifiers(...)` — if using convention translation
2. `RecordObjectMapping.RegisterProvider(...)` — for fluent builder mappings
3. `RecordObjectMapping.Register(...)` — for hand-written `IRecordMapper<T>` implementations
4. `RecordObjectMapping.RegisterTypeConverter(...)` — for custom type conversions

All configuration is global and thread-safe for concurrent reads after initial setup.

## See also

- [Mapping query results to objects](mapping-overview.md) — feature overview, attributes, and common usage
  patterns
