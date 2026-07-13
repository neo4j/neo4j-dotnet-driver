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

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EncryptedStructureCodecTests
{
    private readonly EncryptedStructureCodec _subject = new(new MessageFormatFactory(TestDriverContext.MockContext));

    private static EncryptedStructure Sample() => new(
        ProfileName: "Envelope",
        CipherOutput: new byte[] { 0xDE, 0xAD, 0xBE, 0xEF },
        TypeName: "Integer",
        TypeProtocolMajor: 6,
        TypeProtocolMinor: 0,
        Metadata: new Dictionary<string, object>
        {
            ["keyId"] = "key-1",
            ["iv"] = new byte[] { 1, 2, 3 }
        });

    [Fact]
    public void Encode_ThenDecode_RoundTripsAllFields()
    {
        var structure = Sample();

        var result = _subject.Decode(_subject.Encode(structure));

        result.Should().NotBeSameAs(structure);
        result.Should().BeEquivalentTo(structure, opt => opt.ComparingByMembers<EncryptedStructure>());
    }

    [Fact]
    public void Encode_WritesEncryptedStructHeader()
    {
        var bytes = _subject.Encode(Sample());

        var marker = bytes[0];
        var (hi, lo) = (marker & 0xF0, marker & 0x0F);
        hi.Should().Be(PackStream.TinyStruct);
        lo.Should().Be(7);
    }

    [Fact]
    public void Encode_WritesVersionAsFirstField()
    {
        var bytes = _subject.Encode(Sample());

        bytes[1].Should().Be(0x65);
        bytes[2].Should().Be(0x01);
    }
}
