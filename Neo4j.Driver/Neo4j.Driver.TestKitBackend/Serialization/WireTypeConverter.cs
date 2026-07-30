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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo4j.Driver.TestKitBackend.Serialization;

// Statically-known wire values are declared as IWireType<T> and arrive wrapped in their own
// {"name","data"} envelope, e.g. AuthorizationToken inside NewDriverRequest. The converter
// claims the interface, never T itself, so binding data as T with the same options cannot
// re-enter this converter — enveloped values nest to any depth with no options cloning.
internal class WireTypeConverterFactory : JsonConverterFactory, IProtocolJsonConverter
{
    private readonly IWireTypeNameProvider _wireTypeNameProvider;

    public WireTypeConverterFactory(IWireTypeNameProvider wireTypeNameProvider)
    {
        _wireTypeNameProvider = wireTypeNameProvider;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsInterface &&
            typeToConvert.IsGenericType &&
            typeToConvert.GetGenericTypeDefinition() == typeof(IWireType<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var concreteType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(WireTypeConverter<>).MakeGenericType(concreteType),
            _wireTypeNameProvider.GetInboundTypeName(concreteType))!;
    }
}

internal class WireTypeConverter<T> : JsonConverter<IWireType<T>> where T : IWireType<T>
{
    private readonly string _expectedName;

    public WireTypeConverter(string expectedName)
    {
        _expectedName = expectedName;
    }

    public override IWireType<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
        {
            throw new TestKitProtocolException("Wire type envelope is missing a string \"name\".");
        }

        var name = nameElement.GetString()!; // we know nameElement.ValueKind == JsonValueKind.String
        if (name != _expectedName)
        {
            throw new TestKitProtocolException($"Expected wire type \"{_expectedName}\", got \"{name}\".");
        }

        var dataJson = root.TryGetProperty("data", out var dataElement)
            ? dataElement.GetRawText()
            : "{}";

        try
        {
            return JsonSerializer.Deserialize<T>(dataJson, options)!;
        }
        catch (JsonException ex)
        {
            throw new TestKitProtocolException($"Failed to deserialize the data of wire type \"{name}\".", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, IWireType<T> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
