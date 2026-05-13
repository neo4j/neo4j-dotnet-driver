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
using Neo4j.Driver.Internal.QueryApi.Abstractions.JsonConverters;

namespace Neo4j.Driver.Internal.QueryApi.Implementations.JsonConverters;

[AutoRegister]
internal class JsonValueConverter : IJsonValueConverter
{
    private readonly IEnumerable<ITypedJsonElementConverter> _typedConverters;

    public JsonValueConverter(IEnumerable<ITypedJsonElementConverter> typedConverters)
    {
        _typedConverters = typedConverters;
    }

    public object? Convert(JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Number => jsonElement.TryGetInt64(out var l) ? l : jsonElement.GetDouble(),
            JsonValueKind.Array => jsonElement.EnumerateArray().Select(Convert).ToList(),
            JsonValueKind.Object => ConvertObject(jsonElement),
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

            foreach (var converter in _typedConverters)
            {
                if (converter.CanConvert(typeName))
                {
                    return converter.Convert(jsonElement);
                }
            }

            throw new NotSupportedException($"Unsupported Neo4j type: {typeName}");
        }

        var dict = new Dictionary<string, object?>();
        foreach (var prop in jsonElement.EnumerateObject())
        {
            dict[prop.Name] = Convert(prop.Value);
        }

        return dict;
    }
}
