// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using System.Linq;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.Internal.IO.ValueSerializers;

internal sealed class PathSerializer : ReadOnlySerializer
{
    public const byte Path = (byte)'P';
    internal static readonly PathSerializer Instance = new();
    public override byte[] ReadableStructs => new[] { Path };

    public override object Deserialize(PackStreamReader reader)
    {
        // List of unique nodes
        var uniqNodes = new INode[(int)reader.ReadListHeader()];
        for (var i = 0; i < uniqNodes.Length; i++)
        {
            var node = reader.Read() as INode;

            Throw.ProtocolException.IfFalse(node != null, "receivedNode");

            uniqNodes[i] = node;
        }

        // List of unique relationships, without start/end information
        var uniqRels = new Relationship[(int)reader.ReadListHeader()];
        for (var i = 0; i < uniqRels.Length; i++)
        {
            var uniqRel = reader.Read() as Relationship;

            Throw.ProtocolException.IfFalse(uniqRel != null, "receivedUnboundRelationship");

            uniqRels[i] = uniqRel;
        }

        // Path sequence
        var length = (int)reader.ReadListHeader();

        // Knowing the sequence length, we can create the arrays that will represent the nodes, rels and segments in their "path order"
        var segments = new ISegment[length / 2];
        var nodes = new INode[segments.Length + 1];
        var rels = new IRelationship[segments.Length];

        var prevNode = uniqNodes[0];
        nodes[0] = prevNode;
        for (var i = 0; i < segments.Length; i++)
        {
            var relIdx = (int)reader.ReadLong();
            var nextNode =
                uniqNodes[(int)reader.ReadLong()]; // Start node is always 0, and isn't encoded in the sequence

            // Negative rel index means this rel was traversed "inversed" from its direction
            Relationship rel;
            if (relIdx < 0)
            {
                rel = uniqRels[-relIdx - 1]; // -1 because rel idx are 1-indexed
                rel.SetStartAndEnd(nextNode, prevNode);
            }
            else
            {
                rel = uniqRels[relIdx - 1];
                rel.SetStartAndEnd(prevNode, nextNode);
            }

            nodes[i + 1] = nextNode;
            rels[i] = rel;
            segments[i] = new Segment(prevNode, rel, nextNode);
            prevNode = nextNode;
        }

        return new Path(segments.ToList(), nodes.ToList(), rels.ToList());
    }
    
    public override (object, int) DeserializeSpan(SpanPackStreamReader reader)
    {
        // List of unique nodes
        var uniqNodes = new Node[reader.ReadListHeader()];
        for (var i = 0; i < uniqNodes.Length; i++)
        {
            if (reader.Read() is not Node node)
            {
                throw new ProtocolException("Expecting receivedNode to be true, however the value is false");
            }
            uniqNodes[i] = node;
        }

        // List of unique relationships, without start/end information
        var uniqRels = new Relationship[reader.ReadListHeader()];
        for (var i = 0; i < uniqRels.Length; i++)
        {
            if (reader.Read() is not Relationship uniqRel)
            {
                throw new ProtocolException("Expecting receivedUnboundRelationship to be true, however the value is false");
            }
            uniqRels[i] = uniqRel;
        }

        // Path sequence
        var length = reader.ReadListHeader();

        // Knowing the sequence length, we can create the arrays that will represent the nodes, rels and segments in their "path order"
        var segments = new ISegment[length / 2];
        var nodes = new INode[segments.Length + 1];
        var rels = new IRelationship[segments.Length];

        var prevNode = uniqNodes[0];
        nodes[0] = prevNode;
        for (var i = 0; i < segments.Length; i++)
        {
            var relIdx = (int)reader.ReadLong();
            var nextNode =
                uniqNodes[(int)reader.ReadLong()]; // Start node is always 0, and isn't encoded in the sequence

            // Negative rel index means this rel was traversed "inversed" from its direction
            Relationship rel;
            if (relIdx < 0)
            {
                rel = uniqRels[-relIdx - 1]; // -1 because rel idx are 1-indexed
                rel.SetStartAndEnd(nextNode, prevNode);
            }
            else
            {
                rel = uniqRels[relIdx - 1];
                rel.SetStartAndEnd(prevNode, nextNode);
            }

            nodes[i + 1] = nextNode;
            rels[i] = rel;
            segments[i] = new Segment(prevNode, rel, nextNode);
            prevNode = nextNode;
        }

        return (new Path(segments.ToList(), nodes.ToList(), rels.ToList()), reader.Index);
    }
}
