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

using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class CreateEncapsulatedKeyHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<CreateEncapsulatedKeyHandler>();

    private static Mock<IDriver> DriverWithPropertyEncryption(out Mock<IPropertyEncryption> propertyEncryptionMock)
    {
        var driverMock = new Mock<IDriver>();
        var internalDriverMock = driverMock.As<IInternalDriver>();
        propertyEncryptionMock = new Mock<IPropertyEncryption>();
        internalDriverMock.Setup(d => d.PropertyEncryption()).Returns(propertyEncryptionMock.Object);
        return driverMock;
    }

    [Fact]
    public async Task Creates_a_key_using_the_sole_profiles_key_manager_when_no_profile_name_is_given()
    {
        var driverMock = DriverWithPropertyEncryption(out var propertyEncryptionMock);
        var keyManagerMock = new Mock<IEncapsulatedKeyManager>();
        propertyEncryptionMock.Setup(p => p.KeyManager()).Returns(keyManagerMock.Object);
        keyManagerMock.Setup(m => m.CreateAsync("k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EncapsulatedKey("key-1", "k1", [], new Dictionary<string, string>()));

        var handler = _autoMocker.CreateInstance<CreateEncapsulatedKeyHandler>();
        var request = new CreateEncapsulatedKeyRequest { Driver = driverMock.Object, Alias = "k1" };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncapsulatedKeyResponse("key-1", "k1")), Times.Once);
    }

    [Fact]
    public async Task Creates_a_key_using_the_named_profiles_key_manager()
    {
        var driverMock = DriverWithPropertyEncryption(out var propertyEncryptionMock);
        var keyManagerMock = new Mock<IEncapsulatedKeyManager>();
        propertyEncryptionMock.Setup(p => p.KeyManager("p1")).Returns(keyManagerMock.Object);
        keyManagerMock.Setup(m => m.CreateAsync("k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EncapsulatedKey("key-1", "k1", [], new Dictionary<string, string>()));

        var handler = _autoMocker.CreateInstance<CreateEncapsulatedKeyHandler>();
        var request = new CreateEncapsulatedKeyRequest
        {
            Driver = driverMock.Object,
            Alias = "k1",
            ProfileName = "p1"
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncapsulatedKeyResponse("key-1", "k1")), Times.Once);
    }
}
