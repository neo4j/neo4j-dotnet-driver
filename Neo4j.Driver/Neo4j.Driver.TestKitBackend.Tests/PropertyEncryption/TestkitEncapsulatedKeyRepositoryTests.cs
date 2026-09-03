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
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.PropertyEncryption;

public class TestkitEncapsulatedKeyRepositoryTests
{
    private static readonly byte[] Encapsulation = [1, 2, 3];
    private static readonly Dictionary<string, string> Metadata = new() { ["iv"] = "abc" };

    private readonly TestkitEncapsulatedKeyRepository _repository = new();

    private Task<EncapsulatedKey> Save(string? alias)
    {
        return _repository.SaveAsync(
            alias,
            Encapsulation,
            Metadata,
            TestContext.Current.CancellationToken);
    }

    private Task<EncapsulatedKey> FindByAlias(string alias)
    {
        return _repository.FindAsync(
            new KeyReference(alias, KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);
    }

    private Task<EncapsulatedKey> FindById(string id)
    {
        return _repository.FindAsync(
            new KeyReference(id, KeyReferenceType.Id),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Finds_a_saved_key_by_its_alias()
    {
        var saved = await Save("k1");

        var found = await FindByAlias("k1");

        found.Should().BeEquivalentTo(saved);
    }

    [Fact]
    public async Task Finds_a_saved_key_by_its_id()
    {
        var saved = await Save("k1");

        var found = await FindById(saved.Id);

        found.Should().BeEquivalentTo(saved);
    }

    [Fact]
    public async Task Assigns_a_distinct_id_to_each_saved_key()
    {
        var first = await Save("k1");
        var second = await Save("k2");

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task Imports_a_key_under_the_id_it_was_given()
    {
        var imported = _repository.Import("testkit-key", "k1", Encapsulation, Metadata);

        var found = await FindById("testkit-key");

        imported.Id.Should().Be("testkit-key");
        found.Should().BeEquivalentTo(imported);
    }

    [Fact]
    public async Task Binds_the_alias_of_an_imported_key()
    {
        var imported = _repository.Import("testkit-key", "k1", Encapsulation, Metadata);

        var found = await FindByAlias("k1");

        found.Should().BeEquivalentTo(imported);
    }

    [Fact]
    public async Task Throws_when_the_id_is_unknown()
    {
        var act = () => FindById("nope");

        await act.Should().ThrowAsync<EncapsulatedKeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_when_the_alias_is_unknown()
    {
        var act = () => FindByAlias("nope");

        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
    }

    [Fact]
    public async Task Saving_moves_an_alias_off_its_previous_key()
    {
        var first = await Save("k1");
        var second = await Save("k1");

        var aliased = await FindByAlias("k1");
        var abandoned = await FindById(first.Id);

        aliased.Id.Should().Be(second.Id);
        abandoned.Alias.Should().BeNull();
    }

    [Fact]
    public async Task Importing_moves_an_alias_off_its_previous_key()
    {
        var saved = await Save("k1");

        _repository.Import("testkit-key", "k1", Encapsulation, Metadata);

        var aliased = await FindByAlias("k1");
        var abandoned = await FindById(saved.Id);

        aliased.Id.Should().Be("testkit-key");
        abandoned.Alias.Should().BeNull();
    }
}
