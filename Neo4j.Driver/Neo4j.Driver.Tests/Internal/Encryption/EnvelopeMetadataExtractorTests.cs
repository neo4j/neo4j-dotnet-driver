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

public class EnvelopeMetadataExtractorTests
{
    private readonly EnvelopeMetadataExtractor _subject = new();

    private static Dictionary<string, object> ValidMetadata() => new()
    {
        ["key_id"] = "key-1",
        ["iv"] = new byte[] { 1, 2, 3 },
        ["aad"] = new byte[] { 4, 5 },
        ["aad_protocol_major"] = 6,
        ["aad_protocol_minor"] = 0
    };

    [Fact]
    public void Extract_AllFields_ReturnsMetadata()
    {
        var result = _subject.Extract(ValidMetadata());

        result.KeyId.Should().Be("key-1");
        result.Iv.Should().Equal(1, 2, 3);
        result.Aad.Should().Equal(4, 5);
        result.AadProtocolMajor.Should().Be(6);
        result.AadProtocolMinor.Should().Be(0);
        result.EncapsulationOptions.Should().BeEmpty();
    }

    [Fact]
    public void Extract_MissingAadProtocolVersion_DefaultsToSixZero()
    {
        var metadata = ValidMetadata();
        metadata.Remove("aad_protocol_major");
        metadata.Remove("aad_protocol_minor");

        var result = _subject.Extract(metadata);

        result.AadProtocolMajor.Should().Be(6);
        result.AadProtocolMinor.Should().Be(0);
    }

    [Fact]
    public void Extract_MissingKeyId_Throws()
    {
        var metadata = ValidMetadata();
        metadata.Remove("key_id");

        var act = () => _subject.Extract(metadata);

        act.Should().Throw<MetadataExtractionException>().WithMessage("*key_id*");
    }

    [Fact]
    public void Extract_MissingIv_Throws()
    {
        var metadata = ValidMetadata();
        metadata.Remove("iv");

        var act = () => _subject.Extract(metadata);

        act.Should().Throw<MetadataExtractionException>().WithMessage("*iv*");
    }

    [Fact]
    public void Extract_WrongTypeIv_Throws()
    {
        var metadata = ValidMetadata();
        metadata["iv"] = "not bytes";

        var act = () => _subject.Extract(metadata);

        act.Should().Throw<MetadataExtractionException>().WithMessage("*iv*");
    }

    [Fact]
    public void Extract_MissingAad_DefaultsToEmpty()
    {
        var metadata = ValidMetadata();
        metadata.Remove("aad");

        _subject.Extract(metadata).Aad.Should().BeEmpty();
    }

    [Fact]
    public void Extract_OptPrefixedKeys_AreFlatMappedWithPrefixStripped()
    {
        var metadata = ValidMetadata();
        metadata["opt.region"] = "eu-west-1";
        metadata["opt.kekId"] = "kek-42";

        var result = _subject.Extract(metadata);

        result.EncapsulationOptions.Should().HaveCount(2);
        result.EncapsulationOptions["region"].Should().Be("eu-west-1");
        result.EncapsulationOptions["kekId"].Should().Be("kek-42");
    }
}
