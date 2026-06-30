// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Neo4j.Driver.Internal.Mapping;
using Neo4j.Driver.Internal.Mapping.ConventionTranslation;
using Neo4j.Driver.Internal.Mapping.TypeConversion;
using Neo4j.Driver.Mapping.ConventionTranslation;

namespace Neo4j.Driver.Mapping;

/// <summary>Contains methods for registering a mapping with the global mapping configuration.</summary>
public interface IMappingRegistry
{
    /// <summary>Registers a mapping for the given type.</summary>
    /// <param name="mappingBuilder">
    /// This method will be called, passing a parameter that contains a fluent API for defining
    /// the mapping.
    /// </param>
    /// <typeparam name="T">The type to be mapped.</typeparam>
    /// <returns>This instance for method chaining.</returns>
    IMappingRegistry RegisterMapping<T>(Action<IMappingBuilder<T>> mappingBuilder);
}

internal interface IRecordObjectMapping : IMappingRegistry
{
    object Map(IRecord record, Type type);
    TResult MapFromBlueprint<TResult>(IRecord record, TResult blueprint);
    IMappingTypeConversionManager TypeConversionManager { get; }
    void RegisterTypeConverter<TFrom, TTo>(Func<TFrom, TTo> converter);
    MethodInfo GetMapMethodForType(Type type);
    void TranslateIdentifiers(IConventionTranslator conventionTranslator, bool translateCypherParameters = false);
}

internal delegate object MapDelegate(IRecord record);

/// <summary>Controls global record mapping configuration.</summary>
/// <remarks>
/// <para>
/// The object mapping system converts <see cref="IRecord"/> query results into C# objects. The simplest usage
/// requires no configuration at all: call <see cref="RecordExtensions.AsObject{T}"/> on any record, or chain
/// <see cref="ExecutableQueryMappingExtensions.AsObjectsAsync{T}"/> onto an
/// <see cref="IDriver.ExecutableQuery(string)"/> call, and the default mapper will do the rest.
/// </para>
/// <para>
/// The default mapper automatically selects a constructor (preferring the one with fewest parameters, or the one
/// marked <see cref="MappingConstructorAttribute"/>), then populates any remaining writable properties.
/// Property and parameter names are matched <b>case-sensitively</b> against record field names. Decorate members with
/// attributes in the <c>Neo4j.Driver.Mapping</c> namespace to customise field names, optionality, and default
/// values without writing any mapping code.
/// </para>
/// <para>
/// When your database uses a different naming convention from your C# code (for example camelCase fields vs.
/// PascalCase properties), call <see cref="TranslateIdentifiers(bool)"/> once at startup to configure automatic
/// name translation.
/// </para>
/// <para>
/// For types that need more control than attributes provide, use one of the global registration methods:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="RegisterProvider(IMappingProvider)"/> — register a class that uses the fluent
/// <see cref="IMappingBuilder{TObject}"/> API to define per-type mappings.
/// </description></item>
/// <item><description>
/// <see cref="Register{T}(IRecordMapper{T})"/> — register a hand-written <see cref="IRecordMapper{T}"/>
/// implementation for complete control over a single type.
/// </description></item>
/// <item><description>
/// <see cref="RegisterTypeConverter{TFrom,TTo}"/> — register a conversion function used when a field value's
/// runtime type does not match a target property type.
/// </description></item>
/// </list>
/// <para>
/// All configuration is global and takes effect immediately. This class is thread-safe for concurrent reads after
/// initial configuration, but registration methods should be called during application startup, not from concurrent
/// code.
/// </para>
/// <para>
/// See the conceptual guides
/// <a href="~/articles/mapping-overview.md">Mapping query results to objects</a> and
/// <a href="~/articles/mapping-configuration.md">Configuring the mapping system</a>.
/// </para>
/// </remarks>
public class RecordObjectMapping : IRecordObjectMapping
{
    private readonly ConcurrentDictionary<Type, MethodInfo> _mapMethods = new();
    private readonly ConcurrentDictionary<Type, object> _mappers = new();
    private readonly IMappingTypeConversionManager _typeConversionManager = new MappingTypeConversionManager();
    private IDefaultConverters _defaultConverters;
    private IConventionTranslator _conventionTranslator = new NoOpConventionTranslator();
    private bool _translateCypherParameterNames;

    private RecordObjectMapping()
    {
        _defaultConverters = new DefaultConverters(_typeConversionManager);
        _defaultConverters.Register();
    }

    internal static readonly RecordObjectMapping Instance = new();

    IMappingRegistry IMappingRegistry.RegisterMapping<T>(Action<IMappingBuilder<T>> mappingBuilder)
    {
        var builder = new MappingBuilder<T>();
        mappingBuilder(builder);
        var mapper = builder.Build();
        Register(mapper);
        return this;
    }

    object IRecordObjectMapping.Map(IRecord record, Type type)
    {
        var mapMethod = Instance.GetMapMethodForType(type);
        var mapperForType = GetMapperForType(type);
        var mapDelegate = (MapDelegate)mapMethod.CreateDelegate(typeof(MapDelegate), mapperForType);

        try
        {
            return mapDelegate(record);
        }
        catch (Exception ex)
        {
            var inner = ex is TargetInvocationException tie ? tie.InnerException : ex;
            throw new MappingFailedException($"Failed to map record to type {type.Name}.", inner);
        }
    }

    T IRecordObjectMapping.MapFromBlueprint<T>(IRecord record, T blueprint)
    {
        return (T)Map(record, typeof(T));
    }

    IMappingTypeConversionManager IRecordObjectMapping.TypeConversionManager => _typeConversionManager;

    void IRecordObjectMapping.RegisterTypeConverter<TFrom, TTo>(Func<TFrom, TTo> converter)
    {
        _typeConversionManager.RegisterConverter(converter);
    }

    /// <summary>
    /// Registers a type converter. This will replace any existing converter for the same type.
    /// </summary>
    /// <param name="converter">The function that will convert from <typeparamref name="TFrom"/> to
    /// <typeparamref name="TTo"/>.</param>
    /// <typeparam name="TFrom">The type to convert from.</typeparam>
    /// <typeparam name="TTo">The type to convert to.</typeparam>
    public static void RegisterTypeConverter<TFrom, TTo>(Func<TFrom, TTo> converter)
    {
        ((IRecordObjectMapping)Instance).RegisterTypeConverter(converter);
    }

    internal string GetTranslatedRecordIdentifier(string objectIdentifier)
    {
        return _conventionTranslator.Translate(objectIdentifier);
    }

    internal string GetTranslatedRecordPath(string path)
    {
        if (path is null || path.IndexOf('.') < 0)
        {
            return GetTranslatedRecordIdentifier(path);
        }

        var segments = path.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = GetTranslatedRecordIdentifier(segments[i]);
        }

        return string.Join(".", segments);
    }

    internal string GetTranslatedCypherParameterName(string propertyName)
    {
        return _translateCypherParameterNames 
            ? _conventionTranslator.Translate(propertyName) 
            : propertyName;
    }

    /// <summary>
    /// Uses the supplied <see cref="IConventionTranslator"/> to translate each C# identifier to the matching
    /// database field name.
    /// </summary>
    /// <param name="conventionTranslator">The translator implementation.</param>
    /// <param name="translateCypherParameters">
    /// When <c>true</c>, also translates C# property names to database field names when objects are used as
    /// Cypher query parameters. Defaults to <c>false</c>.
    /// </param>
    void IRecordObjectMapping.TranslateIdentifiers(
        IConventionTranslator conventionTranslator,
        bool translateCypherParameters)
    {
        _conventionTranslator = conventionTranslator;
        _translateCypherParameterNames = translateCypherParameters;

        // default mappers bake the translation config into their used-source dedup keys at build time and are
        // cached per type, so if translation config changes the whole cache is invalidated.
        DefaultMapper.Reset();
    }

    private static void TranslateIdentifiers(IConventionTranslator conventionTranslator, bool translateCypherParameters)
    {
        ((IRecordObjectMapping)Instance).TranslateIdentifiers(conventionTranslator, translateCypherParameters);
    }

    /// <summary>
    /// Uses the supplied <see cref="IdentifierCaseConvention"/> to translate identifiers from the
    /// specified convention to camelCase database identifiers.
    /// </summary>
    /// <param name="identifierConvention">The convention to use for parsing the identifiers.</param>
    /// <param name="translateCypherParameters">Whether to translate names of object properties to be
    /// used as Cypher parameters.</param>
    public static void TranslateIdentifiers(IdentifierCaseConvention identifierConvention,
        bool translateCypherParameters = false)
    {
        var translator = new ConventionTranslator<IEnumerable<string>>(
            new StandardCaseParser(identifierConvention),
            new StandardCaseFormatter(FieldCaseConvention.CamelCase));
        TranslateIdentifiers(translator, translateCypherParameters);
    }

    /// <summary>
    /// Uses the supplied <see cref="FieldCaseConvention"/> to translate identifiers from standard
    /// C# identifiers to the specified database field naming convention.
    /// </summary>
    public static void TranslateIdentifiers(FieldCaseConvention fieldConvention, bool translateCypherParameters = false)
    {
        var translator = new ConventionTranslator<IEnumerable<string>>(
            new StandardCaseParser(IdentifierCaseConvention.CSharpIdentifier),
            new StandardCaseFormatter(fieldConvention));
        TranslateIdentifiers(translator, translateCypherParameters);
    }

    /// <summary>
    /// Translates identifiers using the specified <see cref="IdentifierCaseConvention"/> and <see cref="FieldCaseConvention"/>.
    /// </summary>
    /// <param name="identifierConvention">The convention to use for parsing the identifiers.</param>
    /// <param name="fieldConvention">The convention to use for formatting the record fields.</param>
    /// <param name="translateCypherParameters">Whether to translate names of object properties to be
    /// used as Cypher parameters.</param>
    public static void TranslateIdentifiers(
        IdentifierCaseConvention identifierConvention,
        FieldCaseConvention fieldConvention,
        bool translateCypherParameters = false)
    {
        var translator = new ConventionTranslator<IEnumerable<string>>(
            new StandardCaseParser(identifierConvention),
            new StandardCaseFormatter(fieldConvention));

        TranslateIdentifiers(translator, translateCypherParameters);
    }

    /// <summary>
    /// Translates identifiers using the default configuration.
    /// By default, it uses the <see cref="IdentifierCaseConvention.CSharpIdentifier"/> for parsing object identifiers
    /// and the <see cref="FieldCaseConvention.CamelCase"/> for formatting record fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this method once at application startup if your database uses camelCase field names and your C# types
    /// use either camelCase or PascalCase property names (the most common scenario). After calling this method,
    /// a property named <c>FirstName</c> will automatically look up the record field <c>firstName</c>, and so on.
    /// </para>
    /// <para>
    /// For other combinations of naming conventions, use one of the overloads that accept
    /// <see cref="ConventionTranslation.IdentifierCaseConvention"/> and/or
    /// <see cref="ConventionTranslation.FieldCaseConvention"/> to specify both ends of the translation.
    /// </para>
    /// <para>
    /// When <paramref name="translateCypherParameters"/> is <c>true</c>, the same translation is applied in the
    /// reverse direction when a C# object is passed as a query parameter — property names are translated to the
    /// database naming convention so parameter names stay consistent with field names.
    /// </para>
    /// <para>
    /// Properties or parameters decorated with <see cref="MappingSourceAttribute"/> bypass translation
    /// entirely, because their path is treated as an explicit database field name.
    /// </para>
    /// </remarks>
    /// <param name="translateCypherParameters">
    /// When <c>true</c>, also translates C# property names to database-style names when objects are used as
    /// Cypher query parameters. Defaults to <c>false</c>.
    /// </param>
    public static void TranslateIdentifiers(bool translateCypherParameters = false)
    {
        var translator = new ConventionTranslator<IEnumerable<string>>(
            new StandardCaseParser(IdentifierCaseConvention.CSharpIdentifier),
            new StandardCaseFormatter(FieldCaseConvention.CamelCase));
        TranslateIdentifiers(translator, translateCypherParameters);
    }

    /// <summary>
    /// Translates identifiers using a custom object identifier parser and record field formatter.
    /// </summary>
    /// <typeparam name="TParseResult">The type of data returned by the parse.</typeparam>
    /// <param name="objectIdentifierParser">The parser used to parse object identifiers.</param>
    /// <param name="recordFieldFormatter">The formatter used to format record fields.</param>
    /// <param name="translateCypherParameters">Whether to translate names of object properties to be
    /// used as Cypher parameters.</param>
    public static void TranslateIdentifiers<TParseResult>(
        IIdentifierParser<TParseResult> objectIdentifierParser,
        IFieldFormatter<TParseResult> recordFieldFormatter,
        bool translateCypherParameters = false)
    {
        var translator = new ConventionTranslator<TParseResult>(objectIdentifierParser, recordFieldFormatter);
        TranslateIdentifiers(translator, translateCypherParameters);
    }

    internal static void Reset()
    {
        // clear all registered mappers, type converters and convention translator
        Instance._mappers.Clear();
        Instance._mapMethods.Clear();
        Instance._typeConversionManager.Clear();
        Instance._conventionTranslator = new NoOpConventionTranslator();
        Instance._defaultConverters = new DefaultConverters(Instance._typeConversionManager);
        Instance._defaultConverters.Register();
        DefaultMapper.Reset();
    }

    /// <summary>Registers a single record mapper. This will replace any existing mapper for the same type.</summary>
    /// <param name="mapper">The mapper. This must implement <see cref="IRecordMapper{T}"/> for the type to be mapped.</param>
    /// <exception cref="ArgumentException">The provided <paramref name="mapper"/> does not implement IRecordMapper{T}.</exception>
    public static void Register<T>(IRecordMapper<T> mapper)
    {
        Instance._mappers[typeof(T)] = mapper;
    }

    private static object GetMapperForType(Type type)
    {
        if (Instance._mappers.TryGetValue(type, out var m))
        {
            return m;
        }

        // no mapper registered for this type, so use the default mapper
        var openGenericMethod = typeof(DefaultMapper).GetMethod(nameof(DefaultMapper.Get));
        var closedGenericMethod = openGenericMethod!.MakeGenericMethod(type);
        return closedGenericMethod.Invoke(null, [null]);
    }

    /// <summary>Maps a record to an object of the given type according to the global mapping configuration.</summary>
    /// <remarks>
    /// <para>
    /// See
    /// <a href="~/articles/mapping-overview.md">Mapping query results to objects</a> and
    /// <a href="~/articles/mapping-configuration.md">Configuring the mapping system</a>.
    /// </para>
    /// </remarks>
    /// <param name="record">The record to be mapped.</param>
    /// <typeparam name="T">The type of object to be mapped.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T Map<T>(IRecord record)
    {
        var mapper = (IRecordMapper<T>)GetMapperForType(typeof(T));
        return mapper.Map(record);
    }

    /// <summary>
    /// Registers a mapping provider. This will call <see cref="IMappingProvider.CreateMappers"/> on the provider,
    /// allowing it to register any mappers it wishes.
    /// </summary>
    /// <remarks>
    /// Use this overload when your <see cref="IMappingProvider"/> implementation has a public parameterless
    /// constructor. For providers that require construction arguments, use
    /// <see cref="RegisterProvider(IMappingProvider)"/> instead.
    /// </remarks>
    /// <typeparam name="T">The type of the mapping provider.</typeparam>
    public static void RegisterProvider<T>() where T : IMappingProvider, new()
    {
        RegisterProvider(new T());
    }

    /// <summary>
    /// Registers a mapping provider. This will call <see cref="IMappingProvider.CreateMappers"/> on the provider,
    /// allowing it to register any mappers it wishes.
    /// </summary>
    /// <remarks>
    /// A mapping provider is the recommended way to register multiple type mappings at once using the fluent
    /// <see cref="IMappingBuilder{TObject}"/> API. Implement <see cref="IMappingProvider"/> and call
    /// <see cref="IMappingRegistry.RegisterMapping{T}"/> for each type you want to map inside
    /// <see cref="IMappingProvider.CreateMappers"/>, then pass the provider to this method at startup.
    /// </remarks>
    /// <param name="provider">The provider instance whose mappers will be registered.</param>
    public static void RegisterProvider(IMappingProvider provider)
    {
        provider.CreateMappers(Instance);
    }

    /// <summary>
    /// Gets the map method for the given type.
    /// </summary>
    /// <param name="type">The type to get the map method for.</param>
    /// <returns>The map method.</returns>
    public MethodInfo GetMapMethodForType(Type type)
    {
        return _mapMethods.GetOrAdd(type, GetMapMethod);

        MethodInfo GetMapMethod(Type t)
        {
            var typedInterface = typeof(IRecordMapper<>).MakeGenericType(t);
            var methodInfo = typedInterface.GetMethod(nameof(IRecordMapper<object>.Map));
            return methodInfo;
        }
    }

    /// <summary>Maps a record to an object of the given type according to the global mapping configuration.</summary>
    /// <remarks>
    /// <para>
    /// See
    /// <a href="~/articles/mapping-overview.md">Mapping query results to objects</a> and
    /// <a href="~/articles/mapping-configuration.md">Configuring the mapping system</a>.
    /// </para>
    /// </remarks>
    /// <param name="record">The record to be mapped.</param>
    /// <param name="type">The type of object to be mapped.</param>
    /// <returns>The mapped object.</returns>
    public static object Map(IRecord record, Type type)
    {
        return ((IRecordObjectMapping)Instance).Map(record, type);
    }

    /// <summary>Maps a record to a new object of the same type as the provided blueprint object.</summary>
    /// <remarks>
    /// This overload exists to support anonymous types, whose names cannot be written as a generic type argument.
    /// Pass an instance of the anonymous type as <paramref name="blueprint"/> and the type is inferred
    /// automatically. The property values of the blueprint are ignored; only its type is used.
    /// <code language="csharp">
    /// var blueprint = new { name = default(string), age = default(int) };
    /// var result = RecordObjectMapping.MapFromBlueprint(record, blueprint);
    /// Console.WriteLine(result.name);
    /// </code>
    /// <para>
    /// See
    /// <a href="~/articles/mapping-overview.md">Mapping query results to objects</a> and
    /// <a href="~/articles/mapping-configuration.md">Configuring the mapping system</a>.
    /// </para>
    /// </remarks>
    /// <param name="record">The record to be mapped.</param>
    /// <param name="blueprint">
    /// An object whose runtime type determines the type of the object to be created. The existing
    /// property values of this object are discarded; only its type is used for mapping.
    /// </param>
    /// <typeparam name="T">The type of object that will be mapped. Inferred from <paramref name="blueprint"/>.</typeparam>
    /// <returns>A new mapped object of type <typeparamref name="T"/>.</returns>
    public static T MapFromBlueprint<T>(IRecord record, T blueprint)
    {
        return ((IRecordObjectMapping)Instance).MapFromBlueprint(record, blueprint);
    }
}
