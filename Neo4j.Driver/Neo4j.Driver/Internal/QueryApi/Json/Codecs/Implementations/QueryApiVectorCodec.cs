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
using System.Text.Json.Nodes;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal sealed class QueryApiVectorCodec : IQueryApiTypeCodec
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public class JsonVector
    {
        public string? CoordinatesType { get; init; }

        public string[]? Coordinates { get; init; }
    }

    public bool CanRead(string typeName) => typeName == "Vector";

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var vector = element.Deserialize<JsonVector>(CamelCaseOptions)
            ?? throw new ProtocolException("Vector element could not be deserialized.");

        var coords = vector.Coordinates
            ?? throw new ProtocolException("Vector coordinates are missing.");

        Array values = vector.CoordinatesType switch
        {
            "FLOAT64" => coords.Select(double.Parse).ToArray(),
            "FLOAT32" => coords.Select(float.Parse).ToArray(),
            "INT64" => coords.Select(long.Parse).ToArray(),
            "INT32" => coords.Select(int.Parse).ToArray(),
            "INT16" => coords.Select(short.Parse).ToArray(),
            "INT8" => coords.Select(sbyte.Parse).ToArray(),
            _ => throw new ProtocolException(
                $"Unsupported vector coordinatesType: '{vector.CoordinatesType}'.")
        };

        return Vector.CreateDynamic(values);
    }

    public bool CanWrite(object? value) => false;

    public JsonNode? Write(object? value, IJsonValueEncoder recurse) =>
        throw new NotImplementedException("Vector parameters are not yet implemented as query parameters.");
}
