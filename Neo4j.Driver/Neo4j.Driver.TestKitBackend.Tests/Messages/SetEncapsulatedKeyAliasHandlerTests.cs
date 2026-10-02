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
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class SetEncapsulatedKeyAliasHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<SetEncapsulatedKeyAliasHandler>();

    [Fact]
    public async Task Sets_the_alias_using_the_sole_profiles_key_manager_when_no_profile_name_is_given()
    {
        var driverMock = new Mock<IDriver>();
        var propertyEncryptionMock = driverMock.WithPropertyEncryption();
        var keyManagerMock = new Mock<IEncapsulatedKeyManager>();
        propertyEncryptionMock.Setup(p => p.KeyManager()).Returns(keyManagerMock.Object);

        var handler = _autoMocker.CreateInstance<SetEncapsulatedKeyAliasHandler>();
        var request = new SetEncapsulatedKeyAliasRequest { Driver = driverMock.Object, Id = "key-1", Alias = "k2" };

        await handler.ProcessAsync(request);

        keyManagerMock.Verify(m => m.SetAliasByIdAsync("key-1", "k2", It.IsAny<CancellationToken>()), Times.Once);
        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncapsulatedKeyResponse("key-1", "k2")), Times.Once);
    }

    [Fact]
    public async Task Sets_the_alias_using_the_named_profiles_key_manager()
    {
        var driverMock = new Mock<IDriver>();
        var propertyEncryptionMock = driverMock.WithPropertyEncryption();
        var keyManagerMock = new Mock<IEncapsulatedKeyManager>();
        propertyEncryptionMock.Setup(p => p.KeyManager("p1")).Returns(keyManagerMock.Object);

        var handler = _autoMocker.CreateInstance<SetEncapsulatedKeyAliasHandler>();
        var request = new SetEncapsulatedKeyAliasRequest
        {
            Driver = driverMock.Object,
            Id = "key-1",
            Alias = "k2",
            ProfileName = "p1"
        };

        await handler.ProcessAsync(request);

        keyManagerMock.Verify(m => m.SetAliasByIdAsync("key-1", "k2", It.IsAny<CancellationToken>()), Times.Once);
        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncapsulatedKeyResponse("key-1", "k2")), Times.Once);
    }

    [Fact]
    public async Task Clears_the_alias_when_none_is_given()
    {
        var driverMock = new Mock<IDriver>();
        var propertyEncryptionMock = driverMock.WithPropertyEncryption();
        var keyManagerMock = new Mock<IEncapsulatedKeyManager>();
        propertyEncryptionMock.Setup(p => p.KeyManager()).Returns(keyManagerMock.Object);

        var handler = _autoMocker.CreateInstance<SetEncapsulatedKeyAliasHandler>();
        var request = new SetEncapsulatedKeyAliasRequest { Driver = driverMock.Object, Id = "key-1" };

        await handler.ProcessAsync(request);

        keyManagerMock.Verify(m => m.SetAliasByIdAsync("key-1", null, It.IsAny<CancellationToken>()), Times.Once);
        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncapsulatedKeyResponse("key-1", null)), Times.Once);
    }
}
