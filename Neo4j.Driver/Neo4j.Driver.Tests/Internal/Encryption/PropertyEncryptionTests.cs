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

using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class PropertyEncryptionTests
{
    private readonly Mock<IEncryptionRequestRunner> _runner = new();
    private readonly Mock<IEncryptionProfileRegistry> _registry = new();
    private readonly Mock<IEncapsulatedKeyManagerFactory> _keyManagerFactory = new();

    private PropertyEncryption CreateSubject()
    {
        return new PropertyEncryption(_runner.Object, _registry.Object, _keyManagerFactory.Object);
    }

    [Fact]
    public async Task EncryptRequest_ReturnsABuilderWiredToTheInjectedRunner()
    {
        var token = TestContext.Current.CancellationToken;
        var expected = new byte[] { 1 };
        _runner.Setup(r => r.EncryptToBytesAsync(
                new EncryptRequest("hello", null, null, new KeyReference("id-1", KeyReferenceType.Id)),
                token))
            .ReturnsAsync(expected);

        var result = await CreateSubject().EncryptRequest()
            .FromValue("hello")
            .UsingKeyId("id-1")
            .EncryptToBytesAsync(token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DecryptRequest_ReturnsABuilderWiredToTheInjectedRunner()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0xEE };
        object expected = "decrypted-value";
        _runner.Setup(r => r.DecryptAsync(new DecryptRequest(encrypted, null, true), token)).ReturnsAsync(expected);

        var result = await CreateSubject().DecryptRequest()
            .FromValue(encrypted)
            .WithPersistedAad()
            .DecryptAsync(token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void KeyManager_ResolvesTheDefaultProfileAndReturnsItsKeyManager()
    {
        var profile = Mock.Of<IInternalEncryptionProfile>();
        var expected = Mock.Of<IEncapsulatedKeyManager>();
        _registry.Setup(r => r.Get(null)).Returns(profile);
        _keyManagerFactory.Setup(f => f.CreateKeyManager(profile)).Returns(expected);

        var result = CreateSubject().KeyManager();

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void KeyManager_WithProfileName_ResolvesTheNamedProfileAndReturnsItsKeyManager()
    {
        var profile = Mock.Of<IInternalEncryptionProfile>();
        var expected = Mock.Of<IEncapsulatedKeyManager>();
        _registry.Setup(r => r.Get("profile-b")).Returns(profile);
        _keyManagerFactory.Setup(f => f.CreateKeyManager(profile)).Returns(expected);

        var result = CreateSubject().KeyManager("profile-b");

        result.Should().BeSameAs(expected);
    }
}
