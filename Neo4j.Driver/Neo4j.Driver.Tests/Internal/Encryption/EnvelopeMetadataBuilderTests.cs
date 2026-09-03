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

public class EnvelopeMetadataBuilderTests
{
    private readonly EnvelopeMetadataBuilder _subject = new();

    [Fact]
    public void Build_ReturnsAllFieldsUnderTheAgreedKeys()
    {
        var metadata = new EnvelopeMetadata(
            "key-1",
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5 },
            6,
            0,
            new Dictionary<string, object>());

        var result = _subject.Build(metadata);

        result["key_id"].Should().Be("key-1");
        result["iv"].Should().Be(metadata.Iv);
        result["aad"].Should().Be(metadata.Aad);

        // integers must be written as long: that's what PackStream decoding hands back,
        // and the extractor requires the exact type
        result["aad_encoding_scheme_major"].Should().Be(6L);
        result["aad_encoding_scheme_minor"].Should().Be(0L);
    }

    [Fact]
    public void Build_OmitsAadAndEncodingSchemeFields_WhenAadIsEmpty()
    {
        var metadata = new EnvelopeMetadata(
            "key-1",
            new byte[] { 1, 2, 3 },
            [],
            6,
            0,
            new Dictionary<string, object>());

        var result = _subject.Build(metadata);

        result.Keys.Should().BeEquivalentTo("key_id", "iv");
    }

    [Fact]
    public void Build_DoesNotIncludeEncapsulationOptions()
    {
        var metadata = new EnvelopeMetadata(
            "key-1",
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5 },
            6,
            0,
            new Dictionary<string, object> { ["region"] = "eu-west-1" });

        var result = _subject.Build(metadata);

        result.Should().NotContainKey("opt.region");
        result.Should().HaveCount(5);
    }
}
