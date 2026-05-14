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
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Abstractions.JsonConverters;

namespace Neo4j.Driver.Internal.QueryApi.Implementations.JsonConverters;

[AutoRegister]
internal class VectorJsonElementConverter : ITypedJsonElementConverter
{
    private readonly IJsonDeserializer _jsonDeserializer;

    public VectorJsonElementConverter(IJsonDeserializer jsonDeserializer)
    {
        _jsonDeserializer = jsonDeserializer;
    }

    public bool CanConvert(string typeName) => typeName == "Vector";

    public class JsonVector
    {
        public string? CoordinatesType { get; init; }
        
        public string[]? Coordinates { get; init; }
    }

    public object Convert(JsonElement element)
    {
        var vector = _jsonDeserializer.MapObject<JsonVector>(element)
            ?? throw new InvalidOperationException("Failed to deserialize vector.");

        var coords = vector.Coordinates!;

        Array values = vector.CoordinatesType switch
        {
            "FLOAT64" => coords.Select(double.Parse).ToArray(),
            "FLOAT32" => coords.Select(float.Parse).ToArray(),
            "INT64"   => coords.Select(long.Parse).ToArray(),
            "INT32"   => coords.Select(int.Parse).ToArray(),
            "INT16"   => coords.Select(short.Parse).ToArray(),
            "INT8"    => coords.Select(sbyte.Parse).ToArray(),
            _ => throw new NotSupportedException(
                $"Unsupported vector coordinatesType: '{vector.CoordinatesType}'.")
        };

        return Vector.CreateDynamic(values);
    }
}
