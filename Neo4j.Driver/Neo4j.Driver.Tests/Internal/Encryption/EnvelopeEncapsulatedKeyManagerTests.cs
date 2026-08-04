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
    private readonly Mock<IEncapsulatedKeyRepository> _repository = new();
    private readonly Mock<IEncryptionErrorPolicy> _errorPolicy = new();

    private EnvelopeEncapsulatedKeyManager CreateSubject()
    {
        return new EnvelopeEncapsulatedKeyManager(_kes.Object, _repository.Object, _errorPolicy.Object);
    }

    [Fact]
    public async Task CreateAsync_EncapsulatesWithEmptyOptionsAndSavesTheResultUnderTheAlias()
    {
        var token = TestContext.Current.CancellationToken;
        var resultOptions = new MapKeyEncapsulationOptions(new Dictionary<string, string> { ["iv"] = "abc" });
        var encapsulationResult = new EncapsulationResult([0xAA], resultOptions, [0xBB]);
        var stored = new EncapsulatedKey("key-1", "alias-1", [0xAA], resultOptions.ToMap());

        _kes.Setup(k => k.EncapsulateAsync(
                It.Is<IKeyEncapsulationOptions>(o => o.ToMap().Count == 0),
                token))
            .ReturnsAsync(encapsulationResult);

        _repository.Setup(r => r.SaveAsync("alias-1", encapsulationResult.Encapsulation, resultOptions.ToMap(), token))
            .ReturnsAsync(stored);

        var result = await CreateSubject().CreateAsync("alias-1", token);

        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task CreateAsync_NonDriverException_DelegatesToTheErrorPolicy()
    {
        var token = TestContext.Current.CancellationToken;
        var cause = new InvalidOperationException("kes blew up");
        var wrapped = new PropertyEncryptionException("wrapped", cause);

        _kes.Setup(k => k.EncapsulateAsync(It.IsAny<IKeyEncapsulationOptions>(), token)).ThrowsAsync(cause);
        _errorPolicy.Setup(p => p.Throw("key creation", cause)).Throws(wrapped);

        var act = () => CreateSubject().CreateAsync("alias-1", token);

        var thrown = await act.Should().ThrowAsync<PropertyEncryptionException>();
        thrown.Which.Should().BeSameAs(wrapped);
        _errorPolicy.Verify(p => p.Throw("key creation", cause), Times.Once);
    }
}
