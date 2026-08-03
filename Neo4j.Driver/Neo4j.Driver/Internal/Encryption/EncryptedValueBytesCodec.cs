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

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class EncryptedValueBytesCodec : IEncryptedValueBytesCodec
{
    private const byte EncodingVersion = 0x01;

    private readonly IEncryptedStructureCodec _structureCodec;

    public EncryptedValueBytesCodec(IEncryptedStructureCodec structureCodec)
    {
        _structureCodec = structureCodec;
    }

    public byte[] Encode(EncryptedStructure structure)
    {
        var structureBytes = _structureCodec.Encode(structure);
        var result = new byte[structureBytes.Length + 1];
        result[0] = EncodingVersion;
        Array.Copy(structureBytes, 0, result, 1, structureBytes.Length);
        return result;
    }

    public EncryptedStructure Decode(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes[0] != EncodingVersion)
        {
            throw new ProtocolException(
                $"Expected Encrypted Value Encoding Version 0x{EncodingVersion:X2}, but got: " +
                (bytes.Length == 0 ? "an empty byte array" : $"0x{bytes[0]:X2}"));
        }

        return _structureCodec.Decode(bytes[1..]);
    }
}
