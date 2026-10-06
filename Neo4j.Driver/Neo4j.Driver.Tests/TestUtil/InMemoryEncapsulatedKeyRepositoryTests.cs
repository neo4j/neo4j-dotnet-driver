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
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.TestUtil;

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
    public async Task Create_UsesTheGeneratedIdAndPreservesTheStoredData()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        var created = await subject.CreateAsync("primary", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        created.Id.Should().Be("key-1");
        created.Alias.Should().Be("primary");
        created.Encapsulation.Should().Equal(Encapsulation);
        created.Metadata.Should().Equal(Metadata);
    }

    [Fact]
    public async Task Create_WithNoAlias_CreatesAnUnaliasedKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        var created = await subject.CreateAsync(null, Encapsulation, Metadata, TestContext.Current.CancellationToken);

        created.Alias.Should().BeNull();
    }

    [Fact]
    public async Task Create_UsesAFreshIdFromTheGeneratorForEachKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        var first = await subject.CreateAsync(null, Encapsulation, Metadata, TestContext.Current.CancellationToken);
        var second = await subject.CreateAsync(null, Encapsulation, Metadata, TestContext.Current.CancellationToken);

        first!.Id.Should().Be("key-1");
        second!.Id.Should().Be("key-2");
    }

    [Fact]
    public async Task FindById_ReturnsTheCreatedKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        var created = await subject.CreateAsync("primary", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        var found = await subject.FindByIdAsync("key-1", TestContext.Current.CancellationToken);

        found.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task FindByAlias_ReturnsTheKeyCreatedUnderThatAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.CreateAsync("primary", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        var found = await subject.FindByAliasAsync("primary", TestContext.Current.CancellationToken);

        found!.Id.Should().Be("key-1");
    }

    [Fact]
    public async Task FindById_ReturnsNullWhenTheKeyIsUnknown()
    {
        var subject = CreateSubject();

        var found = await subject.FindByIdAsync("missing", TestContext.Current.CancellationToken);

        found.Should().BeNull();
    }

    [Fact]
    public async Task FindByAlias_ReturnsNullWhenTheAliasIsUnknown()
    {
        var subject = CreateSubject();

        var found = await subject.FindByAliasAsync("missing", TestContext.Current.CancellationToken);

        found.Should().BeNull();
    }

    [Fact]
    public async Task SetAliasById_MakesTheKeyDiscoverableByTheNewAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.CreateAsync("primary", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        await subject.SetAliasByIdAsync("key-1", "extra", TestContext.Current.CancellationToken);

        var found = await subject.FindByAliasAsync("extra", TestContext.Current.CancellationToken);
        found!.Id.Should().Be("key-1");
        found!.Alias.Should().Be("extra");
    }

    [Fact]
    public async Task SetAliasById_ReplacesAnyExistingAliasOnTheSameKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.CreateAsync("primary", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        await subject.SetAliasByIdAsync("key-1", "extra", TestContext.Current.CancellationToken);

        var key = await subject.FindByIdAsync("key-1", TestContext.Current.CancellationToken);
        key!.Alias.Should().Be("extra");

        var gone = await subject.FindByAliasAsync("primary", TestContext.Current.CancellationToken);
        gone.Should().BeNull();
    }

    [Fact]
    public async Task SetAliasById_ThrowsWhenTheIdIsUnknown()
    {
        var subject = CreateSubject();

        var act = () => subject.SetAliasByIdAsync("missing", "extra", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<EncapsulatedKeyNotFoundException>();
    }

    [Fact]
    public async Task SetAliasByIdToNull_RemovesTheAliasButKeepsTheKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.CreateAsync("primary", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        await subject.SetAliasByIdAsync("key-1", null, TestContext.Current.CancellationToken);

        var byId = await subject.FindByIdAsync("key-1", TestContext.Current.CancellationToken);
        byId!.Alias.Should().BeNull();

        var gone = await subject.FindByAliasAsync("primary", TestContext.Current.CancellationToken);
        gone.Should().BeNull();
    }

    [Fact]
    public async Task SetAliasByIdToNull_OnAKeyWithNoAlias_IsANoOp()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.CreateAsync(null, Encapsulation, Metadata, TestContext.Current.CancellationToken);

        await subject.SetAliasByIdAsync("key-1", null, TestContext.Current.CancellationToken);

        var key = await subject.FindByIdAsync("key-1", TestContext.Current.CancellationToken);
        key!.Alias.Should().BeNull();
    }

    [Fact]
    public async Task DeleteById_RemovesTheKeyAndItsAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.CreateAsync("primary", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        await subject.DeleteByIdAsync("key-1", TestContext.Current.CancellationToken);

        var byId = await subject.FindByIdAsync("key-1", TestContext.Current.CancellationToken);
        byId.Should().BeNull();

        var byAlias = await subject.FindByAliasAsync("primary", TestContext.Current.CancellationToken);
        byAlias.Should().BeNull();
    }

    [Fact]
    public async Task DeleteById_ThrowsWhenTheIdIsUnknown()
    {
        var subject = CreateSubject();

        var act = () => subject.DeleteByIdAsync("missing", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<EncapsulatedKeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteById_ThenTheAliasCanBeReusedByAnotherKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        await subject.CreateAsync("primary", Encapsulation, Metadata, TestContext.Current.CancellationToken);
        await subject.DeleteByIdAsync("key-1", TestContext.Current.CancellationToken);

        await subject.CreateAsync("primary", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        var byAlias = await subject.FindByAliasAsync("primary", TestContext.Current.CancellationToken);
        byAlias!.Id.Should().Be("key-2");
    }

    [Fact]
    public async Task SetAliasById_ThrowsWhenAnotherKeyAlreadyHoldsTheAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        await subject.CreateAsync("shared", Encapsulation, Metadata, TestContext.Current.CancellationToken);
        await subject.CreateAsync(null, Encapsulation, Metadata, TestContext.Current.CancellationToken);

        var act = () => subject.SetAliasByIdAsync("key-2", "shared", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<EncapsulatedAliasInUseException>().WithMessage("*shared*");
    }

    [Fact]
    public async Task SetAliasById_AfterTheHoldingKeyReleasesIt_BindsTheAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        await subject.CreateAsync("shared", Encapsulation, Metadata, TestContext.Current.CancellationToken);
        await subject.CreateAsync(null, Encapsulation, Metadata, TestContext.Current.CancellationToken);

        await subject.SetAliasByIdAsync("key-1", null, TestContext.Current.CancellationToken);
        await subject.SetAliasByIdAsync("key-2", "shared", TestContext.Current.CancellationToken);

        var byAlias = await subject.FindByAliasAsync("shared", TestContext.Current.CancellationToken);
        byAlias!.Id.Should().Be("key-2");
    }

    [Fact]
    public async Task SetAliasById_IsIdempotentWhenTheAliasIsAlreadyOnTheKey()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1");

        await subject.CreateAsync("primary", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        await subject.SetAliasByIdAsync("key-1", "primary", TestContext.Current.CancellationToken);

        var key = await subject.FindByIdAsync("key-1", TestContext.Current.CancellationToken);
        key!.Alias.Should().Be("primary");

        var byAlias = await subject.FindByAliasAsync("primary", TestContext.Current.CancellationToken);
        byAlias!.Id.Should().Be("key-1");
    }

    [Fact]
    public async Task Create_ThrowsWhenAnotherKeyAlreadyHoldsTheAlias()
    {
        var subject = CreateSubject();
        SetGeneratedIds("key-1", "key-2");

        await subject.CreateAsync("shared", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        var act = () => subject.CreateAsync("shared", Encapsulation, Metadata, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<EncapsulatedAliasInUseException>().WithMessage("*shared*");
    }
}
