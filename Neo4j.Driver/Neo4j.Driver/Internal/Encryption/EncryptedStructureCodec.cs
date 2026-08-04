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

using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class EncryptedStructureCodec : IEncryptedStructureCodec
{
    private static readonly BoltProtocolVersion StructureVersion = BoltProtocolVersion.V6_1;
    private const byte EncryptedSignature = 0x65;
    private const int FieldCount = 6;

    private readonly IPackStreamMemorySerializer _packStreamMemorySerializer;
    private readonly MessageFormat _format;

    public EncryptedStructureCodec(
        IMessageFormatFactory messageFormatFactory,
        IPackStreamMemorySerializer packStreamMemorySerializer)
    {
        _packStreamMemorySerializer = packStreamMemorySerializer;
        _format = messageFormatFactory.CreateMessageFormat(StructureVersion);
    }

    public byte[] Encode(EncryptedStructure structure)
    {
        return _packStreamMemorySerializer.Serialize(
            _format,
            writer =>
            {
                writer.WriteStructHeader(FieldCount, EncryptedSignature);
                writer.Write(structure.ProfileName);
                writer.Write(structure.CipherOutput);
                writer.Write(structure.TypeName);
                writer.Write(structure.TypeSerializationSchemeMajor);
                writer.Write(structure.TypeSerializationSchemeMinor);
                writer.Write(structure.Metadata);
            });
    }

    public EncryptedStructure Decode(byte[] bytes)
    {
        return _packStreamMemorySerializer.Deserialize(_format, bytes, ReadStructure);
    }

    public string PeekProfileName(byte[] bytes)
    {
        return _packStreamMemorySerializer.Deserialize(_format, bytes, ReadProfileName);
    }

    private static EncryptedStructure ReadStructure(IPackStreamReader reader)
    {
        ReadAndValidateSignature(reader);

        return new EncryptedStructure(
            ProfileName: reader.ReadString(),
            CipherOutput: reader.ReadBytes(),
            TypeName: reader.ReadString(),
            TypeSerializationSchemeMajor: reader.ReadInteger(),
            TypeSerializationSchemeMinor: reader.ReadInteger(),
            Metadata: reader.ReadMap());
    }

    private static string ReadProfileName(IPackStreamReader reader)
    {
        ReadAndValidateSignature(reader);
        return reader.ReadString();
    }

    private static void ReadAndValidateSignature(IPackStreamReader reader)
    {
        reader.ReadStructHeader();
        var signature = reader.ReadStructSignature();
        if (signature != EncryptedSignature)
        {
            throw new ProtocolException(
                $"Expected an Encrypted structure (0x{EncryptedSignature:X2}), but got: 0x{signature:X2}");
        }
    }
}
