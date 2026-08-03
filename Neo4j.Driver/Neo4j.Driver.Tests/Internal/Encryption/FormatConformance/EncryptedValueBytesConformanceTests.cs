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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Internal.IO;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption.FormatConformance;

public class EncryptedValueBytesConformanceTests
{
    private readonly EncryptedValueBytesCodec _subject = new(
        new EncryptedStructureCodec(
            new MessageFormatFactory(TestDriverContext.MockContext),
            new PackStreamSerializationHelper(new PackStreamReaderWriterFactory())));

    // Reuses EncryptedStructureConformanceTests' known-answer structure/bytes for the inner
    // Encrypted structure (already hand-verified there) - this test's only concern is the outer
    // Encrypted Value Bytes envelope, i.e. the single prepended encoding version byte.
    private static EncryptedStructure KnownAnswerStructure() => new(
        ProfileName: "env",
        CipherOutput: [0xFF],
        TypeName: "Int",
        TypeSerializationSchemeMajor: 6,
        TypeSerializationSchemeMinor: 0,
        Metadata: new Dictionary<string, object>());

    private static readonly byte[] KnownAnswerBytes =
    [
        0x01, // Encrypted Value Encoding Version
        0xB6, 0x65, // struct header: TinyStruct[6], Encrypted signature
        0x83, 0x65, 0x6E, 0x76, // profileName = "env" (TinyString[3])
        0xCC, 0x01, 0xFF, // cipherOutput = [0xFF] (Bytes8[1])
        0x83, 0x49, 0x6E, 0x74, // typeName = "Int" (TinyString[3])
        0x06, // typeSerializationSchemeMajor = 6
        0x00, // typeSerializationSchemeMinor = 0
        0xA0 // metadata = {} (TinyMap[0])
    ];

    [Fact]
    public void Encode_ProducesTheExactKnownAnswerByteSequence()
    {
        var bytes = _subject.Encode(KnownAnswerStructure());

        bytes.Should().Equal(KnownAnswerBytes);
    }

    [Fact]
    public void Decode_ParsesTheExactKnownAnswerByteSequence()
    {
        var result = _subject.Decode(KnownAnswerBytes);

        result.Should()
            .BeEquivalentTo(KnownAnswerStructure(), opt => opt.ComparingByMembers<EncryptedStructure>());
    }
}
