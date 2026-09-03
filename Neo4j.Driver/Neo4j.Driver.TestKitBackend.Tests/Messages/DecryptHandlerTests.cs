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
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class DecryptHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<DecryptHandler>();
    private static readonly HexBytes EncryptedValue = new([0xDE, 0xAD, 0xBE, 0xEF]);

    private static (Mock<IDriver> Driver, Mock<IDecryptRequestAadStep> AadStep) DriverAcceptingValue()
    {
        var driverMock = new Mock<IDriver>();
        var propertyEncryptionMock = driverMock.WithPropertyEncryption();
        var valueStepMock = new Mock<IDecryptRequestValueStep>();
        var aadStepMock = new Mock<IDecryptRequestAadStep>();

        propertyEncryptionMock.Setup(p => p.DecryptRequest()).Returns(valueStepMock.Object);
        valueStepMock.Setup(v => v.FromValue(EncryptedValue.Value)).Returns(aadStepMock.Object);

        return (driverMock, aadStepMock);
    }

    [Fact]
    public async Task Decrypts_with_the_persisted_aad_and_responds_with_the_mapped_value()
    {
        var (driverMock, aadStepMock) = DriverAcceptingValue();
        var executeStepMock = new Mock<IDecryptRequestExecuteStep>();
        aadStepMock.Setup(a => a.WithPersistedAad()).Returns(executeStepMock.Object);
        executeStepMock.Setup(e => e.DecryptAsync(It.IsAny<CancellationToken>())).ReturnsAsync("hello world");
        _autoMocker.GetMock<INativeToCypherMapper>().Setup(m => m.Map("hello world")).Returns(new CypherString("hello world"));

        var handler = _autoMocker.CreateInstance<DecryptHandler>();
        var request = new DecryptRequest { Driver = driverMock.Object, Value = EncryptedValue, UsePersistedAad = true };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new DecryptedValueResponse(new CypherString("hello world"))), Times.Once);
    }

    [Fact]
    public async Task Decrypts_with_an_explicit_mapped_aad()
    {
        var (driverMock, aadStepMock) = DriverAcceptingValue();
        var executeStepMock = new Mock<IDecryptRequestExecuteStep>();

        var aad = new CypherString("row-42");
        _autoMocker.GetMock<ICypherToNativeMapper>().Setup(m => m.Map(aad)).Returns("row-42");
        aadStepMock.Setup(a => a.WithAad("row-42")).Returns(executeStepMock.Object);
        executeStepMock.Setup(e => e.DecryptAsync(It.IsAny<CancellationToken>())).ReturnsAsync("aad-bound");
        _autoMocker.GetMock<INativeToCypherMapper>().Setup(m => m.Map("aad-bound")).Returns(new CypherString("aad-bound"));

        var handler = _autoMocker.CreateInstance<DecryptHandler>();
        var request = new DecryptRequest { Driver = driverMock.Object, Value = EncryptedValue, Aad = aad };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new DecryptedValueResponse(new CypherString("aad-bound"))), Times.Once);
    }

    [Fact]
    public async Task Raises_a_frontend_error_when_neither_aad_nor_use_persisted_aad_is_set()
    {
        var handler = _autoMocker.CreateInstance<DecryptHandler>();
        var request = new DecryptRequest { Driver = new Mock<IDriver>().Object, Value = EncryptedValue };

        var act = () => handler.ProcessAsync(request);

        await act.Should().ThrowAsync<FrontendException>();
    }

    [Fact]
    public async Task Raises_a_frontend_error_when_both_aad_and_use_persisted_aad_are_set()
    {
        var handler = _autoMocker.CreateInstance<DecryptHandler>();
        var request = new DecryptRequest
        {
            Driver = new Mock<IDriver>().Object,
            Value = EncryptedValue,
            Aad = new CypherString("row-42"),
            UsePersistedAad = true
        };

        var act = () => handler.ProcessAsync(request);

        await act.Should().ThrowAsync<FrontendException>();
    }
}
