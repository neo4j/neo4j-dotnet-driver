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
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.PropertyEncryption;

public class DriverEncryptionObjectStoreTests
{
    private readonly DriverEncryptionObjectStore _store = new();
    private readonly IDriver _driver = Mock.Of<IDriver>();

    private (IFixedIvProvider IvProvider, IReadOnlyDictionary<string, ITestkitEncapsulatedKeyRepository> Repositories)
        StoreObjects(IDriver driver, params string[] profileNames)
    {
        var ivProvider = Mock.Of<IFixedIvProvider>();
        var repositories = profileNames.ToDictionary(
            name => name,
            _ => Mock.Of<ITestkitEncapsulatedKeyRepository>());

        _store.StoreObjects(driver, ivProvider, repositories);
        return (ivProvider, repositories);
    }

    [Fact]
    public void Returns_the_iv_provider_that_was_added_for_the_driver()
    {
        var stored = StoreObjects(_driver);

        var ivProvider = _store.GetIvProvider(_driver);

        ivProvider.Should().BeSameAs(stored.IvProvider);
    }

    [Fact]
    public void Returns_the_repository_added_under_the_profile_name()
    {
        var stored = StoreObjects(_driver, "p1", "p2");

        var repository = _store.GetRepository(_driver, "p2");

        repository.Should().BeSameAs(stored.Repositories["p2"]);
    }

    [Fact]
    public void Keeps_each_driver_fixtures_separate()
    {
        var first = StoreObjects(_driver, "p1");
        var otherDriver = Mock.Of<IDriver>();
        var second = StoreObjects(otherDriver, "p1");

        var repository = _store.GetRepository(otherDriver, "p1");

        repository.Should().BeSameAs(second.Repositories["p1"]);
        repository.Should().NotBeSameAs(first.Repositories["p1"]);
    }

    [Fact]
    public void Throws_when_the_driver_has_no_fixtures()
    {
        var act = () => _store.GetIvProvider(_driver);

        act.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void Throws_when_the_profile_name_is_unknown()
    {
        StoreObjects(_driver, "p1");

        var act = () => _store.GetRepository(_driver, "nope");

        act.Should().Throw<TestKitProtocolException>();
    }
}
