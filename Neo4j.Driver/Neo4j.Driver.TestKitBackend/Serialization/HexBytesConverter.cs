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
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Serialization;

internal class HexBytesConverter : JsonConverter<HexBytes>, IProtocolJsonConverter
{
    public override HexBytes Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        if (string.IsNullOrEmpty(hex))
        {
            return new HexBytes([]);
        }

        return new HexBytes(Convert.FromHexString(hex.Replace(" ", "")));
    }

    public override void Write(Utf8JsonWriter writer, HexBytes value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Convert.ToHexStringLower(value.Value));
    }
}
