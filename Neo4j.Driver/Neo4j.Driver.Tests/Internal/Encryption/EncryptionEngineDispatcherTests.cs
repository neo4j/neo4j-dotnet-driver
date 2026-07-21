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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EncryptionEngineDispatcherTests
{
    private static readonly IInternalEncryptionProfile Profile =
        Mock.Of<IInternalEncryptionProfile>(p => p.Name == "profile-a");
    private static readonly KeyReference KeyRef = new("key-1", KeyReferenceType.Id);

    [Fact]
    public async Task DispatchEncryptAsync_ReturnsResultFromTheAcceptingEngine()
    {
        var expected = new byte[] { 1, 2, 3 };
        Task<byte[]>? encryptionTask = Task.FromResult(expected);

        var engine = new Mock<IEncryptionEngine>();
        engine.Setup(e => e.TryStartEncrypt(
                Profile,
                "value",
                KeyRef,
                null,
                It.IsAny<CancellationToken>(),
                out encryptionTask))
            .Returns(true);

        var dispatcher = new EncryptionEngineDispatcher([engine.Object]);

        var result = await dispatcher.DispatchEncryptAsync(Profile, "value", KeyRef, null, CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DispatchEncryptAsync_TriesEnginesInOrderUntilOneAccepts()
    {
        var expected = new byte[] { 9 };
        Task<byte[]>? noTask = null;
        Task<byte[]>? yesTask = Task.FromResult(expected);

        var rejecting = new Mock<IEncryptionEngine>();
        rejecting.Setup(e => e.TryStartEncrypt(
                Profile,
                "value",
                KeyRef,
                null,
                It.IsAny<CancellationToken>(),
                out noTask))
            .Returns(false);

        var accepting = new Mock<IEncryptionEngine>();
        accepting.Setup(e => e.TryStartEncrypt(
                Profile,
                "value",
                KeyRef,
                null,
                It.IsAny<CancellationToken>(),
                out yesTask))
            .Returns(true);

        var dispatcher = new EncryptionEngineDispatcher([rejecting.Object, accepting.Object]);

        var result = await dispatcher.DispatchEncryptAsync(Profile, "value", KeyRef, null, CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DispatchEncryptAsync_ThrowsWhenNoEngineAccepts()
    {
        Task<byte[]>? noTask = null;

        var engine = new Mock<IEncryptionEngine>();
        engine.Setup(e => e.TryStartEncrypt(
                Profile,
                "value",
                KeyRef,
                null,
                It.IsAny<CancellationToken>(),
                out noTask))
            .Returns(false);

        var dispatcher = new EncryptionEngineDispatcher([engine.Object]);

        var act = () => dispatcher.DispatchEncryptAsync(Profile, "value", KeyRef, null, CancellationToken.None);

        await act.Should().ThrowAsync<EncryptionEngineNotFoundException>();
    }

    [Fact]
    public async Task DispatchDecryptAsync_ReturnsResultFromTheAcceptingEngine()
    {
        var encrypted = new byte[] { 4, 5, 6 };
        object expected = "decrypted-value";
        Task<object>? decryptionTask = Task.FromResult(expected);

        var engine = new Mock<IEncryptionEngine>();
        engine.Setup(e => e.TryStartDecrypt(
                Profile,
                encrypted,
                null,
                It.IsAny<CancellationToken>(),
                out decryptionTask))
            .Returns(true);

        var dispatcher = new EncryptionEngineDispatcher([engine.Object]);

        var result = await dispatcher.DispatchDecryptAsync(Profile, encrypted, null, CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DispatchDecryptAsync_ThrowsWhenNoEngineAccepts()
    {
        var encrypted = new byte[] { 4, 5, 6 };
        Task<object>? noTask = null;

        var engine = new Mock<IEncryptionEngine>();
        engine.Setup(e => e.TryStartDecrypt(
                Profile,
                encrypted,
                null,
                It.IsAny<CancellationToken>(),
                out noTask))
            .Returns(false);

        var dispatcher = new EncryptionEngineDispatcher([engine.Object]);

        var act = () => dispatcher.DispatchDecryptAsync(Profile, encrypted, null, CancellationToken.None);

        await act.Should().ThrowAsync<EncryptionEngineNotFoundException>();
    }
}
