﻿// Copyright (c) "Neo4j"
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

using System.Collections.Generic;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.Internal.IO.ValueSerializers;

internal sealed class NodeSerializer : ReadOnlySerializer
{
    public const byte Node = (byte)'N';
    internal static readonly NodeSerializer Instance = new();
    public override byte[] ReadableStructs => new[] { Node };

    public override object Deserialize(PackStreamReader reader)
    {
        var urn = reader.ReadLong();

        var numLabels = (int)reader.ReadListHeader();
        var labels = new List<string>(numLabels);
        for (var i = 0; i < numLabels; i++)
        {
            labels.Add(reader.ReadString());
        }

        var numProps = (int)reader.ReadMapHeader();
        var props = new Dictionary<string, object>(numProps);
        for (var j = 0; j < numProps; j++)
        {
            var key = reader.ReadString();
            props.Add(key, reader.Read());
        }

        return new Node(urn, labels, props);
    }

    public override (object, int) DeserializeSpan(SpanPackStreamReader reader)
    {
        var urn = reader.ReadLong();

        var numLabels = reader.ReadListHeader();
        var labels = new List<string>(numLabels);
        for (var i = 0; i < numLabels; i++)
        {
            labels.Add(reader.ReadString());
        }

        var numProps = reader.ReadMapHeader();
        var props = new Dictionary<string, object>(numProps);
        for (var i = 0; i < numProps; i++)
        {
            var key = reader.ReadString();
            props.Add(key, reader.Read());
        }

        return (new Node(urn, labels, props), reader.Index);
    }
}
