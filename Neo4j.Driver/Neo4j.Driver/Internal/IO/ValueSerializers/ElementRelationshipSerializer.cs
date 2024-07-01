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

using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.Internal.IO.ValueSerializers;

internal sealed class ElementRelationshipSerializer : ReadOnlySerializer
{
    public const byte Relationship = (byte)'R';
    internal static readonly ElementRelationshipSerializer Instance = new();
    public override byte[] ReadableStructs => new[] { Relationship };

    public override object Deserialize(PackStreamReader reader)
    {
        var relId = reader.ReadLong();
        var relStartId = reader.ReadLong();
        var relEndId = reader.ReadLong();

        var relType = reader.ReadString();
        var props = reader.ReadMap();

        var urn = reader.ReadString();
        var startUrn = reader.ReadString();
        var endUrn = reader.ReadString();

        return new Relationship(relId, urn, relStartId, relEndId, startUrn, endUrn, relType, props);
    }

    public override (object, int) DeserializeSpan(SpanPackStreamReader reader)
    {
        var relId = reader.ReadLong();
        var relStartId = reader.ReadLong();
        var relEndId = reader.ReadLong();

        var relType = reader.ReadString();
        var props = reader.ReadMap();

        var urn = reader.ReadString();
        var startUrn = reader.ReadString();
        var endUrn = reader.ReadString();

        return (new Relationship(relId, urn, relStartId, relEndId, startUrn, endUrn, relType, props), reader.Index);
    }
}
