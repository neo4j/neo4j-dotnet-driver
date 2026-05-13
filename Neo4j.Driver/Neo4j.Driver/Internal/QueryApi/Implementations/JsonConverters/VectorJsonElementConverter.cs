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
using System.Linq;
using System.Text.Json;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions.JsonConverters;

namespace Neo4j.Driver.Internal.QueryApi.Implementations.JsonConverters;

[AutoRegister]
internal class VectorJsonElementConverter : ITypedJsonElementConverter
{
    public bool CanConvert(string typeName) => typeName == "Vector";

    public object Convert(JsonElement element)
    {
        var value = element.GetProperty("_value");
        var coordinatesType = value.GetProperty("coordinatesType").GetString()
            ?? throw new InvalidOperationException("Vector '_value.coordinatesType' is null.");

        var coords = value.GetProperty("coordinates")
            .EnumerateArray()
            .Select(e => e.GetString()!);

        Array values = coordinatesType switch
        {
            "FLOAT64" => coords.Select(double.Parse).ToArray(),
            "FLOAT32" => coords.Select(float.Parse).ToArray(),
            "INT64"   => coords.Select(long.Parse).ToArray(),
            "INT32"   => coords.Select(int.Parse).ToArray(),
            "INT16"   => coords.Select(short.Parse).ToArray(),
            "INT8"    => coords.Select(sbyte.Parse).ToArray(),
            _ => throw new NotSupportedException(
                $"Unsupported vector coordinatesType: '{coordinatesType}'.")
        };

        return Vector.CreateDynamic(values);
    }
}
