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
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class JsonValueDecoder : IJsonValueDecoder
{
    private readonly IEnumerable<IQueryApiTypeCodec> _codecs;

    public JsonValueDecoder(IEnumerable<IQueryApiTypeCodec> codecs)
    {
        _codecs = codecs;
    }

    public object? Decode(JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Array => jsonElement.EnumerateArray().Select(Decode).ToList(),
            JsonValueKind.Object => ConvertObject(jsonElement),

            // the cast to object is necessary to force the return type of the ternary expression to be `object` -
            // otherwise, the compiler will use double, potentially losing precision for large integers
            JsonValueKind.Number => jsonElement.TryGetInt64(out var l) ? (object)l : jsonElement.GetDouble(),

            _ => throw new ArgumentOutOfRangeException(
                nameof(jsonElement),
                jsonElement.ValueKind,
                "Unexpected JSON value kind.")
        };
    }

    private object? ConvertObject(JsonElement jsonElement)
    {
        if (jsonElement.TryGetProperty("$type", out var typeElement))
        {
            var typeName = typeElement.GetString() ?? "unknown";

            foreach (var codec in _codecs)
            {
                if (codec.CanRead(typeName))
                    return codec.Read(jsonElement, this);
            }

            throw new ProtocolException($"Unsupported Neo4j type: {typeName}");
        }

        var dict = new Dictionary<string, object?>();
        foreach (var prop in jsonElement.EnumerateObject())
            dict[prop.Name] = Decode(prop.Value);

        return dict;
    }
}
