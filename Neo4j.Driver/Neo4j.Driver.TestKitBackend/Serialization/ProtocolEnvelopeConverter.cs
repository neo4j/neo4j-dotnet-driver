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

internal class ProtocolEnvelopeConverter<T> : JsonConverter<T>
{
    private readonly string _expectedName;

    public ProtocolEnvelopeConverter(string expectedName)
    {
        _expectedName = expectedName;
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
        {
            throw new TestKitProtocolException("Protocol envelope is missing a string \"name\".");
        }

        var name = nameElement.GetString()!;
        if (name != _expectedName)
        {
            throw new TestKitProtocolException($"Expected protocol envelope \"{_expectedName}\", got \"{name}\".");
        }

        var dataJson = root.TryGetProperty("data", out var dataElement)
            ? dataElement.GetRawText()
            : "{}";

        try
        {
            return JsonSerializer.Deserialize<T>(dataJson, options);
        }
        catch (TestKitProtocolException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TestKitProtocolException($"Failed to deserialize the data of protocol envelope \"{name}\".", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", _expectedName);
        writer.WritePropertyName("data");
        JsonSerializer.Serialize(writer, value, options);
        writer.WriteEndObject();
    }
}
