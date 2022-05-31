// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.Internal.IO.ValueSerializers
{
    internal class ElementRelationshipSerializer : ReadOnlySerializer
    {
        public const byte Relationship = (byte)'R';
        public override IEnumerable<byte> ReadableStructs => new[] { Relationship };

        public override object Deserialize(IPackStreamReader reader, byte signature, long size)
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
    }
}