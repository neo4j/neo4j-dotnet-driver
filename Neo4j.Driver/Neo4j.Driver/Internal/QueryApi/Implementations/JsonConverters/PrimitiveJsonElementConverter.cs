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
using System.Text.Json;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Abstractions.JsonConverters;

namespace Neo4j.Driver.Internal.QueryApi.Implementations.JsonConverters;

[AutoRegister]
internal class PrimitiveJsonElementConverter : ITypedJsonElementConverter
{
    private static readonly HashSet<string> SupportedTypes =
        ["Null", "Boolean", "Integer", "Float", "String", "Base64", "Unsupported"];

    private readonly IBase64Decoder _decoder;

    public PrimitiveJsonElementConverter(IBase64Decoder decoder)
    {
        _decoder = decoder;
    }

    public bool CanConvert(string typeName)
    {
        return SupportedTypes.Contains(typeName);
    }

    public object? Convert(JsonElement element)
    {
        var typeName = element.GetProperty("$type").GetString()!;

        return typeName switch
        {
            "Null" => null,
            "Boolean" => element.GetProperty("_value").GetBoolean(),
            "Integer" => long.Parse(element.GetProperty("_value").GetString()!),
            "Float" => ParseFloat(element.GetProperty("_value").GetString()!),
            "String" => element.GetProperty("_value").GetString(),
            "Base64" => _decoder.Decode(element.GetProperty("_value").GetString()!),
            "Unsupported" => new UnsupportedType("Unsupported", 0, 0, element.GetProperty("_value").GetString()!),
            _ => throw new NotSupportedException($"Unsupported Neo4j type: {typeName}")
        };
    }

    private static double ParseFloat(string value) => value switch
    {
        "NaN" => double.NaN,
        "Infinity" => double.PositiveInfinity,
        "-Infinity" => double.NegativeInfinity,
        _ => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture)
    };
}
