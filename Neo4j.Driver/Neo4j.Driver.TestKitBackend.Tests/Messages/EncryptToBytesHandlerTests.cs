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

using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class EncryptToBytesHandlerTests
{
    // Moq can't build a Setup/Verify expression over a ReadOnlySpan<byte> parameter, so this
    // records SetNextIv calls instead of mocking them.
    private class RecordingIvProvider : IFixedIvProvider
    {
        public byte[]? SetIv { get; private set; }
        public bool Consumed { get; private set; }

        public void SetNextIv(ReadOnlySpan<byte> iv)
        {
            SetIv = iv.ToArray();
        }

        public void EnsureConsumed()
        {
            Consumed = true;
        }

        public byte[] GetIv()
        {
            throw new NotSupportedException();
        }
    }

    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<EncryptToBytesHandler>();
    private static readonly byte[] EncryptedBytes = [0xDE, 0xAD, 0xBE, 0xEF];

    private (Mock<IDriver> Driver, Mock<IEncryptRequestKeyStep> KeyStep, Mock<IEncryptRequestExecuteStep> ExecuteStep)
        DriverAcceptingValue(ICypherValue value, object nativeValue)
    {
        _autoMocker.GetMock<ICypherToNativeMapper>().Setup(m => m.Map(value)).Returns(nativeValue);

        var driverMock = new Mock<IDriver>();
        var internalDriverMock = driverMock.As<IInternalDriver>();
        var propertyEncryptionMock = new Mock<IPropertyEncryption>();
        var valueStepMock = new Mock<IEncryptRequestValueStep>();
        var keyStepMock = new Mock<IEncryptRequestKeyStep>();
        var executeStepMock = new Mock<IEncryptRequestExecuteStep>();

        internalDriverMock.Setup(d => d.PropertyEncryption()).Returns(propertyEncryptionMock.Object);
        propertyEncryptionMock.Setup(p => p.EncryptRequest()).Returns(valueStepMock.Object);
        valueStepMock.Setup(v => v.FromValue(nativeValue)).Returns(keyStepMock.Object);

        return (driverMock, keyStepMock, executeStepMock);
    }

    [Fact]
    public async Task Encrypts_by_key_alias_and_responds_with_the_hex_encoded_bytes()
    {
        var value = new CypherString("hello world");
        var (driverMock, keyStepMock, executeStepMock) = DriverAcceptingValue(value, "hello world");
        keyStepMock.Setup(k => k.UsingKeyAlias("k1")).Returns(executeStepMock.Object);
        executeStepMock.Setup(e => e.EncryptToBytesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(EncryptedBytes);

        var handler = _autoMocker.CreateInstance<EncryptToBytesHandler>();
        var request = new EncryptToBytesRequest { Driver = driverMock.Object, Value = value, KeyAlias = "k1" };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncryptedValueResponse(new HexBytes(EncryptedBytes))), Times.Once);
    }

    [Fact]
    public async Task Encrypts_by_key_id()
    {
        var value = new CypherString("hello world");
        var (driverMock, keyStepMock, executeStepMock) = DriverAcceptingValue(value, "hello world");
        keyStepMock.Setup(k => k.UsingKeyId("key-1")).Returns(executeStepMock.Object);
        executeStepMock.Setup(e => e.EncryptToBytesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(EncryptedBytes);

        var handler = _autoMocker.CreateInstance<EncryptToBytesHandler>();
        var request = new EncryptToBytesRequest { Driver = driverMock.Object, Value = value, KeyId = "key-1" };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncryptedValueResponse(new HexBytes(EncryptedBytes))), Times.Once);
    }

    [Fact]
    public async Task Binds_the_mapped_aad_before_selecting_the_key()
    {
        var value = new CypherString("hello world");
        var aad = new CypherString("row-42");
        var (driverMock, keyStepMock, executeStepMock) = DriverAcceptingValue(value, "hello world");
        _autoMocker.GetMock<ICypherToNativeMapper>().Setup(m => m.Map(aad)).Returns("row-42");
        keyStepMock.Setup(k => k.WithAad("row-42")).Returns(keyStepMock.Object);
        keyStepMock.Setup(k => k.UsingKeyAlias("k1")).Returns(executeStepMock.Object);
        executeStepMock.Setup(e => e.EncryptToBytesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(EncryptedBytes);

        var handler = _autoMocker.CreateInstance<EncryptToBytesHandler>();
        var request = new EncryptToBytesRequest
        {
            Driver = driverMock.Object,
            Value = value,
            Aad = aad,
            KeyAlias = "k1"
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncryptedValueResponse(new HexBytes(EncryptedBytes))), Times.Once);
    }

    [Fact]
    public async Task Selects_the_named_profile_before_selecting_the_key()
    {
        var value = new CypherString("hello world");
        var (driverMock, keyStepMock, executeStepMock) = DriverAcceptingValue(value, "hello world");
        keyStepMock.Setup(k => k.UsingProfile("p1")).Returns(keyStepMock.Object);
        keyStepMock.Setup(k => k.UsingKeyAlias("k1")).Returns(executeStepMock.Object);
        executeStepMock.Setup(e => e.EncryptToBytesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(EncryptedBytes);

        var handler = _autoMocker.CreateInstance<EncryptToBytesHandler>();
        var request = new EncryptToBytesRequest
        {
            Driver = driverMock.Object,
            Value = value,
            ProfileName = "p1",
            KeyAlias = "k1"
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncryptedValueResponse(new HexBytes(EncryptedBytes))), Times.Once);
    }

    [Fact]
    public async Task Sets_and_consumes_a_fixed_iv_around_the_encrypt_call()
    {
        var value = new CypherString("hello world");
        var (driverMock, keyStepMock, executeStepMock) = DriverAcceptingValue(value, "hello world");
        keyStepMock.Setup(k => k.UsingKeyAlias("k1")).Returns(executeStepMock.Object);
        executeStepMock.Setup(e => e.EncryptToBytesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(EncryptedBytes);

        var ivProvider = new RecordingIvProvider();
        _autoMocker.GetMock<IDriverEncryptionObjectStore>()
            .Setup(s => s.GetIvProvider(driverMock.Object))
            .Returns(ivProvider);

        var iv = Enumerable.Range(0, 12).Select(b => (byte)b).ToArray();
        var handler = _autoMocker.CreateInstance<EncryptToBytesHandler>();
        var request = new EncryptToBytesRequest
        {
            Driver = driverMock.Object,
            Value = value,
            KeyAlias = "k1",
            Iv = new HexBytes(iv)
        };

        await handler.ProcessAsync(request);

        ivProvider.SetIv.Should().Equal(iv);
        ivProvider.Consumed.Should().BeTrue();
    }

    [Fact]
    public async Task Raises_a_frontend_error_when_neither_key_alias_nor_key_id_is_set()
    {
        var handler = _autoMocker.CreateInstance<EncryptToBytesHandler>();
        var request = new EncryptToBytesRequest { Driver = new Mock<IDriver>().Object, Value = new CypherString("v") };

        var act = () => handler.ProcessAsync(request);

        await act.Should().ThrowAsync<FrontendException>();
    }

    [Fact]
    public async Task Raises_a_frontend_error_when_both_key_alias_and_key_id_are_set()
    {
        var handler = _autoMocker.CreateInstance<EncryptToBytesHandler>();
        var request = new EncryptToBytesRequest
        {
            Driver = new Mock<IDriver>().Object,
            Value = new CypherString("v"),
            KeyAlias = "k1",
            KeyId = "key-1"
        };

        var act = () => handler.ProcessAsync(request);

        await act.Should().ThrowAsync<FrontendException>();
    }
}
