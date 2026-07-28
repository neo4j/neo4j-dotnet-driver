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

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo4j.Driver.TestKitBackend.Protocol;

// IWireType payloads that are not themselves IProtocolMessage (those are claimed by
// EnvelopeConverter) still arrive nested as their own {"name","data"} envelope, e.g.
// AuthorizationToken inside NewDriverRequest. A factory closes PayloadEnvelopeConverter<T>
// per payload type.
internal class PayloadEnvelopeConverterFactory : JsonConverterFactory, IProtocolJsonConverter
{
    private readonly IWireTypeNameProvider _wireTypeNameProvider;
    private readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> _innerOptionsCache = new();

    public PayloadEnvelopeConverterFactory(IWireTypeNameProvider wireTypeNameProvider)
    {
        _wireTypeNameProvider = wireTypeNameProvider;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IWireType).IsAssignableFrom(typeToConvert) &&
            !typeof(IProtocolMessage).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(
            typeof(PayloadEnvelopeConverter<>).MakeGenericType(typeToConvert),
            this,
            _wireTypeNameProvider.GetInboundTypeName(typeToConvert))!;
    }

    // Deserializing "data" with the same options this factory came from would match this
    // factory again for the same concrete type, recursing straight back into the envelope
    // check instead of reading the payload's own fields. One clone with this factory removed
    // is enough for a single level of nesting; real re-entry (options ping-pong) can wait until
    // a payload needs to nest arbitrarily deep (Cypher values, M6-7).
    public JsonSerializerOptions InnerOptions(JsonSerializerOptions options)
    {
        return _innerOptionsCache.GetValue(options, BuildInnerOptions);
    }

    private JsonSerializerOptions BuildInnerOptions(JsonSerializerOptions options)
    {
        var inner = new JsonSerializerOptions(options);
        foreach (var converter in inner.Converters.Where(c => ReferenceEquals(c, this)).ToList())
        {
            inner.Converters.Remove(converter);
        }

        return inner;
    }
}

internal class PayloadEnvelopeConverter<T> : JsonConverter<T> where T : IWireType
{
    private readonly PayloadEnvelopeConverterFactory _factory;
    private readonly string _expectedName;

    public PayloadEnvelopeConverter(PayloadEnvelopeConverterFactory factory, string expectedName)
    {
        _factory = factory;
        _expectedName = expectedName;
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
        {
            throw new TestKitProtocolException("Payload envelope is missing a string \"name\".");
        }

        var name = nameElement.GetString()!; // we know nameElement.ValueKind == JsonValueKind.String
        if (name != _expectedName)
        {
            throw new TestKitProtocolException($"Expected payload \"{_expectedName}\", got \"{name}\".");
        }

        var dataJson = root.TryGetProperty("data", out var dataElement)
            ? dataElement.GetRawText()
            : "{}";

        try
        {
            return JsonSerializer.Deserialize<T>(dataJson, _factory.InnerOptions(options))!;
        }
        catch (JsonException ex)
        {
            throw new TestKitProtocolException($"Failed to deserialize the data of payload \"{name}\".", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
