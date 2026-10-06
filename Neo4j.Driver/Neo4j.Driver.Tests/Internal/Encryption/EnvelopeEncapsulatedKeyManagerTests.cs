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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EnvelopeEncapsulatedKeyManagerTests
{
    private readonly Mock<IKeyEncapsulationService> _kes = new();
    private readonly Mock<IEncapsulatedKeyRecordRepository> _repository = new();
    private readonly Mock<IEncryptionErrorPolicy> _errorPolicy = new();

    private EnvelopeEncapsulatedKeyManager CreateSubject()
    {
        return new EnvelopeEncapsulatedKeyManager(_kes.Object, _repository.Object, _errorPolicy.Object);
    }

    private static readonly Dictionary<string, string> ResultMetadata = new() { ["iv"] = "abc" };
    private static readonly KeyEncapsulationResult EncapsulationResult = new([0xAA], ResultMetadata, [0xBB]);

    private record SuppliedOptions : IKeyEncapsulationOptions
    {
        public IReadOnlyDictionary<string, string> ToMap()
        {
            return new Dictionary<string, string> { ["region"] = "eu-west-1" };
        }
    }

    [Fact]
    public async Task CreateAsync_WithAnAlias_EncapsulatesWithEmptyOptionsAndStoresUnderThatAlias()
    {
        var token = TestContext.Current.CancellationToken;
        var stored = new EncapsulatedKeyRecord("key-1", "alias-1", [0xAA], ResultMetadata);

        _kes.Setup(k => k.EncapsulateAsync(
                It.Is<IKeyEncapsulationOptions>(o => o.ToMap().Count == 0),
                token))
            .ReturnsAsync(EncapsulationResult);

        _repository.Setup(r => r.CreateAsync("alias-1", EncapsulationResult.Encapsulation, ResultMetadata, token))
            .ReturnsAsync(stored);

        var result = await CreateSubject().CreateAsync("alias-1", cancellationToken: token);

        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task CreateAsync_WithNoAlias_StoresTheKeyUnaliased()
    {
        var token = TestContext.Current.CancellationToken;
        var stored = new EncapsulatedKeyRecord("key-1", null, [0xAA], ResultMetadata);

        _kes.Setup(k => k.EncapsulateAsync(It.IsAny<IKeyEncapsulationOptions>(), token))
            .ReturnsAsync(EncapsulationResult);

        _repository.Setup(r => r.CreateAsync(null, EncapsulationResult.Encapsulation, ResultMetadata, token))
            .ReturnsAsync(stored);

        var result = await CreateSubject().CreateAsync(cancellationToken: token);

        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task CreateAsync_WithEncapsulationOptions_PassesThemToTheEncapsulationService()
    {
        var token = TestContext.Current.CancellationToken;
        var options = new SuppliedOptions();
        var stored = new EncapsulatedKeyRecord("key-1", "alias-1", [0xAA], ResultMetadata);

        _kes.Setup(k => k.EncapsulateAsync(options, token)).ReturnsAsync(EncapsulationResult);
        _repository.Setup(r => r.CreateAsync("alias-1", EncapsulationResult.Encapsulation, ResultMetadata, token))
            .ReturnsAsync(stored);

        var result = await CreateSubject().CreateAsync("alias-1", options, token);

        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task FindByAliasAsync_ReturnsTheKeyTheRepositoryHolds()
    {
        var token = TestContext.Current.CancellationToken;
        var stored = new EncapsulatedKeyRecord("key-1", "alias-1", [0xAA], ResultMetadata);

        _repository.Setup(r => r.FindByAliasAsync("alias-1", token)).ReturnsAsync(stored);

        var result = await CreateSubject().FindByAliasAsync("alias-1", token);

        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task FindByAliasAsync_WhenTheAliasIsUnknown_ReturnsNull()
    {
        var token = TestContext.Current.CancellationToken;

        _repository.Setup(r => r.FindByAliasAsync("missing", token))
            .ReturnsAsync((EncapsulatedKeyRecord?)null);

        var result = await CreateSubject().FindByAliasAsync("missing", token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAliasByIdAsync_SetsTheAliasOnTheKey()
    {
        var token = TestContext.Current.CancellationToken;

        await CreateSubject().SetAliasByIdAsync("key-1", "alias-1", token);

        _repository.Verify(r => r.SetAliasByIdAsync("key-1", "alias-1", token), Times.Once);
    }

    [Fact]
    public async Task DeleteAliasByIdAsync_ClearsTheAliasOnTheKey()
    {
        var token = TestContext.Current.CancellationToken;

        await CreateSubject().DeleteAliasByIdAsync("key-1", token);

        _repository.Verify(r => r.SetAliasByIdAsync("key-1", null, token), Times.Once);
    }

    [Fact]
    public async Task DeleteByIdAsync_DeletesTheKey()
    {
        var token = TestContext.Current.CancellationToken;

        await CreateSubject().DeleteByIdAsync("key-1", token);

        _repository.Verify(r => r.DeleteByIdAsync("key-1", token), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NonDriverException_DelegatesToTheErrorPolicy()
    {
        var token = TestContext.Current.CancellationToken;
        var cause = new InvalidOperationException("kes blew up");
        var wrapped = new PropertyEncryptionException("wrapped", cause);

        _kes.Setup(k => k.EncapsulateAsync(It.IsAny<IKeyEncapsulationOptions>(), token)).ThrowsAsync(cause);
        _errorPolicy.Setup(p => p.Throw("key creation", cause, It.IsAny<CancellationToken>())).Throws(wrapped);

        var act = () => CreateSubject().CreateAsync("alias-1", cancellationToken: token);

        var thrown = await act.Should().ThrowAsync<PropertyEncryptionException>();
        thrown.Which.Should().BeSameAs(wrapped);
        _errorPolicy.Verify(p => p.Throw("key creation", cause, It.IsAny<CancellationToken>()), Times.Once);
    }

    public static TheoryData<string, Func<IEncapsulatedKeyManager, CancellationToken, Task>> RepositoryOperations =>
        new()
        {
            { "key lookup", (manager, token) => manager.FindByAliasAsync("alias-1", token) },
            { "alias update", (manager, token) => manager.SetAliasByIdAsync("key-1", "alias-1", token) },
            { "alias update", (manager, token) => manager.DeleteAliasByIdAsync("key-1", token) },
            { "key deletion", (manager, token) => manager.DeleteByIdAsync("key-1", token) }
        };

    [Theory]
    [MemberData(nameof(RepositoryOperations))]
    public async Task RepositoryOperation_NonDriverException_DelegatesToTheErrorPolicy(
        string operationName,
        Func<IEncapsulatedKeyManager, CancellationToken, Task> operation)
    {
        var token = TestContext.Current.CancellationToken;
        var cause = new InvalidOperationException("repository blew up");
        var wrapped = new PropertyEncryptionException("wrapped", cause);

        _repository.Setup(r => r.FindByAliasAsync(It.IsAny<string>(), token)).ThrowsAsync(cause);
        _repository.Setup(r => r.SetAliasByIdAsync(It.IsAny<string>(), It.IsAny<string?>(), token)).ThrowsAsync(cause);
        _repository.Setup(r => r.DeleteByIdAsync(It.IsAny<string>(), token)).ThrowsAsync(cause);
        _errorPolicy.Setup(p => p.Throw(operationName, cause, It.IsAny<CancellationToken>())).Throws(wrapped);

        var act = () => operation(CreateSubject(), token);

        var thrown = await act.Should().ThrowAsync<PropertyEncryptionException>();
        thrown.Which.Should().BeSameAs(wrapped);
        _errorPolicy.Verify(p => p.Throw(operationName, cause, It.IsAny<CancellationToken>()), Times.Once);
    }
}
