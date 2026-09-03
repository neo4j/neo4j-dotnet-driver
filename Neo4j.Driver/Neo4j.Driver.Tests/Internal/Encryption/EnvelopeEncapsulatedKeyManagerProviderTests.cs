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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EnvelopeEncapsulatedKeyManagerProviderTests
{
    private static EnvelopeEncapsulatedKeyManagerProvider CreateSubject()
    {
        return new EnvelopeEncapsulatedKeyManagerProvider(new EncryptionErrorPolicy());
    }

    [Fact]
    public async Task TryCreateKeyManager_WithEnvelopeProfile_ReturnsManagerCarryingTheProfilesKesAndRepository()
    {
        var kes = new Mock<IKeyEncapsulationService>();
        var repository = new Mock<IEncapsulatedKeyRepository>();
        var encapsulationResult = new EncapsulationResult(
            [0xAA],
            new MapKeyEncapsulationOptions(new Dictionary<string, string> { ["iv"] = "abc" }),
            [0xBB]);
        var stored = new EncapsulatedKey("key-1", "alias-1", [0xAA], new Dictionary<string, string> { ["iv"] = "abc" });

        kes.Setup(k => k.EncapsulateAsync(It.IsAny<IKeyEncapsulationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(encapsulationResult);
        repository.Setup(r => r.SaveAsync(
                "alias-1",
                encapsulationResult.Encapsulation,
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var profile = Mock.Of<IEnvelopeEncryptionProfile>(
            p => p.KeyEncapsulationService == kes.Object && p.KeyRepository == repository.Object);

        var started = CreateSubject().TryCreateKeyManager(profile, out var manager);

        started.Should().BeTrue();
        var result = await manager!.CreateAsync("alias-1", TestContext.Current.CancellationToken);

        result.Should().BeSameAs(stored);
    }

    [Fact]
    public void TryCreateKeyManager_WithNonEnvelopeProfile_ReturnsFalse()
    {
        var result = CreateSubject().TryCreateKeyManager(Mock.Of<IInternalEncryptionProfile>(), out var manager);

        result.Should().BeFalse();
        manager.Should().BeNull();
    }
}
