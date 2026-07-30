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

using Neo4j.Driver.TestKitBackend.Dispatch;
namespace Neo4j.Driver.TestKitBackend.Serialization;

internal class EnvelopeConverter : JsonConverter<IProtocolMessage>, IProtocolJsonConverter
{
    private readonly IMessageTypeMap _messageTypeMap;

    public EnvelopeConverter(IMessageTypeMap messageTypeMap)
    {
        _messageTypeMap = messageTypeMap;
    }

    public override IProtocolMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
        {
            throw new TestKitProtocolException("Message envelope is missing a string \"name\".");
        }

        var name = nameElement.GetString()!; // we know nameElement.ValueKind == JsonValueKind.String 
        var messageType = _messageTypeMap.GetTypeByName(name);

        var dataJson = root.TryGetProperty("data", out var dataElement)
            ? dataElement.GetRawText()
            : "{}";

        try
        {
            return (IProtocolMessage)JsonSerializer.Deserialize(dataJson, messageType, options)!;
        }
        catch (JsonException ex)
        {
            throw new TestKitProtocolException($"Failed to deserialize the data of message \"{name}\".", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, IProtocolMessage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.OutboundTypeName);
        writer.WritePropertyName("data");
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
        writer.WriteEndObject();
    }
}
