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

namespace Neo4j.Driver.Tests.BenchkitBackend.InfrastructureExtensions;

/// <summary>
/// The purpose of this class is to ensure that when the destination type is "object" that we don't deserialize a
/// primitive into a JsonElement of some kind, which causes issues when trying to write the value to a PackStream,
/// but instead deserialize it into a primitive type.
/// </summary>
internal class ObjectToPrimitiveConverter : JsonConverter<object>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(object);
    }

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
            JsonTokenType.String => reader.GetString()!,
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        object objectToWrite,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
    }
}
