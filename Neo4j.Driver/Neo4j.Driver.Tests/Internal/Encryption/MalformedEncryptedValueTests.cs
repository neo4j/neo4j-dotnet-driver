// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class MalformedEncryptedValueTests
{
    private const long AllocationBudgetBytes = 1024 * 1024;

    private readonly EncryptedValueBytesCodec _subject = new(
        new EncryptedStructureCodec(
            new MessageFormatFactory(TestDriverContext.MockContext),
            new PackStreamMemorySerializer(new PackStreamReaderWriterFactory())));

    private static readonly byte[] HeaderThroughNameAndCipherOutput =
    [
        0x01, // Encrypted Value Encoding Version
        0xB8, 0x65, // structure of 8 fields, Encrypted signature
        0x81, 0x78, // profile type "x"
        0x01, // profile version
        0x81, 0x6E, // profile name "n"
    ];

    private static readonly byte[] HeaderThroughSchemeVersions =
    [
        ..HeaderThroughNameAndCipherOutput,
        0xCC, 0x00, // empty cipher output
        0x81, 0x74, // type name "t"
        0x01, // scheme major
        0x00, // scheme minor
    ];

    public static TheoryData<string, byte[]> DeclaredSizesBeyondTheInput()
    {
        return new TheoryData<string, byte[]>
        {
            { "a 1 GiB string with no payload", [0x01, 0xB8, 0x65, 0xD2, 0x40, 0x00, 0x00, 0x00] },
            { "an int.MaxValue string", [0x01, 0xB8, 0x65, 0xD2, 0x7F, 0xFF, 0xFF, 0xFF] },
            {
                "an int.MaxValue byte array",
                [..HeaderThroughNameAndCipherOutput, 0xCE, 0x7F, 0xFF, 0xFF, 0xFF]
            },
            {
                "an int.MaxValue map of metadata",
                [..HeaderThroughSchemeVersions, 0xDA, 0x7F, 0xFF, 0xFF, 0xFF]
            },
            {
                "a million-entry map of metadata with no entries",
                [..HeaderThroughSchemeVersions, 0xDA, 0x00, 0x0F, 0x42, 0x40]
            },
            {
                "an int.MaxValue list inside the metadata",
                [..HeaderThroughSchemeVersions, 0xA1, 0x81, 0x6B, 0xD6, 0x7F, 0xFF, 0xFF, 0xFF]
            },
        };
    }

    private static byte[] WithMetadataNestedLists(int depth)
    {
        byte[] singleEntryMapWithKeyK = [0xA1, 0x81, 0x6B];
        var nesting = new byte[depth];
        Array.Fill(nesting, (byte)0x91);
        nesting[^1] = 0x90;
        return [..HeaderThroughSchemeVersions, ..singleEntryMapWithKeyK, ..nesting];
    }

    [Theory]
    [MemberData(nameof(DeclaredSizesBeyondTheInput))]
    public void Decode_RejectsASizeBeyondTheRemainingInputWithoutAllocatingIt(string _, byte[] bytes)
    {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        var act = () => _subject.Decode(bytes);

        act.Should().Throw<PropertyEncryptionException>();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        allocated.Should().BeLessThan(AllocationBudgetBytes);
    }

    public static TheoryData<string, byte[]> InputsThatEndOrOverflowWithoutAProtocolError()
    {
        return new TheoryData<string, byte[]>
        {
            { "only the encoding version", [0x01] },
            { "a structure header and signature with no fields", [0x01, 0xB8, 0x65] },
            { "an Int32 profile version cut short", [0x01, 0xB8, 0x65, 0x81, 0x78, 0xCA, 0x00, 0x00] },
            { "an Int64 profile version", [0x01, 0xB8, 0x65, 0x81, 0x78, 0xCB, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00] },
        };
    }

    [Theory]
    [MemberData(nameof(InputsThatEndOrOverflowWithoutAProtocolError))]
    public void Decode_ThrowsPropertyEncryptionExceptionWhenTheReaderEndsOrOverflows(string _, byte[] bytes)
    {
        var act = () => _subject.Decode(bytes);

        act.Should().Throw<PropertyEncryptionException>();
    }

    [Fact]
    public void Decode_AcceptsMetadataNestedAtTheLimit()
    {
        var bytes = WithMetadataNestedLists(PackStreamReader.MaxNestingDepth);

        var act = () => _subject.Decode(bytes);

        act.Should().NotThrow();
    }

    [Fact]
    public void Decode_RejectsMetadataNestedBeyondTheLimit()
    {
        var bytes = WithMetadataNestedLists(PackStreamReader.MaxNestingDepth + 1);

        var act = () => _subject.Decode(bytes);

        act.Should().Throw<PropertyEncryptionException>();
    }

    [Fact]
    public void Decode_RejectsMetadataNestedFarEnoughToOverflowTheStack()
    {
        var bytes = WithMetadataNestedLists(1_000_000);

        var act = () => _subject.Decode(bytes);

        act.Should().Throw<PropertyEncryptionException>();
    }
}
