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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Tests.Internal.Core;
using Moq.AutoMock;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EncryptionRequestRunnerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<EncryptionRequestRunner>();
    private readonly Mock<IEncryptionProfileRegistry> _registry;
    private readonly Mock<IEncryptionEngineDispatcher> _dispatcher;
    private readonly Mock<IPlaintextCodec> _plaintextCodec;
    private readonly Mock<IEncryptedValueBytesCodec> _encryptedValueBytesCodec;

    public EncryptionRequestRunnerTests()
    {
        _registry = _autoMocker.GetMock<IEncryptionProfileRegistry>();
        _dispatcher = _autoMocker.GetMock<IEncryptionEngineDispatcher>();
        _plaintextCodec = _autoMocker.GetMock<IPlaintextCodec>();
        _encryptedValueBytesCodec = _autoMocker.GetMock<IEncryptedValueBytesCodec>();
    }

    private EncryptionRequestRunner CreateSubject()
    {
        return _autoMocker.CreateInstance<EncryptionRequestRunner>();
    }

    private static EncryptedStructure StructureForProfile(string profileName)
    {
        return new EncryptedStructure("ENVELOPE", 1, profileName, [], "STRING", 1, 0, new Dictionary<string, object>());
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
    public async Task DecryptAsync_WithExplicitAad_DecodesOnceAndDispatchesTheStructureWithSerializedAad()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0xEE };
        var profile = Mock.Of<IInternalEncryptionProfile>();
        var aad = new { context = "row-42" };
        var aadBytes = new byte[] { 0xAA };
        object expected = 5L;

        var structure = StructureForProfile("profile-a");
        _encryptedValueBytesCodec.Setup(c => c.Decode(encrypted)).Returns(structure);
        _registry.Setup(r => r.Get("profile-a")).Returns(profile);
        _plaintextCodec.Setup(c => c.Serialize(aad)).Returns(aadBytes);
        _dispatcher.Setup(d => d.DispatchDecryptAsync(profile, structure, aadBytes, token)).ReturnsAsync(expected);

        var request = new DecryptRequest(encrypted, aad);
        var result = await CreateSubject().DecryptAsync(request, token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DecryptAsync_WithoutExternalAad_DispatchesWithNullAad()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0xEE };
        var profile = Mock.Of<IInternalEncryptionProfile>();
        object expected = "decrypted-value";

        var structure = StructureForProfile("profile-a");
        _encryptedValueBytesCodec.Setup(c => c.Decode(encrypted)).Returns(structure);
        _registry.Setup(r => r.Get("profile-a")).Returns(profile);
        _dispatcher.Setup(d => d.DispatchDecryptAsync(profile, structure, null, token)).ReturnsAsync(expected);

        var request = new DecryptRequest(encrypted, null);
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

    [Fact]
    public async Task EncryptToBytesAsync_FaultsTheReturnedTask_WhenTheProfileCannotBeResolved()
    {
        var keyRef = new KeyReference("id-1", KeyReferenceType.Id);
        _registry.Setup(r => r.Get("missing")).Throws(new EncryptionProfileNotFoundException("missing"));

        var request = new EncryptRequest("hello", null, "missing", keyRef);
        var task = CreateSubject().EncryptToBytesAsync(request, TestContext.Current.CancellationToken);

        var awaiting = () => task;
        await awaiting.Should().ThrowAsync<EncryptionProfileNotFoundException>();
    }

    [Fact]
    public async Task DecryptAsync_FaultsTheReturnedTask_WhenTheProfileCannotBeResolved()
    {
        var encrypted = new byte[] { 0xEE };
        var structure = StructureForProfile("missing");
        _encryptedValueBytesCodec.Setup(c => c.Decode(encrypted)).Returns(structure);
        _registry.Setup(r => r.Get("missing")).Throws(new EncryptionProfileNotFoundException("missing"));

        var request = new DecryptRequest(encrypted, null);
        var task = CreateSubject().DecryptAsync(request, TestContext.Current.CancellationToken);

        var awaiting = () => task;
        await awaiting.Should().ThrowAsync<EncryptionProfileNotFoundException>();
    }

    [Fact]
    public async Task DecryptAsync_WhenTheValueCannotBeDecoded_DelegatesToTheErrorPolicy()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0x01 };
        var cause = new EndOfStreamException();
        var wrapped = new PropertyEncryptionException("wrapped", cause);

        _encryptedValueBytesCodec.Setup(c => c.Decode(encrypted)).Throws(cause);
        _autoMocker.GetMock<IEncryptionErrorPolicy>()
            .Setup(p => p.Throw("decryption", cause, token))
            .Throws(wrapped);

        var request = new DecryptRequest(encrypted, null);
        var act = () => CreateSubject().DecryptAsync(request, token);

        var thrown = await act.Should().ThrowAsync<PropertyEncryptionException>();
        thrown.Which.Should().BeSameAs(wrapped);
    }

    [Fact]
    public async Task EncryptToBytesAsync_WhenTheAadCannotBeSerialized_DelegatesToTheErrorPolicy()
    {
        var token = TestContext.Current.CancellationToken;
        var aad = new object();
        var cause = new InvalidOperationException("unserializable");
        var wrapped = new PropertyEncryptionException("wrapped", cause);

        _plaintextCodec.Setup(c => c.Serialize(aad)).Throws(cause);
        _autoMocker.GetMock<IEncryptionErrorPolicy>()
            .Setup(p => p.Throw("encryption", cause, token))
            .Throws(wrapped);

        var request = new EncryptRequest("hello", aad, null, new KeyReference("id-1", KeyReferenceType.Id));
        var act = () => CreateSubject().EncryptToBytesAsync(request, token);

        var thrown = await act.Should().ThrowAsync<PropertyEncryptionException>();
        thrown.Which.Should().BeSameAs(wrapped);
    }

    [Fact]
    public async Task EncryptToBytesAsync_WhenDispatchFails_DelegatesToTheErrorPolicy()
    {
        var token = TestContext.Current.CancellationToken;
        var keyRef = new KeyReference("id-1", KeyReferenceType.Id);
        var cause = new InvalidOperationException("kes blew up");
        var wrapped = new PropertyEncryptionException("wrapped", cause);

        _dispatcher
            .Setup(d => d.DispatchEncryptAsync(
                It.IsAny<IInternalEncryptionProfile>(),
                "hello",
                keyRef,
                null,
                null,
                token))
            .ThrowsAsync(cause);
        _autoMocker.GetMock<IEncryptionErrorPolicy>()
            .Setup(p => p.Throw("encryption", cause, token))
            .Throws(wrapped);

        var request = new EncryptRequest("hello", null, null, keyRef);
        var act = () => CreateSubject().EncryptToBytesAsync(request, token);

        var thrown = await act.Should().ThrowAsync<PropertyEncryptionException>();
        thrown.Which.Should().BeSameAs(wrapped);
    }

    [Fact]
    public async Task DecryptAsync_WhenDispatchFails_DelegatesToTheErrorPolicy()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0xEE };
        var cause = new InvalidOperationException("kes blew up");
        var wrapped = new PropertyEncryptionException("wrapped", cause);

        var structure = StructureForProfile("profile-a");
        _encryptedValueBytesCodec.Setup(c => c.Decode(encrypted)).Returns(structure);
        _dispatcher
            .Setup(d => d.DispatchDecryptAsync(It.IsAny<IInternalEncryptionProfile>(), structure, null, token))
            .ThrowsAsync(cause);
        _autoMocker.GetMock<IEncryptionErrorPolicy>()
            .Setup(p => p.Throw("decryption", cause, token))
            .Throws(wrapped);

        var request = new DecryptRequest(encrypted, null);
        var act = () => CreateSubject().DecryptAsync(request, token);

        var thrown = await act.Should().ThrowAsync<PropertyEncryptionException>();
        thrown.Which.Should().BeSameAs(wrapped);
    }
}
