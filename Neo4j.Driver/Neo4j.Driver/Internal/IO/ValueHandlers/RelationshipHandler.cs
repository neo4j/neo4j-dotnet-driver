﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.Internal.IO.ValueHandlers
{
    internal class RelationshipHandler : ReadOnlyStructHandler
    {
        public const byte Relationship = (byte)'R';
        public override IEnumerable<byte> ReadableStructs => new[] {Relationship};

        public override object Read(IPackStreamReader reader, byte signature, long size)
        {
            var urn = reader.ReadLong();
            var startUrn = reader.ReadLong();
            var endUrn = reader.ReadLong();
            var relType = reader.ReadString();
            var props = reader.ReadMap();

            return new Relationship(urn, startUrn, endUrn, relType, props);
        }
    }
}