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
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ImportEncapsulatedKeyHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ImportEncapsulatedKeyHandler>();
    private static readonly HexBytes Encapsulation = new([0xDE, 0xAD, 0xBE, 0xEF]);
    private static readonly Dictionary<string, string> Metadata = new() { ["iv"] = "abc123" };

    [Fact]
    public async Task Imports_using_the_named_profiles_repository()
    {
        var driver = Mock.Of<IDriver>();
        var repositoryMock = new Mock<ITestkitEncapsulatedKeyRepository>();
        _autoMocker.GetMock<IDriverEncryptionObjectStore>()
            .Setup(s => s.GetRepository(driver, "p1"))
            .Returns(repositoryMock.Object);

        repositoryMock.Setup(r => r.Import("key-1", "k1", Encapsulation.Value, Metadata))
            .Returns(new EncapsulatedKey("key-1", "k1", [], new Dictionary<string, string>()));

        var handler = _autoMocker.CreateInstance<ImportEncapsulatedKeyHandler>();
        var request = new ImportEncapsulatedKeyRequest
        {
            Driver = driver,
            KeyId = "key-1",
            Alias = "k1",
            Encapsulation = Encapsulation,
            Metadata = Metadata,
            ProfileName = "p1"
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncapsulatedKeyResponse("key-1", "k1")), Times.Once);
    }

    [Fact]
    public async Task Imports_using_the_sole_repository_when_no_profile_name_is_given()
    {
        var driver = Mock.Of<IDriver>();
        var repositoryMock = new Mock<ITestkitEncapsulatedKeyRepository>();
        _autoMocker.GetMock<IDriverEncryptionObjectStore>()
            .Setup(s => s.GetRepository(driver))
            .Returns(repositoryMock.Object);

        repositoryMock.Setup(r => r.Import("key-1", "k1", Encapsulation.Value, Metadata))
            .Returns(new EncapsulatedKey("key-1", "k1", [], new Dictionary<string, string>()));

        var handler = _autoMocker.CreateInstance<ImportEncapsulatedKeyHandler>();
        var request = new ImportEncapsulatedKeyRequest
        {
            Driver = driver,
            KeyId = "key-1",
            Alias = "k1",
            Encapsulation = Encapsulation,
            Metadata = Metadata
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new EncapsulatedKeyResponse("key-1", "k1")), Times.Once);
    }
}
