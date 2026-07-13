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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class InMemoryEncapsulatedKeyRepositoryTests
{
    private static readonly byte[] Encapsulation = [1, 2, 3, 4];

    private static readonly IReadOnlyDictionary<string, string> Metadata =
        new Dictionary<string, string> { ["iv"] = "abc" };

    private readonly AutoMocker _autoMock = new(MockBehavior.Loose);

    private InMemoryEncapsulatedKeyRepository CreateSubject()
    {
        return _autoMock.CreateInstance<InMemoryEncapsulatedKeyRepository>();
    }

    private void SetGeneratedIds(params string[] ids)
    {
        var setup = _autoMock.GetMock<IKeyIdGenerator>().SetupSequence(g => g.Get());
        foreach (var id in ids)
        {
            setup = setup.Returns(id);
        }
    }

    [Fact]
    public async Task Save_UsesTheGeneratedIdAndPreservesTheStoredData()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        string[] aliases = ["primary"];
        HashSet<string> aliasSet = new(aliases);

        var saved = await subject.SaveAsync(aliasSet, Encapsulation, Metadata);

        saved.Id.Should().Be("key-1");
        saved.Aliases.Should().BeEquivalentTo(aliases);
        saved.Encapsulation.Should().Equal(Encapsulation);
        saved.Metadata.Should().Equal(Metadata);
    }

    [Fact]
    public async Task Save_UsesAFreshIdFromTheGeneratorForEachKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        HashSet<string> aliasSet = [];

        var first = await subject.SaveAsync(aliasSet, Encapsulation, Metadata);
        var second = await subject.SaveAsync(aliasSet, Encapsulation, Metadata);

        first.Id.Should().Be("key-1");
        second.Id.Should().Be("key-2");
    }

    [Fact]
    public async Task FindById_ReturnsTheSavedKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        HashSet<string> aliasSet = ["primary"];
        var saved = await subject.SaveAsync(aliasSet, Encapsulation, Metadata);

        var found = await subject.FindByIdAsync("key-1");

        found.Should().BeEquivalentTo(saved);
    }

    [Fact]
    public async Task FindByAlias_ReturnsTheKeySavedUnderThatAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        HashSet<string> aliasSet = ["primary", "secondary"];
        await subject.SaveAsync(aliasSet, Encapsulation, Metadata);

        var byFirst = await subject.FindByAliasAsync("primary");
        var bySecond = await subject.FindByAliasAsync("secondary");

        byFirst.Id.Should().Be("key-1");
        bySecond.Id.Should().Be("key-1");
    }

    [Fact]
    public async Task FindById_ThrowsWhenTheKeyIsUnknown()
    {
        var subject = CreateSubject();

        var act = () => subject.FindByIdAsync("missing");

        await act.Should().ThrowAsync<EncapsulatedKeyNotFoundException>();
    }

    [Fact]
    public async Task FindByAlias_ThrowsWhenTheAliasIsUnknown()
    {
        var subject = CreateSubject();

        var act = () => subject.FindByAliasAsync("missing");

        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
    }

    [Fact]
    public async Task AddAliasById_MakesTheKeyDiscoverableByTheNewAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        HashSet<string> aliasSet = ["primary"];
        await subject.SaveAsync(aliasSet, Encapsulation, Metadata);

        await subject.AddAliasByIdAsync("key-1", "extra");

        var found = await subject.FindByAliasAsync("extra");
        found.Id.Should().Be("key-1");
        found.Aliases.Should().Contain("extra");
    }

    [Fact]
    public async Task AddAliasById_ThrowsWhenTheIdIsUnknown()
    {
        var subject = CreateSubject();

        var act = () => subject.AddAliasByIdAsync("missing", "extra");

        await act.Should().ThrowAsync<EncapsulatedKeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAliasById_RemovesTheAliasButKeepsTheKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        HashSet<string> aliasSet = ["primary"];
        await subject.SaveAsync(aliasSet, Encapsulation, Metadata);

        await subject.DeleteAliasByIdAsync("key-1", "primary");

        var byId = await subject.FindByIdAsync("key-1");
        byId.Aliases.Should().NotContain("primary");

        var act = () => subject.FindByAliasAsync("primary");
        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
    }

    [Fact]
    public async Task DeleteAliasById_ThrowsWhenTheAliasIsNotBoundToTheKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        HashSet<string> aliasSet = ["primary"];
        await subject.SaveAsync(aliasSet, Encapsulation, Metadata);

        var act = () => subject.DeleteAliasByIdAsync("key-1", "never-added");

        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
    }

    [Fact]
    public async Task DeleteById_RemovesTheKeyAndItsAliases()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        HashSet<string> aliasSet = ["primary"];
        await subject.SaveAsync(aliasSet, Encapsulation, Metadata);

        await subject.DeleteByIdAsync("key-1");

        var byId = () => subject.FindByIdAsync("key-1");
        await byId.Should().ThrowAsync<EncapsulatedKeyNotFoundException>();

        var byAlias = () => subject.FindByAliasAsync("primary");
        await byAlias.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
    }

    [Fact]
    public async Task DeleteById_ThrowsWhenTheIdIsUnknown()
    {
        var subject = CreateSubject();

        var act = () => subject.DeleteByIdAsync("missing");

        await act.Should().ThrowAsync<EncapsulatedKeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteById_ThenTheAliasCanBeReusedByAnotherKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        HashSet<string> firstAliases = ["primary"];
        await subject.SaveAsync(firstAliases, Encapsulation, Metadata);
        await subject.DeleteByIdAsync("key-1");

        HashSet<string> secondAliases = ["primary"];
        await subject.SaveAsync(secondAliases, Encapsulation, Metadata);

        var byAlias = await subject.FindByAliasAsync("primary");
        byAlias.Id.Should().Be("key-2");
    }

    [Fact]
    public async Task AddAliasById_MovesTheAliasFromAnotherKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        HashSet<string> firstAliases = ["shared"];
        await subject.SaveAsync(firstAliases, Encapsulation, Metadata);
        HashSet<string> secondAliases = [];
        await subject.SaveAsync(secondAliases, Encapsulation, Metadata);

        await subject.AddAliasByIdAsync("key-2", "shared");

        var byAlias = await subject.FindByAliasAsync("shared");
        byAlias.Id.Should().Be("key-2");

        var losing = await subject.FindByIdAsync("key-1");
        losing.Aliases.Should().NotContain("shared");

        var gaining = await subject.FindByIdAsync("key-2");
        gaining.Aliases.Should().Contain("shared");
    }

    [Fact]
    public async Task AddAliasById_IsIdempotentWhenTheAliasIsAlreadyOnTheKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        HashSet<string> aliasSet = ["primary"];
        await subject.SaveAsync(aliasSet, Encapsulation, Metadata);

        await subject.AddAliasByIdAsync("key-1", "primary");

        var key = await subject.FindByIdAsync("key-1");
        key.Aliases.Should().BeEquivalentTo("primary");

        var byAlias = await subject.FindByAliasAsync("primary");
        byAlias.Id.Should().Be("key-1");
    }

    [Fact]
    public async Task Save_MovesAnAliasAlreadyOwnedByAnotherKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        HashSet<string> firstAliases = ["shared"];
        await subject.SaveAsync(firstAliases, Encapsulation, Metadata);

        HashSet<string> secondAliases = ["shared"];
        await subject.SaveAsync(secondAliases, Encapsulation, Metadata);

        var byAlias = await subject.FindByAliasAsync("shared");
        byAlias.Id.Should().Be("key-2");

        var losing = await subject.FindByIdAsync("key-1");
        losing.Aliases.Should().NotContain("shared");
    }

    [Fact]
    public async Task Save_TakesASnapshotOfTheAliases()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        HashSet<string> aliasSet = ["primary"];
        var saved = await subject.SaveAsync(aliasSet, Encapsulation, Metadata);

        aliasSet.Add("sneaked-in");

        saved.Aliases.Should().BeEquivalentTo("primary");

        var act = () => subject.FindByAliasAsync("sneaked-in");
        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
    }
}
