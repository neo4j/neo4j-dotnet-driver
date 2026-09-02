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

public class EncryptionRequestRunnerTests
{
    private readonly Mock<IEncryptionProfileRegistry> _registry = new();
    private readonly Mock<IEncryptionEngineDispatcher> _dispatcher = new();
    private readonly Mock<IPlaintextCodec> _plaintextCodec = new();
    private readonly Mock<IEncryptedValueBytesCodec> _encryptedValueBytesCodec = new();

    private EncryptionRequestRunner CreateSubject()
    {
        return new EncryptionRequestRunner(
            _registry.Object,
            _dispatcher.Object,
            _plaintextCodec.Object,
            _encryptedValueBytesCodec.Object);
    }

    [Fact]
    public async Task EncryptToBytesAsync_ResolvesTheNamedProfileAndDispatchesWithSerializedAad()
    {
        var token = TestContext.Current.CancellationToken;
        var profile = Mock.Of<IInternalEncryptionProfile>();
        var aad = new { context = "row-42" };
        var aadBytes = new byte[] { 0xAA };
        var keyRef = new KeyReference("id-1", KeyReferenceType.Id);
        var expected = new byte[] { 1, 2, 3 };

        _registry.Setup(r => r.Get("profile-b")).Returns(profile);
        _plaintextCodec.Setup(c => c.Serialize(aad)).Returns(aadBytes);
        _dispatcher.Setup(d => d.DispatchEncryptAsync(profile, "hello", keyRef, aadBytes, null, token)).ReturnsAsync(expected);

        var request = new EncryptRequest("hello", aad, "profile-b", keyRef);
        var result = await CreateSubject().EncryptToBytesAsync(request, token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task EncryptToBytesAsync_WithoutAad_ResolvesTheDefaultProfileAndDispatchesWithNullAad()
    {
        var token = TestContext.Current.CancellationToken;
        var profile = Mock.Of<IInternalEncryptionProfile>();
        var keyRef = new KeyReference("id-1", KeyReferenceType.Id);
        var expected = new byte[] { 9 };

        _registry.Setup(r => r.Get(null)).Returns(profile);
        _dispatcher.Setup(d => d.DispatchEncryptAsync(profile, "hello", keyRef, null, null, token)).ReturnsAsync(expected);

        var request = new EncryptRequest("hello", null, null, keyRef);
        var result = await CreateSubject().EncryptToBytesAsync(request, token);

        result.Should().BeSameAs(expected);
        _plaintextCodec.Verify(c => c.Serialize(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task DecryptAsync_WithExplicitAad_PeeksTheProfileNameAndDispatchesWithSerializedAad()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0xEE };
        var profile = Mock.Of<IInternalEncryptionProfile>();
        var aad = new { context = "row-42" };
        var aadBytes = new byte[] { 0xAA };
        object expected = 5L;

        _encryptedValueBytesCodec.Setup(c => c.PeekProfileName(encrypted)).Returns("profile-a");
        _registry.Setup(r => r.Get("profile-a")).Returns(profile);
        _plaintextCodec.Setup(c => c.Serialize(aad)).Returns(aadBytes);
        _dispatcher.Setup(d => d.DispatchDecryptAsync(profile, encrypted, aadBytes, token)).ReturnsAsync(expected);

        var request = new DecryptRequest(encrypted, aad, UsePersistedAad: false);
        var result = await CreateSubject().DecryptAsync(request, token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DecryptAsync_WithPersistedAad_DispatchesWithNullAad()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0xEE };
        var profile = Mock.Of<IInternalEncryptionProfile>();
        object expected = "decrypted-value";

        _encryptedValueBytesCodec.Setup(c => c.PeekProfileName(encrypted)).Returns("profile-a");
        _registry.Setup(r => r.Get("profile-a")).Returns(profile);
        _dispatcher.Setup(d => d.DispatchDecryptAsync(profile, encrypted, null, token)).ReturnsAsync(expected);

        var request = new DecryptRequest(encrypted, null, UsePersistedAad: true);
        var result = await CreateSubject().DecryptAsync(request, token);

        result.Should().BeSameAs(expected);
        _plaintextCodec.Verify(c => c.Serialize(It.IsAny<object>()), Times.Never);
    }
    [Fact]
    public async Task EncryptToBytesAsync_ForwardsTheRequestIvToTheDispatcher()
    {
        var token = TestContext.Current.CancellationToken;
        var profile = Mock.Of<IInternalEncryptionProfile>();
        var iv = new byte[] { 0x70, 0x71 };
        var keyRef = new KeyReference("id-1", KeyReferenceType.Id);
        var expected = new byte[] { 4, 5 };

        _registry.Setup(r => r.Get("profile-b")).Returns(profile);
        _dispatcher.Setup(d => d.DispatchEncryptAsync(profile, "hello", keyRef, null, iv, token)).ReturnsAsync(expected);

        var request = new EncryptRequest("hello", null, "profile-b", keyRef, iv);
        var result = await CreateSubject().EncryptToBytesAsync(request, token);

        result.Should().BeSameAs(expected);
    }
}
