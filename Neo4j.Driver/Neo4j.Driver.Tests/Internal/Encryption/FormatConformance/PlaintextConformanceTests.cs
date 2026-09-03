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

public class PlaintextConformanceTests
{
    private static PlaintextCodec CreateCodec() => new(
        new MessageFormatFactory(TestDriverContext.MockContext),
        new PackStreamMemorySerializer(new PackStreamReaderWriterFactory()));

    // Hand-computed from the PackStream marker rules: one vector per marker family the
    // supported property types can produce.
    public static TheoryData<object, byte[]> SerializationTestData() => new()
    {
        { true, new byte[] { 0xC3 } },
        { false, new byte[] { 0xC2 } },
        { 42L, new byte[] { 0x2A } }, // TinyInt
        { -1L, new byte[] { 0xFF } }, // TinyInt (negative)
        { -128L, new byte[] { 0xC8, 0x80 } }, // INT_8
        { 128L, new byte[] { 0xC9, 0x00, 0x80 } }, // INT_16
        { 32768L, new byte[] { 0xCA, 0x00, 0x00, 0x80, 0x00 } }, // INT_32
        { 2147483648L, new byte[] { 0xCB, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00 } }, // INT_64
        { 3.25, new byte[] { 0xC1, 0x40, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } }, // FLOAT_64
        { "hello", new byte[] { 0x85, 0x68, 0x65, 0x6C, 0x6C, 0x6F } }, // TinyString
        { "", new byte[] { 0x80 } },
        { new byte[] { 0x01, 0x02, 0x03 }, new byte[] { 0xCC, 0x03, 0x01, 0x02, 0x03 } }, // BYTES_8
        { new List<object> { 1L, 2L, 3L }, new byte[] { 0x93, 0x01, 0x02, 0x03 } }, // TinyList
        { new List<object> { "a", "b" }, new byte[] { 0x92, 0x81, 0x61, 0x81, 0x62 } },
        { new List<object>(), new byte[] { 0x90 } }
    };

    [Theory]
    [MemberData(nameof(SerializationTestData))]
    public void Serialize_ProducesTheKnownBytes(object value, byte[] expected)
    {
        CreateCodec().Serialize(value).Should().Equal(expected);
    }

    [Theory]
    [MemberData(nameof(SerializationTestData))]
    public void Deserialize_ReadsTheKnownBytes(object expected, byte[] bytes)
    {
        CreateCodec().Deserialize(bytes).Should().BeEquivalentTo(expected);
    }
}
