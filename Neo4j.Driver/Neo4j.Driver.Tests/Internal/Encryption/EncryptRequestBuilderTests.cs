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

public class EncryptRequestBuilderTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<EncryptRequestBuilder>();
    private readonly Mock<IEncryptionRequestRunner> _runner;
    private readonly Mock<IPropertyTypeInspector> _propertyTypeInspector;

    public EncryptRequestBuilderTests()
    {
        _runner = _autoMocker.GetMock<IEncryptionRequestRunner>();
        _propertyTypeInspector = _autoMocker.GetMock<IPropertyTypeInspector>();
    }

    private EncryptRequestBuilder CreateSubject()
    {
        return _autoMocker.CreateInstance<EncryptRequestBuilder>();
    }

    [Fact]
    public async Task EncryptToBytesAsync_UsingKeyId_AssemblesRequestFromMandatoryStagesOnly()
    {
        var token = TestContext.Current.CancellationToken;
        var expected = new byte[] { 1, 2, 3 };
        _runner.Setup(r => r.EncryptToBytesAsync(
                new EncryptRequest("hello", null, null, new KeyReference("id-1", KeyReferenceType.Id)),
                token))
            .ReturnsAsync(expected);

        var builder = CreateSubject();

        var result = await builder.FromValue("hello").UsingKeyId("id-1").EncryptToBytesAsync(token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task EncryptToBytesAsync_UsingKeyAlias_SetsAnAliasKeyReference()
    {
        var token = TestContext.Current.CancellationToken;
        var expected = new byte[] { 4, 5 };
        _runner.Setup(r => r.EncryptToBytesAsync(
                new EncryptRequest(5L, null, null, new KeyReference("alias-1", KeyReferenceType.Alias)),
                token))
            .ReturnsAsync(expected);

        var builder = CreateSubject();

        var result = await builder.FromValue(5L).UsingKeyAlias("alias-1").EncryptToBytesAsync(token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task EncryptToBytesAsync_WithAadAndUsingProfile_IncludesThemInTheRequest()
    {
        var token = TestContext.Current.CancellationToken;
        var expected = new byte[] { 6 };
        var aad = new { context = "row-42" };
        _runner.Setup(r => r.EncryptToBytesAsync(
                new EncryptRequest("hello", aad, "profile-b", new KeyReference("id-1", KeyReferenceType.Id)),
                token))
            .ReturnsAsync(expected);

        var builder = CreateSubject();

        var result = await builder.FromValue("hello")
            .WithAad(aad)
            .UsingProfile("profile-b")
            .UsingKeyId("id-1")
            .EncryptToBytesAsync(token);

        result.Should().BeSameAs(expected);
    }
    [Fact]
    public async Task EncryptToBytesAsync_WithAFixedIv_CarriesTheIvOnTheRequest()
    {
        var token = TestContext.Current.CancellationToken;
        var iv = new byte[] { 0x70, 0x71 };
        var expected = new byte[] { 7 };
        _runner.Setup(r => r.EncryptToBytesAsync(
                new EncryptRequest("hello", null, null, new KeyReference("id-1", KeyReferenceType.Id), iv),
                token))
            .ReturnsAsync(expected);

        var step = CreateSubject().FromValue("hello");
        ((IInternalEncryptRequest)step).UseFixedIv(iv);

        var result = await step.UsingKeyId("id-1").EncryptToBytesAsync(token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void WithAad_WithANullAad_Throws()
    {
        var builder = CreateSubject();

        var act = () => builder.FromValue("hello").WithAad(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithAad_WithAnAadTypeTheInspectorRejects_ThrowsImmediately()
    {
        var aad = 1.5;
        var rejection = new ArgumentException("unsupported aad");
        _propertyTypeInspector.Setup(i => i.ValidateAad(aad)).Throws(rejection);
        var builder = CreateSubject();

        var act = () => builder.FromValue("hello").WithAad(aad);

        act.Should().Throw<ArgumentException>().Which.Should().BeSameAs(rejection);
    }

    [Fact]
    public void UsingKeyAlias_WithANullAlias_Throws()
    {
        var builder = CreateSubject();

        var act = () => builder.FromValue("hello").UsingKeyAlias(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UsingKeyId_WithANullId_Throws()
    {
        var builder = CreateSubject();

        var act = () => builder.FromValue("hello").UsingKeyId(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UsingProfile_WithANullProfileName_Throws()
    {
        var builder = CreateSubject();

        var act = () => builder.FromValue("hello").UsingProfile(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
