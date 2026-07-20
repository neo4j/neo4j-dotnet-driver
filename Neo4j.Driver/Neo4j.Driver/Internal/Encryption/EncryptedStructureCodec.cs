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
    // always serialize at the latest supported version (per ADR 037); UUID is excluded from
    // the types we encode even though it's within this dialect, until UUID support is confirmed
    private static readonly BoltProtocolVersion StructureVersion = BoltProtocolVersion.V6_1;
    private const byte EncryptedSignature = 0x65;
    private const int FieldCount = 7;
    private const int StructureFormatVersion = 1;

    private readonly IPackStreamSerializationHelper _packStreamHelper;
    private readonly MessageFormat _format;

    public EncryptedStructureCodec(
        IMessageFormatFactory messageFormatFactory,
        IPackStreamSerializationHelper packStreamHelper)
    {
        _packStreamHelper = packStreamHelper;
        _format = messageFormatFactory.CreateMessageFormat(StructureVersion);
    }

    public byte[] Encode(EncryptedStructure structure)
    {
        return _packStreamHelper.Write(
            _format,
            writer =>
            {
                writer.WriteStructHeader(FieldCount, EncryptedSignature);
                writer.Write(StructureFormatVersion);
                writer.Write(structure.ProfileName);
                writer.Write(structure.CipherOutput);
                writer.Write(structure.TypeName);
                writer.Write(structure.TypeProtocolMajor);
                writer.Write(structure.TypeProtocolMinor);
                writer.Write(structure.Metadata);
            });
    }

    public EncryptedStructure Decode(byte[] bytes)
    {
        return _packStreamHelper.Read(_format, bytes, ReadStructure);
    }

    private static EncryptedStructure ReadStructure(IPackStreamReader reader)
    {
        reader.ReadStructHeader();
        var signature = reader.ReadStructSignature();
        if (signature != EncryptedSignature)
        {
            throw new ProtocolException(
                $"Expected an Encrypted structure (0x{EncryptedSignature:X2}), but got: 0x{signature:X2}");
        }

        var version = reader.ReadInteger();
        if (version != StructureFormatVersion)
        {
            throw new ProtocolException(
                $"Unsupported Encrypted structure version {version}; expected {StructureFormatVersion}.");
        }

        return new EncryptedStructure(
            ProfileName: reader.ReadString(),
            CipherOutput: reader.ReadBytes(),
            TypeName: reader.ReadString(),
            TypeProtocolMajor: reader.ReadInteger(),
            TypeProtocolMinor: reader.ReadInteger(),
            Metadata: reader.ReadMap());
    }
}
