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
using Moq.Language;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.Tests.TestUtil;
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
        _autoMock
            .GetMock<IKeyIdGenerator>()
            .SetupSequence(g => g.Get())
            .ReturnsSequence(ids);
    }

    [Fact]
    public async Task Save_UsesTheGeneratedIdAndPreservesTheStoredData()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        var saved = await subject.SaveAsync("primary", Encapsulation, Metadata);

        saved.Id.Should().Be("key-1");
        saved.Alias.Should().Be("primary");
        saved.Encapsulation.Should().Equal(Encapsulation);
        saved.Metadata.Should().Equal(Metadata);
    }

    [Fact]
    public async Task Save_WithNoAliasSavesAnUnaliasedKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        var saved = await subject.SaveAsync(null, Encapsulation, Metadata);

        saved.Alias.Should().BeNull();
    }

    [Fact]
    public async Task Save_UsesAFreshIdFromTheGeneratorForEachKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        var first = await subject.SaveAsync(null, Encapsulation, Metadata);
        var second = await subject.SaveAsync(null, Encapsulation, Metadata);

        first.Id.Should().Be("key-1");
        second.Id.Should().Be("key-2");
    }

    [Fact]
    public async Task FindById_ReturnsTheSavedKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        var saved = await subject.SaveAsync("primary", Encapsulation, Metadata);

        var found = await subject.FindAsync(new KeyReference("key-1", KeyReferenceType.Id));

        found.Should().BeEquivalentTo(saved);
    }

    [Fact]
    public async Task FindByAlias_ReturnsTheKeySavedUnderThatAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.SaveAsync("primary", Encapsulation, Metadata);

        var found = await subject.FindAsync(new KeyReference("primary", KeyReferenceType.Alias));

        found.Id.Should().Be("key-1");
    }

    [Fact]
    public async Task FindById_ThrowsWhenTheKeyIsUnknown()
    {
        var subject = CreateSubject();

        var act = () => subject.FindAsync(new KeyReference("missing", KeyReferenceType.Id));

        await act.Should().ThrowAsync<EncapsulatedKeyNotFoundException>();
    }

    [Fact]
    public async Task FindByAlias_ThrowsWhenTheAliasIsUnknown()
    {
        var subject = CreateSubject();

        var act = () => subject.FindAsync(new KeyReference("missing", KeyReferenceType.Alias));

        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
    }

    [Fact]
    public async Task AddAliasById_MakesTheKeyDiscoverableByTheNewAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.SaveAsync("primary", Encapsulation, Metadata);

        await subject.AddAliasByIdAsync("key-1", "extra");

        var found = await subject.FindAsync(new KeyReference("extra", KeyReferenceType.Alias));
        found.Id.Should().Be("key-1");
        found.Alias.Should().Be("extra");
    }

    [Fact]
    public async Task AddAliasById_ReplacesAnyExistingAliasOnTheSameKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.SaveAsync("primary", Encapsulation, Metadata);

        await subject.AddAliasByIdAsync("key-1", "extra");

        var key = await subject.FindAsync(new KeyReference("key-1", KeyReferenceType.Id));
        key.Alias.Should().Be("extra");

        var act = () => subject.FindAsync(new KeyReference("primary", KeyReferenceType.Alias));
        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
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

        await subject.SaveAsync("primary", Encapsulation, Metadata);

        await subject.DeleteAliasByIdAsync("key-1", "primary");

        var byId = await subject.FindAsync(new KeyReference("key-1", KeyReferenceType.Id));
        byId.Alias.Should().BeNull();

        var act = () => subject.FindAsync(new KeyReference("primary", KeyReferenceType.Alias));
        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
    }

    [Fact]
    public async Task DeleteAliasById_ThrowsWhenTheAliasIsNotBoundToTheKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.SaveAsync("primary", Encapsulation, Metadata);

        var act = () => subject.DeleteAliasByIdAsync("key-1", "never-added");

        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
    }

    [Fact]
    public async Task DeleteById_RemovesTheKeyAndItsAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.SaveAsync("primary", Encapsulation, Metadata);

        await subject.DeleteByIdAsync("key-1");

        var byId = () => subject.FindAsync(new KeyReference("key-1", KeyReferenceType.Id));
        await byId.Should().ThrowAsync<EncapsulatedKeyNotFoundException>();

        var byAlias = () => subject.FindAsync(new KeyReference("primary", KeyReferenceType.Alias));
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

        await subject.SaveAsync("primary", Encapsulation, Metadata);
        await subject.DeleteByIdAsync("key-1");

        await subject.SaveAsync("primary", Encapsulation, Metadata);

        var byAlias = await subject.FindAsync(new KeyReference("primary", KeyReferenceType.Alias));
        byAlias.Id.Should().Be("key-2");
    }

    [Fact]
    public async Task AddAliasById_MovesTheAliasFromAnotherKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        await subject.SaveAsync("shared", Encapsulation, Metadata);
        await subject.SaveAsync(null, Encapsulation, Metadata);

        await subject.AddAliasByIdAsync("key-2", "shared");

        var byAlias = await subject.FindAsync(new KeyReference("shared", KeyReferenceType.Alias));
        byAlias.Id.Should().Be("key-2");

        var losing = await subject.FindAsync(new KeyReference("key-1", KeyReferenceType.Id));
        losing.Alias.Should().BeNull();

        var gaining = await subject.FindAsync(new KeyReference("key-2", KeyReferenceType.Id));
        gaining.Alias.Should().Be("shared");
    }

    [Fact]
    public async Task AddAliasById_IsIdempotentWhenTheAliasIsAlreadyOnTheKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.SaveAsync("primary", Encapsulation, Metadata);

        await subject.AddAliasByIdAsync("key-1", "primary");

        var key = await subject.FindAsync(new KeyReference("key-1", KeyReferenceType.Id));
        key.Alias.Should().Be("primary");

        var byAlias = await subject.FindAsync(new KeyReference("primary", KeyReferenceType.Alias));
        byAlias.Id.Should().Be("key-1");
    }

    [Fact]
    public async Task Save_MovesAnAliasAlreadyOwnedByAnotherKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        await subject.SaveAsync("shared", Encapsulation, Metadata);

        await subject.SaveAsync("shared", Encapsulation, Metadata);

        var byAlias = await subject.FindAsync(new KeyReference("shared", KeyReferenceType.Alias));
        byAlias.Id.Should().Be("key-2");

        var losing = await subject.FindAsync(new KeyReference("key-1", KeyReferenceType.Id));
        losing.Alias.Should().BeNull();
    }
}
