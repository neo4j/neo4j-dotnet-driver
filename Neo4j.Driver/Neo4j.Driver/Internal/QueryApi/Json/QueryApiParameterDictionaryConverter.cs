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

#nullable enable

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal sealed class QueryApiParameterDictionaryConverter : 
    JsonConverter<QueryApiParameterDictionary>,
    IQueryApiJsonConverter
{
    private readonly IJsonValueEncoder _encoder;

    public QueryApiParameterDictionaryConverter(
        IJsonValueEncoder encoder)
    {
        _encoder = encoder;
    }

    public override QueryApiParameterDictionary Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException("Parameter dictionaries are write-only.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        QueryApiParameterDictionary value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (key, paramValue) in value)
        {
            writer.WritePropertyName(key);
            var node = _encoder.Encode(paramValue);
            if (node is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                node.WriteTo(writer, options);
            }
        }

        writer.WriteEndObject();
    }
}
