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

using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.IO.MessageSerializers;

internal sealed class SuccessMessageSerializer : ReadOnlySerializer, IPackStreamMessageDeserializer
{
    internal static SuccessMessageSerializer Instance = new();

    private static readonly byte[] StructTags = { MessageFormat.MsgSuccess };
    public override byte[] ReadableStructs => StructTags;

    public override object Deserialize(PackStreamReader reader)
    {
        var map = reader.ReadMap();
        return new SuccessMessage(map);
    }

    public IResponseMessage DeserializeMessage(BoltProtocolVersion formatVersion, SpanPackStreamReader packStreamReader)
    {
        var map = packStreamReader.ReadMap();
        return new SuccessMessage(map);
    }
}
