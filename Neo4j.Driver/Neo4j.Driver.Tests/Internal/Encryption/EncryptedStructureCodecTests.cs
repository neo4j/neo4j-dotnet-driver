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
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EncryptedStructureCodecTests
{
    private readonly Mock<IMessageFormatFactory> _messageFormatFactory = new();
    private readonly Mock<IPackStreamMemorySerializer> _packStreamMemorySerializer = new();
    private readonly MessageFormat _format = new MessageFormatFactory(TestDriverContext.MockContext)
        .CreateMessageFormat(BoltProtocolVersion.V6_0);

    private static EncryptedStructure Sample() => new(
        ProfileName: "Envelope",
        CipherOutput: new byte[] { 0xDE, 0xAD, 0xBE, 0xEF },
        TypeName: "Integer",
        TypeSerializationSchemeMajor: 6,
        TypeSerializationSchemeMinor: 0,
        Metadata: new Dictionary<string, object>
        {
            ["keyId"] = "key-1",
            ["iv"] = new byte[] { 1, 2, 3 }
        });

    private EncryptedStructureCodec CreateSubject()
    {
        _messageFormatFactory.Setup(f => f.CreateMessageFormat(It.IsAny<BoltProtocolVersion>())).Returns(_format);
        return new EncryptedStructureCodec(_messageFormatFactory.Object, _packStreamMemorySerializer.Object);
    }

    private void StubHelperRead(IPackStreamReader reader)
    {
        _packStreamMemorySerializer
            .Setup(h => h.Deserialize(_format, It.IsAny<byte[]>(), It.IsAny<Func<IPackStreamReader, EncryptedStructure>>()))
            .Returns((MessageFormat _, byte[] _, Func<IPackStreamReader, EncryptedStructure> read) => read(reader));
    }

    private void StubHelperReadString(IPackStreamReader reader)
    {
        _packStreamMemorySerializer
            .Setup(h => h.Deserialize(_format, It.IsAny<byte[]>(), It.IsAny<Func<IPackStreamReader, string>>()))
            .Returns((MessageFormat _, byte[] _, Func<IPackStreamReader, string> read) => read(reader));
    }

    [Fact]
    public void Encode_WritesTheStructureThroughTheHelperAndReturnsItsBytes()
    {
        var structure = Sample();
        var writer = new Mock<IPackStreamWriter>();
        var expectedBytes = new byte[] { 0xAA };

        _packStreamMemorySerializer
            .Setup(h => h.Serialize(_format, It.IsAny<Action<IPackStreamWriter>>()))
            .Returns((MessageFormat _, Action<IPackStreamWriter> write) =>
            {
                write(writer.Object);
                return expectedBytes;
            });

        var result = CreateSubject().Encode(structure);

        result.Should().BeSameAs(expectedBytes);
        writer.Verify(w => w.Write(structure.Metadata), Times.Once);
    }

    [Fact]
    public void Decode_WrongSignature_ThrowsProtocolException()
    {
        var reader = new Mock<IPackStreamReader>();
        reader.Setup(r => r.ReadStructSignature()).Returns((byte)0x99);
        StubHelperRead(reader.Object);

        var act = () => CreateSubject().Decode([]);

        act.Should().Throw<ProtocolException>();
    }

    [Fact]
    public void PeekProfileName_ReadsOnlyTheProfileNameField()
    {
        var reader = new Mock<IPackStreamReader>(MockBehavior.Strict);
        reader.Setup(r => r.ReadStructHeader()).Returns(1L);
        reader.Setup(r => r.ReadStructSignature()).Returns((byte)0x65);
        reader.Setup(r => r.ReadString()).Returns("Envelope");
        StubHelperReadString(reader.Object);

        var result = CreateSubject().PeekProfileName([]);

        result.Should().Be("Envelope");
    }

    [Fact]
    public void PeekProfileName_WrongSignature_ThrowsProtocolException()
    {
        var reader = new Mock<IPackStreamReader>();
        reader.Setup(r => r.ReadStructSignature()).Returns((byte)0x99);
        StubHelperReadString(reader.Object);

        var act = () => CreateSubject().PeekProfileName([]);

        act.Should().Throw<ProtocolException>();
    }
}
