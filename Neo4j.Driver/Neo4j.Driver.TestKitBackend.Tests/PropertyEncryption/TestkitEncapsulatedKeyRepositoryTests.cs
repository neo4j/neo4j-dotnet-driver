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

    private Task<EncapsulatedKeyRecord> Create(string? alias)
    {
        return _repository.CreateAsync(
            alias,
            Encapsulation,
            Metadata,
            TestContext.Current.CancellationToken);
    }

    private Task<EncapsulatedKeyRecord?> FindByAlias(string alias)
    {
        return _repository.FindByAliasAsync(
            alias,
            TestContext.Current.CancellationToken);
    }

    private Task<EncapsulatedKeyRecord?> FindById(string id)
    {
        return _repository.FindByIdAsync(
            id,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Finds_a_saved_key_by_its_alias()
    {
        var saved = await Create("k1");

        var found = await FindByAlias("k1");

        found.Should().BeEquivalentTo(saved);
    }

    [Fact]
    public async Task Finds_a_saved_key_by_its_id()
    {
        var saved = await Create("k1");

        var found = await FindById(saved.Id);

        found.Should().BeEquivalentTo(saved);
    }

    [Fact]
    public async Task Assigns_a_distinct_id_to_each_saved_key()
    {
        var first = await Create("k1");
        var second = await Create("k2");

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task Imports_a_key_under_the_id_it_was_given()
    {
        var imported = await _repository.ImportAsync("testkit-key", "k1", Encapsulation, Metadata);

        var found = await FindById("testkit-key");

        imported.Id.Should().Be("testkit-key");
        found.Should().BeEquivalentTo(imported);
    }

    [Fact]
    public async Task Binds_the_alias_of_an_imported_key()
    {
        var imported = await _repository.ImportAsync("testkit-key", "k1", Encapsulation, Metadata);

        var found = await FindByAlias("k1");

        found.Should().BeEquivalentTo(imported);
    }

    [Fact]
    public async Task Returns_null_when_the_id_is_unknown()
    {
        var found = await FindById("nope");

        found.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_the_alias_is_unknown()
    {
        var found = await FindByAlias("nope");

        found.Should().BeNull();
    }

    [Fact]
    public async Task Creating_throws_when_another_key_already_holds_the_alias()
    {
        await Create("k1");

        var act = () => Create("k1");

        await act.Should().ThrowAsync<EncapsulatedAliasInUseException>().WithMessage("*k1*");
    }

    [Fact]
    public async Task Importing_throws_when_another_key_already_holds_the_alias()
    {
        await Create("k1");

        var act = () => _repository.ImportAsync("testkit-key", "k1", Encapsulation, Metadata);

        await act.Should().ThrowAsync<EncapsulatedAliasInUseException>().WithMessage("*k1*");
    }
}
