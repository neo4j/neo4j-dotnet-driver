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

internal sealed class QueryApiRelationshipCodec : IQueryApiTypeCodec
{
    public bool CanRead(string typeName)
    {
        return typeName == "Relationship";
    }

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var value = element.GetProperty("_value");

        var elementId = value.GetProperty("_element_id").GetString()
            ?? throw new ProtocolException("Relationship element id was null.");

        var startNodeElementId = value.GetProperty("_start_node_element_id").GetString()
            ?? throw new ProtocolException("Relationship start node element id was null.");

        var endNodeElementId = value.GetProperty("_end_node_element_id").GetString()
            ?? throw new ProtocolException("Relationship end node element id was null.");

        var type = value.GetProperty("_type").GetString()
            ?? throw new ProtocolException("Relationship type was null.");

        var properties = new Dictionary<string, object>();
        foreach (var property in value.GetProperty("_properties").EnumerateObject())
        {
            properties[property.Name] = recurse.Decode(property.Value)!;
        }

        return new Relationship(0, elementId, 0, 0, startNodeElementId, endNodeElementId, type, properties);
    }

    public bool CanWrite(object? value)
    {
        return false;
    }

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        throw new NotSupportedException("Relationship values cannot be used as query parameters.");
    }
}
