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
using System.Text.Json.Nodes;
using Neo4j.Driver.Internal.DependencyInjection;
using static Neo4j.Driver.Internal.QueryApi.QueryApiCodecHelper;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal sealed class QueryApiVectorCodec : IQueryApiTypeCodec
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly VectorType[] VectorTypes =
    [
        new VectorType<sbyte>("INT8", null),
        new VectorType<short>("INT16", null),
        new VectorType<int>("INT32", null),
        new VectorType<long>("INT64", null),
        new VectorType<float>("FLOAT32", "G9"),
        new VectorType<double>("FLOAT64", "G17")
    ];

    private static readonly Dictionary<Type, VectorType> ByElementType =
        VectorTypes.ToDictionary(v => v.ElementType);

    private static readonly Dictionary<string, VectorType> ByWireName =
        VectorTypes.ToDictionary(v => v.WireName);

    public QueryApiMediaVersion RequiredVersion => QueryApiMediaVersion.V1_1;

    public bool CanRead(string typeName)
    {
        return typeName == "Vector";
    }

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var vector = element.GetProperty("_value").Deserialize<JsonVector>(CamelCaseOptions) ??
            throw new ProtocolException("Vector element could not be deserialized.");

        var coords = vector.Coordinates ?? throw new ProtocolException("Vector coordinates are missing.");

        if (vector.CoordinatesType is null || !ByWireName.TryGetValue(vector.CoordinatesType, out var type))
        {
            throw new ProtocolException($"Unsupported vector coordinatesType: '{vector.CoordinatesType}'.");
        }

        return type.Build(coords);
    }

    public bool CanWrite(object? value)
    {
        return value is IVector;
    }

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        var vector = (IVector)value!;
        if (!ByElementType.TryGetValue(vector.ElementType, out var type))
        {
            throw new ProtocolException($"Unsupported vector element type: '{vector.ElementType}'.");
        }

        var coordinates = new JsonArray();
        foreach (var element in vector.UntypedValues)
        {
            coordinates.Add(JsonValue.Create(type.Format(element)));
        }

        var body = new JsonObject
        {
            ["coordinatesType"] = type.WireName,
            ["coordinates"] = coordinates
        };

        return CreateTypedEnvelope("Vector", body);
    }

    private class JsonVector
    {
        public string? CoordinatesType { get; init; }

        public string[]? Coordinates { get; init; }
    }
}
