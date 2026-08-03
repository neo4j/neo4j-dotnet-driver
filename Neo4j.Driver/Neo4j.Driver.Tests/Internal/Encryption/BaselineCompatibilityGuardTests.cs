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
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class BaselineCompatibilityGuardTests
{
    private readonly BaselineCompatibilityGuard _subject = new();

    private static EncryptedStructure Structure(int typeSchemeMajor, int typeSchemeMinor)
    {
        return new EncryptedStructure(
            "profile-a",
            [0xC0, 0xD0],
            "VECTOR",
            typeSchemeMajor,
            typeSchemeMinor,
            new Dictionary<string, object>());
    }

    private static EnvelopeMetadata Metadata(int aadProtocolMajor, int aadProtocolMinor)
    {
        return new EnvelopeMetadata(
            "key-1",
            [1, 2, 3],
            [0xAA],
            aadProtocolMajor,
            aadProtocolMinor,
            new Dictionary<string, object>());
    }

    [Theory]
    [InlineData(7, 0, "7.0")]
    [InlineData(1, 1, "1.1")]
    public void IsUnsupportedBaselineType_BaselineNewerThanLatestKnown_ReturnsUnsupportedType(
        int typeSchemeMajor,
        int typeSchemeMinor,
        string expectedMinimumVersion)
    {
        var result = _subject.IsUnsupportedBaselineType(Structure(typeSchemeMajor, typeSchemeMinor), out var unsupported);

        result.Should().BeTrue();
        unsupported!.Name.Should().Be("VECTOR");
        unsupported.MinimumProtocolVersion.Should().Be(expectedMinimumVersion);
    }

    [Fact]
    public void IsUnsupportedBaselineType_CurrentBaseline_ReturnsFalse()
    {
        var result = _subject.IsUnsupportedBaselineType(Structure(1, 0), out var unsupported);

        result.Should().BeFalse();
        unsupported.Should().BeNull();
    }

    [Fact]
    public void EnsureAadProtocolCompatibility_NoSuppliedAad_IgnoresNewerPersistedBaseline()
    {
        var act = () => _subject.EnsureAadProtocolCompatibility(null, Metadata(7, 0));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureAadProtocolCompatibility_SuppliedAadWithNewerPersistedBaseline_Throws()
    {
        var act = () => _subject.EnsureAadProtocolCompatibility([0x99], Metadata(7, 0));

        act.Should().Throw<ClientException>();
    }

    [Fact]
    public void EnsureAadProtocolCompatibility_SuppliedAadWithCurrentBaseline_DoesNotThrow()
    {
        var act = () => _subject.EnsureAadProtocolCompatibility([0x99], Metadata(1, 0));

        act.Should().NotThrow();
    }
}
