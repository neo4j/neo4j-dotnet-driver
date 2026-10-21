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

internal sealed class QueryApiPathCodec : IQueryApiTypeCodec
{
    public bool CanRead(string typeName)
    {
        return typeName == "Path";
    }

    public object? Read(JsonElement element, IJsonValueDecoder recurse)
    {
        var value = element.GetProperty("_value");
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ProtocolException("Path value was not an array.");
        }

        var elements = value.EnumerateArray();
        if (!elements.MoveNext())
        {
            throw new ProtocolException("Path value was empty; a path has at least one node.");
        }

        var nodes = new List<INode> { DecodeNode(elements.Current, recurse) };
        var relationships = new List<IRelationship>();
        var segments = new List<ISegment>();

        while (elements.MoveNext())
        {
            var relationship = DecodeRelationship(elements.Current, recurse);

            if (!elements.MoveNext())
            {
                throw new ProtocolException("Path ended on a relationship; expected a trailing node.");
            }

            var start = nodes[^1];
            var end = DecodeNode(elements.Current, recurse);

            relationships.Add(relationship);
            nodes.Add(end);
            segments.Add(new Segment(start, relationship, end));
        }

        return new Path(segments, nodes, relationships);
    }

    public bool CanWrite(object? value)
    {
        return false;
    }

    public JsonNode? Write(object? value, IJsonValueEncoder recurse)
    {
        throw new NotSupportedException("Path values cannot be used as query parameters.");
    }

    private static INode DecodeNode(JsonElement element, IJsonValueDecoder recurse)
    {
        return recurse.Decode(element) as INode
            ?? throw new ProtocolException("Expected a Node in the path sequence.");
    }

    private static IRelationship DecodeRelationship(JsonElement element, IJsonValueDecoder recurse)
    {
        return recurse.Decode(element) as IRelationship
            ?? throw new ProtocolException("Expected a Relationship in the path sequence.");
    }
}
