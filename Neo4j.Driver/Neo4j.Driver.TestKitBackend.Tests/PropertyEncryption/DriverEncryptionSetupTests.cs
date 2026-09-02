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

using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.PropertyEncryption;

public class DriverEncryptionSetupTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<DriverEncryptionSetup>();

    public DriverEncryptionSetupTests()
    {
        _autoMocker.Use<Func<byte[]?, IKeyEncapsulationService>>(_ => Mock.Of<IKeyEncapsulationService>());
        _autoMocker.Use<Func<ITestkitEncapsulatedKeyRepository>>(() => Mock.Of<ITestkitEncapsulatedKeyRepository>());
    }

    private DriverEncryptionObjects Prepare(params PropertyEncryptionProfileInput[] profiles)
    {
        var setup = _autoMocker.CreateInstance<DriverEncryptionSetup>();
        return setup.Prepare(profiles);
    }

    [Fact]
    public void Requests_a_key_encapsulation_service_with_a_null_kek_when_none_is_supplied()
    {
        byte[]? requestedKek = null;
        var requested = false;
        _autoMocker.Use<Func<byte[]?, IKeyEncapsulationService>>(
            kek =>
            {
                requested = true;
                requestedKek = kek;
                return Mock.Of<IKeyEncapsulationService>();
            });

        Prepare(new PropertyEncryptionProfileInput("profile-1", null));

        requested.Should().BeTrue();
        requestedKek.Should().BeNull();
    }

    [Fact]
    public void Passes_the_kek_bytes_through_to_the_key_encapsulation_service_factory()
    {
        byte[]? requestedKek = null;
        _autoMocker.Use<Func<byte[]?, IKeyEncapsulationService>>(
            kek =>
            {
                requestedKek = kek;
                return Mock.Of<IKeyEncapsulationService>();
            });

        Prepare(new PropertyEncryptionProfileInput("profile-1", new HexBytes([0x01, 0x02, 0x0a, 0xff])));

        requestedKek.Should().Equal(0x01, 0x02, 0x0a, 0xff);
    }

    [Fact]
    public void Keys_the_repository_dictionary_by_profile_name()
    {
        var repository1 = Mock.Of<ITestkitEncapsulatedKeyRepository>();
        var repository2 = Mock.Of<ITestkitEncapsulatedKeyRepository>();
        var repositories = new Queue<ITestkitEncapsulatedKeyRepository>(new[] { repository1, repository2 });
        _autoMocker.Use<Func<ITestkitEncapsulatedKeyRepository>>(repositories.Dequeue);

        var result = Prepare(
            new PropertyEncryptionProfileInput("profile-1", null),
            new PropertyEncryptionProfileInput("profile-2", null));

        result.RepositoriesByProfileName["profile-1"].Should().BeSameAs(repository1);
        result.RepositoriesByProfileName["profile-2"].Should().BeSameAs(repository2);
    }

    [Fact]
    public void Returns_a_profile_for_each_input_with_the_matching_name()
    {
        var result = Prepare(
            new PropertyEncryptionProfileInput("profile-1", null),
            new PropertyEncryptionProfileInput("profile-2", null));

        var names = result.Profiles.Select(p => p.Name);

        names.Should().Equal("profile-1", "profile-2");
    }
}
