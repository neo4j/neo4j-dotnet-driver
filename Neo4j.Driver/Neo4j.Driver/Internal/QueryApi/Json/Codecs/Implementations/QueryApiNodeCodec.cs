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
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.Internal.QueryApi;

internal sealed class QueryApiNodeCodec : IQueryApiTypeCodec
{
    public bool CanRead(string typeName)
    {
        return typeName == "Node";
    }

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var value = element.GetProperty("_value");

        var elementId = value.GetProperty("_element_id").GetString()
            ?? throw new ProtocolException("Node element id was null.");

        var labels = new List<string>();
        foreach (var label in value.GetProperty("_labels").EnumerateArray())
        {
            labels.Add(
                label.GetString() 
                    ?? throw new ProtocolException("Node label was null."));
        }

        var properties = new Dictionary<string, object>();
        foreach (var property in value.GetProperty("_properties").EnumerateObject())
        {
            properties[property.Name] = recurse.Decode(property.Value)!;
        }

        return new Node(0, elementId, labels, properties);
    }

    public bool CanWrite(object? value)
    {
        return false;
    }

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        throw new NotSupportedException("Node values cannot be used as query parameters.");
    }
}
