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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Public.Preview.Encryption;

public class PropertyEncryptionProfileTests
{
    [Fact]
    public void Envelope_ReturnsAProfileWithTheGivenName()
    {
        var profile = PropertyEncryptionProfile.Envelope(
            "profile-name",
            Mock.Of<IKeyEncapsulationService>(),
            Mock.Of<IEncapsulatedKeyRepository>());

        profile.Name.Should().Be("profile-name");
    }

    [Fact]
    public void Envelope_ReturnsAnEnvelopeProfileCarryingTheKeyEncapsulationServiceAndRepository()
    {
        var kes = Mock.Of<IKeyEncapsulationService>();
        var repository = Mock.Of<IEncapsulatedKeyRepository>();

        var profile = PropertyEncryptionProfile.Envelope("profile-name", kes, repository);

        var envelope = profile.Should().BeAssignableTo<IEnvelopeProfile>().Subject;
        envelope.KeyEncapsulationService.Should().BeSameAs(kes);
        envelope.KeyRepository.Should().BeSameAs(repository);
    }

    [Fact]
    public void Envelope_ReturnsADistinctInstancePerCall()
    {
        var kes = Mock.Of<IKeyEncapsulationService>();
        var repository = Mock.Of<IEncapsulatedKeyRepository>();

        var first = PropertyEncryptionProfile.Envelope("profile-name", kes, repository);
        var second = PropertyEncryptionProfile.Envelope("profile-name", kes, repository);

        first.Should().NotBeSameAs(second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Envelope_WithNullOrWhitespaceName_Throws(string? name)
    {
        var act = () => PropertyEncryptionProfile.Envelope(
            name!,
            Mock.Of<IKeyEncapsulationService>(),
            Mock.Of<IEncapsulatedKeyRepository>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Envelope_WithNullKeyEncapsulationService_Throws()
    {
        var act = () => PropertyEncryptionProfile.Envelope(
            "profile-name",
            null!,
            Mock.Of<IEncapsulatedKeyRepository>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Envelope_WithNullKeyRepository_Throws()
    {
        var act = () => PropertyEncryptionProfile.Envelope(
            "profile-name",
            Mock.Of<IKeyEncapsulationService>(),
            null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
