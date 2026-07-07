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

using System.IO;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.Encryption;

internal sealed class PlaintextSerializer : IPlaintextSerializer, IPlaintextDeserializer
{
    // The plaintext scheme is pinned to a fixed PackStream/Bolt version so encrypted values do not
    // change encoding with the negotiated connection version. 6.1 (UUID) is deliberately excluded
    // until UUID support is settled.
    private static readonly BoltProtocolVersion PlaintextVersion = BoltProtocolVersion.V6_0;

    private readonly MessageFormat _format;

    public PlaintextSerializer(DriverContext context)
    {
        _format = new MessageFormat(PlaintextVersion, context);
    }

    public byte[] Serialize(object value)
    {
        using var stream = new MemoryStream();
        new PackStreamWriter(_format, stream).Write(value);
        return stream.ToArray();
    }

    public object Deserialize(byte[] plaintext)
    {
        return new PackStreamReader(_format, new MemoryStream(plaintext), new ByteBuffers()).Read();
    }
}
