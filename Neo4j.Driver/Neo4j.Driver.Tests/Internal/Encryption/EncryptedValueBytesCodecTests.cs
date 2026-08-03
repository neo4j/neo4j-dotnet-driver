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

using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Internal.Protocol;
using Xunit;
using static Neo4j.Driver.Tests.Internal.Encryption.EncryptionTestHelpers;

namespace Neo4j.Driver.Tests.Internal.Encryption;

// EncryptedValueBytesCodec's own responsibility is the outer Encrypted Value Bytes envelope (the
// 1-byte encoding version prefix) - the inner Encrypted structure's own byte layout is
// EncryptedStructureCodec's concern (locked down in EncryptedStructureConformanceTests). This
// codec's own outer-envelope layout is locked down for real in
// Encryption.FormatConformance.EncryptedValueBytesConformanceTests.
public class EncryptedValueBytesCodecTests
{
    private readonly Mock<IEncryptedStructureCodec> _structureCodec = new();

    private static EncryptedStructure Sample() => new(
        ProfileName: "Envelope",
        CipherOutput: [0xDE, 0xAD],
        TypeName: "Integer",
        TypeSerializationSchemeMajor: 1,
        TypeSerializationSchemeMinor: 0,
        Metadata: new Dictionary<string, object>());

    private EncryptedValueBytesCodec CreateSubject() => new(_structureCodec.Object);

    [Fact]
    public void Encode_PrependsTheEncodingVersionByteToTheStructureCodecsBytes()
    {
        var structure = Sample();
        _structureCodec.Setup(c => c.Encode(structure)).Returns(new byte[] { 0xAA, 0xBB });

        var result = CreateSubject().Encode(structure);

        result.Should().Equal(0x01, 0xAA, 0xBB);
    }

    [Fact]
    public void Decode_StripsTheEncodingVersionByteAndDelegatesToTheStructureCodec()
    {
        var structure = Sample();
        _structureCodec.Setup(c => c.Decode(Matches(new byte[] { 0xAA, 0xBB }))).Returns(structure);

        var result = CreateSubject().Decode([0x01, 0xAA, 0xBB]);

        result.Should().BeSameAs(structure);
    }

    [Fact]
    public void Decode_WrongEncodingVersion_ThrowsProtocolException()
    {
        var act = () => CreateSubject().Decode([0x02, 0xAA, 0xBB]);

        act.Should().Throw<ProtocolException>();
    }

    [Fact]
    public void Decode_EmptyBytes_ThrowsProtocolException()
    {
        var act = () => CreateSubject().Decode([]);

        act.Should().Throw<ProtocolException>();
    }
}
