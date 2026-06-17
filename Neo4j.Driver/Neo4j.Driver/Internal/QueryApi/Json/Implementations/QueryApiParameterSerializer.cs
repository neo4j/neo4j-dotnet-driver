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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal sealed class QueryApiParameterSerializer : JsonConverter<object?>, IJsonSerializer
{
    private readonly JsonSerializerOptions _options;
    private readonly IEnumerable<IQueryApiTypeCodec> _codecs;

    public QueryApiParameterSerializer(IEnumerable<IQueryApiTypeCodec> codecs)
    {
        _codecs = codecs;
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { this }
        };
    }

    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _options);
    }

    public override bool HandleNull => true;

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("QueryApiParameterSerializer is write-only.");

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        var cdc = _codecs.FirstOrDefault(c => c.CanWrite(value)) ?? 
            throw new NotSupportedException(
                $"Query parameter type '{value?.GetType().Name}' is not supported by the HTTP Query API.");

        cdc.Write(writer, value, options);
    }
}
