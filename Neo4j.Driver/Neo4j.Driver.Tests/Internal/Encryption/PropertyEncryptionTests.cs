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
using Neo4j.Driver.Tests.Internal.Core;
using Moq.AutoMock;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class PropertyEncryptionTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<PropertyEncryption>();
    private readonly Mock<IEncryptionProfileRegistry> _registry;
    private readonly Mock<IEncapsulatedKeyManagerFactory> _keyManagerFactory;

    public PropertyEncryptionTests()
    {
        _registry = _autoMocker.GetMock<IEncryptionProfileRegistry>();
        _keyManagerFactory = _autoMocker.GetMock<IEncapsulatedKeyManagerFactory>();
    }

    private PropertyEncryption CreateSubject()
    {
        return _autoMocker.CreateInstance<PropertyEncryption>();
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
