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

// Shared envelope read/write for every open union (messages, cypher values, ...): the concrete
// type isn't known at the declaration site, only from the envelope's "name" at parse time.
// Subclasses stay concrete (not this class generic) so DI keeps a distinct resolver per union.
internal abstract class WireTypeUnionConverter<TUnion> : JsonConverter<TUnion>, IProtocolJsonConverter
    where TUnion : IWireType
{
    private readonly IWireTypeResolver _resolver;

    protected WireTypeUnionConverter(IWireTypeResolver resolver)
    {
        _resolver = resolver;
    }

    public override TUnion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
        {
            throw new TestKitProtocolException("Wire type envelope is missing a string \"name\".");
        }

        var name = nameElement.GetString()!; // we know nameElement.ValueKind == JsonValueKind.String
        var concreteType = _resolver.GetTypeByName(name);

        var dataJson = root.TryGetProperty("data", out var dataElement)
            ? dataElement.GetRawText()
            : "{}";

        try
        {
            return (TUnion)JsonSerializer.Deserialize(dataJson, concreteType, options)!;
        }
        catch (JsonException ex)
        {
            throw new TestKitProtocolException($"Failed to deserialize the data of wire type \"{name}\".", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, TUnion value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.OutboundTypeName);
        writer.WritePropertyName("data");
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
        writer.WriteEndObject();
    }
}
