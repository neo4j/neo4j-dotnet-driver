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

// Without this, System.Text.Json boxes untyped values (e.g. Dictionary<string, object> values)
// as JsonElement instead of native CLR scalars.
internal class ObjectJsonConverter : JsonConverter<object?>, IProtocolJsonConverter
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number when reader.TryGetInt64(out var intVal) => intVal,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.StartObject => ReadObject(ref reader, options),
            JsonTokenType.StartArray => ReadArray(ref reader, options),
            _ => throw new NotSupportedException($"Unsupported JSON token type '{reader.TokenType}'.")
        };
    }

    private Dictionary<string, object?> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var map = new Dictionary<string, object?>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var key = reader.GetString()!;
            reader.Read();
            map[key] = Read(ref reader, typeof(object), options);
        }

        return map;
    }

    private List<object?> ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var list = new List<object?>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(Read(ref reader, typeof(object), options));
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;

            case string s:
                writer.WriteStringValue(s);
                break;

            case bool b:
                writer.WriteBooleanValue(b);
                break;

            case long l:
                writer.WriteNumberValue(l);
                break;

            case int i:
                writer.WriteNumberValue(i);
                break;

            case double d:
                writer.WriteNumberValue(d);
                break;

            case IDictionary<string, object> map:
                writer.WriteStartObject();
                foreach (var (key, mapValue) in map)
                {
                    writer.WritePropertyName(key);
                    Write(writer, mapValue, options);
                }

                writer.WriteEndObject();
                break;

            case IEnumerable<object> list:
                writer.WriteStartArray();
                foreach (var item in list)
                {
                    Write(writer, item, options);
                }

                writer.WriteEndArray();
                break;

            default:
                throw new NotSupportedException($"Cannot write untyped value of type '{value.GetType().Name}'.");
        }
    }
}
