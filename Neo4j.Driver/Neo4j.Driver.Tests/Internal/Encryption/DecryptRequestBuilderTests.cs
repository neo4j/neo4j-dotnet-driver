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

public class DecryptRequestBuilderTests
{
    [Fact]
    public async Task DecryptAsync_WithAad_AssemblesRequestAndReturnsRunnerResult()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0xEE };
        var aad = new { context = "row-42" };
        object expected = 5L;
        var runner = new Mock<IEncryptionRequestRunner>();
        runner.Setup(r => r.DecryptAsync(new DecryptRequest(encrypted, aad, false), token)).ReturnsAsync(expected);

        var builder = new DecryptRequestBuilder(runner.Object);

        var result = await builder.FromValue(encrypted).WithAad(aad).DecryptAsync(token);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DecryptAsync_WithPersistedAad_SetsUsePersistedAadAndLeavesAadNull()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = new byte[] { 0xEE };
        object expected = "decrypted-value";
        var runner = new Mock<IEncryptionRequestRunner>();
        runner.Setup(r => r.DecryptAsync(new DecryptRequest(encrypted, null, true), token)).ReturnsAsync(expected);

        var builder = new DecryptRequestBuilder(runner.Object);

        var result = await builder.FromValue(encrypted).WithPersistedAad().DecryptAsync(token);

        result.Should().BeSameAs(expected);
    }
}
