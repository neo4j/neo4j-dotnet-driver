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
using System.Text.Json.Nodes;
using Neo4j.Driver.Internal.DependencyInjection;
using static Neo4j.Driver.Internal.QueryApi.QueryApiCodecHelper;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class QueryApiMapCodec : IQueryApiTypeCodec
{
    public bool CanRead(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        return typeName == "Map";
    }

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        ArgumentNullException.ThrowIfNull(recurse);
        var result = new Dictionary<string, object?>();
        foreach (var prop in element.GetProperty("_value").EnumerateObject())
            result[prop.Name] = recurse.Decode(prop.Value);
        return result;
    }

    public bool CanWrite(object? value) => value is IDictionary<string, object?>;

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        if (value is not IDictionary<string, object?> dict)
        {
            throw new ArgumentException("Value must be an IDictionary<string, object?>.", nameof(value));
        }

        ArgumentNullException.ThrowIfNull(recurse);

        var obj = new JsonObject();
        foreach (var (key, val) in dict)
        {
            obj[key] = recurse.Encode(val);
        }

        return CreateTypedEnvelope("Map", obj);
    }
}
