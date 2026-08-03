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
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EncryptRequestBuilderTests
{
    [Fact]
    public async Task EncryptToBytesAsync_UsingKeyId_AssemblesRequestFromMandatoryStagesOnly()
    {
        var token = TestContext.Current.CancellationToken;
        var expected = new byte[] { 1, 2, 3 };
        var runner = new Mock<IEncryptionRequestRunner>();
        runner.Setup(r => r.EncryptToBytesAsync(
                new EncryptRequest("hello", null, null, new KeyReference("id-1", KeyReferenceType.Id)),
                token))
            .ReturnsAsync(expected);

        var builder = new EncryptRequestBuilder(runner.Object);

        var result = await builder.FromValue("hello").UsingKeyId("id-1").EncryptToBytesAsync(token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task EncryptToBytesAsync_UsingKeyAlias_SetsAnAliasKeyReference()
    {
        var token = TestContext.Current.CancellationToken;
        var expected = new byte[] { 4, 5 };
        var runner = new Mock<IEncryptionRequestRunner>();
        runner.Setup(r => r.EncryptToBytesAsync(
                new EncryptRequest(5L, null, null, new KeyReference("alias-1", KeyReferenceType.Alias)),
                token))
            .ReturnsAsync(expected);

        var builder = new EncryptRequestBuilder(runner.Object);

        var result = await builder.FromValue(5L).UsingKeyAlias("alias-1").EncryptToBytesAsync(token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task EncryptToBytesAsync_WithAadAndUsingProfile_IncludesThemInTheRequest()
    {
        var token = TestContext.Current.CancellationToken;
        var expected = new byte[] { 6 };
        var aad = new { context = "row-42" };
        var runner = new Mock<IEncryptionRequestRunner>();
        runner.Setup(r => r.EncryptToBytesAsync(
                new EncryptRequest("hello", aad, "profile-b", new KeyReference("id-1", KeyReferenceType.Id)),
                token))
            .ReturnsAsync(expected);

        var builder = new EncryptRequestBuilder(runner.Object);

        var result = await builder.FromValue("hello")
            .WithAad(aad)
            .UsingProfile("profile-b")
            .UsingKeyId("id-1")
            .EncryptToBytesAsync(token);

        result.Should().BeSameAs(expected);
    }
}
