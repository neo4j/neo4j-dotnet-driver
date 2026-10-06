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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.Tests.Internal.Core;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class DecryptRequestBuilderTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<DecryptRequestBuilder>();
    private readonly Mock<IEncryptionRequestRunner> _runner;
    private readonly Mock<IPropertyTypeInspector> _propertyTypeInspector;

    public DecryptRequestBuilderTests()
    {
        _runner = _autoMocker.GetMock<IEncryptionRequestRunner>();
        _propertyTypeInspector = _autoMocker.GetMock<IPropertyTypeInspector>();
    }

    private DecryptRequestBuilder CreateSubject()
    {
        return _autoMocker.CreateInstance<DecryptRequestBuilder>();
    }

    [Fact]
    public async Task DecryptAsync_WithAad_AssemblesRequestAndReturnsRunnerResult()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0xEE };
        var aad = new { context = "row-42" };
        object expected = 5L;
        _runner.Setup(r => r.DecryptAsync(new DecryptRequest(encrypted, aad), token)).ReturnsAsync(expected);

        var builder = CreateSubject();

        var result = await builder.FromValue(encrypted).WithAad(aad).DecryptAsync(token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DecryptAsync_WithPersistedAad_LeavesAadNull()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0xEE };
        object expected = "decrypted-value";
        _runner.Setup(r => r.DecryptAsync(new DecryptRequest(encrypted, null), token)).ReturnsAsync(expected);

        var builder = CreateSubject();

        var result = await builder.FromValue(encrypted).WithPersistedAad().DecryptAsync(token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void WithAad_WithANullAad_Throws()
    {
        var builder = CreateSubject();

        var act = () => builder.FromValue([0xEE]).WithAad(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithAad_WithAnAadTypeTheInspectorRejects_ThrowsImmediately()
    {
        var aad = 1.5;
        var rejection = new ArgumentException("unsupported aad");
        _propertyTypeInspector.Setup(i => i.ValidateAad(aad)).Throws(rejection);
        var builder = CreateSubject();

        var act = () => builder.FromValue([0xEE]).WithAad(aad);

        act.Should().Throw<ArgumentException>().Which.Should().BeSameAs(rejection);
    }

    [Fact]
    public void FromValue_WithANullValue_Throws()
    {
        var builder = CreateSubject();

        var act = () => builder.FromValue(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
