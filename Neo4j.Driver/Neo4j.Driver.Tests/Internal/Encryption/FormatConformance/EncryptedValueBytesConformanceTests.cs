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
            new PackStreamMemorySerializer(new PackStreamReaderWriterFactory())));

    private static readonly byte[] KnownAnswerBytes =
    [
        0x01, // Encrypted Value Encoding Version
        ..EncryptedStructureConformanceTests.KnownAnswerBytes
    ];

    [Fact]
    public void Encode_ProducesTheExactKnownAnswerByteSequence()
    {
        var bytes = _subject.Encode(EncryptedStructureConformanceTests.KnownAnswerStructure());

        bytes.Should().Equal(KnownAnswerBytes);
    }

    [Fact]
    public void Decode_ParsesTheExactKnownAnswerByteSequence()
    {
        var result = _subject.Decode(KnownAnswerBytes);

        result.Should()
            .BeEquivalentTo(EncryptedStructureConformanceTests.KnownAnswerStructure(), opt => opt.ComparingByMembers<EncryptedStructure>());
    }
}
