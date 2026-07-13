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
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class PlaintextSerializerTests
{
    private readonly PlaintextSerializer _subject = new(new MessageFormatFactory(TestDriverContext.MockContext));

    // just a few sanity checks - serialization is delegated to PackStreamReader/Writer
    // which are both fully tested in PackStreamTestSpecs   
    public static IEnumerable<object[]> LockVectors => new[]
    {
        new object[] { true, new byte[] { 0xC3 } },
        new object[] { false, new byte[] { 0xC2 } },
        new object[] { 42L, new byte[] { 0x2A } },
        new object[] { 1234L, new byte[] { 0xC9, 0x04, 0xD2 } },
        new object[] { 1.5, new byte[] { 0xC1, 0x3F, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } },
        new object[] { "hello", new byte[] { 0x85, 0x68, 0x65, 0x6C, 0x6C, 0x6F } },
        new object[] { "", new byte[] { 0x80 } },
        new object[] { new byte[] { 0x01, 0x02, 0x03 }, new byte[] { 0xCC, 0x03, 0x01, 0x02, 0x03 } },
        new object[] { new List<long> { 1L, 2L, 3L }, new byte[] { 0x93, 0x01, 0x02, 0x03 } }
    };

    [Theory]
    [MemberData(nameof(LockVectors))]
    public void Serialize_ProducesPackStreamBytes(object value, byte[] expected)
    {
        _subject.Serialize(value).Should().Equal(expected);
    }

    [Theory]
    [MemberData(nameof(LockVectors))]
    public void Deserialize_ReadsPackStreamBytesBackToValue(object value, byte[] plaintext)
    {
        _subject.Deserialize(plaintext).Should().BeEquivalentTo(value);
    }
}
