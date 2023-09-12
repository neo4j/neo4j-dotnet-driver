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

using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO.MessageSerializers;

internal sealed class RecordMessageSerializer : ReadOnlySerializer, IPackStreamMessageDeserializer
{
    internal static RecordMessageSerializer Instance = new();

    private static readonly byte[] StructTags = { MessageFormat.MsgRecord };
    public override byte[] ReadableStructs => StructTags;

    public override object Deserialize(PackStreamReader reader)
    {
        var fieldCount = (int)reader.ReadListHeader();
        var fields = new object[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            fields[i] = reader.Read();
        }

        return new RecordMessage(fields);
    }

    public IResponseMessage DeserializeMessage(BoltProtocolVersion formatVersion, SpanPackStreamReader packStreamReader)
    {
        var fieldCount = packStreamReader.ReadListHeader();
        var fields = new object[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            fields[i] = packStreamReader.Read();
        }
        return new RecordMessage(fields);
    }
}
