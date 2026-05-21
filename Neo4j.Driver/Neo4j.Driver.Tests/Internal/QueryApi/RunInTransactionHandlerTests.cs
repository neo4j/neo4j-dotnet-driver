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

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Running a query within an explicit transaction requires the transaction ID in the URL and, for Aura instances,
/// the <c>neo4j-cluster-affinity</c> header forwarded from BEGIN. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-run-query-in-transaction
/// </summary>
public class RunInTransactionHandlerTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    // Freezes the minimum mock chain needed to exercise the handler without crashing:
    // PostAsync("query/v2/tx/{txId}") → request → SendAsync(request) → response
    private void SetupChain()
    {
        var txContext = _fixture.Freeze<QueryApiTransactionContext>();
        var request = new HttpRequestMessage();
        var response = new HttpResponseMessage { Content = new ByteArrayContent([]) };

        _fixture.Freeze<Mock<IJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<RunInTransactionHandler.RequestBody>()))
            .Returns(new StringContent(""));

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync($"query/v2/tx/{txContext.TxId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiHttpClient>>()
            .Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    [Fact]
    public async Task Serializes_Statement_InRequestBody()
    {
        RunInTransactionHandler.RequestBody? capturedBody = null;
        SetupChain();

        _fixture.Freeze<Mock<IJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<RunInTransactionHandler.RequestBody>()))
            .Callback<RunInTransactionHandler.RequestBody>(b => capturedBody = b)
            .Returns(new StringContent(""));

        var subject = _fixture.Create<RunInTransactionHandler>();
        await subject.RunInTransactionAsync(new Query("MATCH (n) RETURN n"), TestContext.Current.CancellationToken);

        capturedBody.Should().NotBeNull();
        capturedBody!.Statement.Should().Be("MATCH (n) RETURN n");
    }

    [Fact]
    public async Task Serializes_Parameters_WhenQueryHasParameters()
    {
        var query = new Query("MATCH (n {id: $id}) RETURN n", new Dictionary<string, object> { ["id"] = 7 });
        RunInTransactionHandler.RequestBody? capturedBody = null;
        SetupChain();

        _fixture.Freeze<Mock<IJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<RunInTransactionHandler.RequestBody>()))
            .Callback<RunInTransactionHandler.RequestBody>(b => capturedBody = b)
            .Returns(new StringContent(""));

        var subject = _fixture.Create<RunInTransactionHandler>();
        await subject.RunInTransactionAsync(query, TestContext.Current.CancellationToken);

        capturedBody.Should().NotBeNull();
        capturedBody!.Parameters.Should().ContainKey("id");
    }

    [Fact]
    public async Task Returns_FieldsRowsAndBookmarks_FromDeserializedBody()
    {
        // Spec: successful response contains data.fields, data.values, and bookmarks
        var expectedBody = new QueryApiResultBody
        {
            Data = new QueryApiDataBody
            {
                Fields = ["x"],
                Values = [[JsonDocument.Parse("42").RootElement]]
            },
            Bookmarks = ["neo4j:bookmark:v1:tx200"]
        };

        SetupChain();

        _fixture.Freeze<Mock<IJsonDeserializer>>()
            .Setup(x => x.DeserializeAsync<QueryApiResultBody>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBody);

        var subject = _fixture.Create<RunInTransactionHandler>();
        var result = await subject.RunInTransactionAsync(
            new Query("RETURN 42 AS x"),
            TestContext.Current.CancellationToken);

        result.Fields.Should().Equal("x");
        result.Rows.Should().HaveCount(1);
        result.Bookmarks.Should().Equal("neo4j:bookmark:v1:tx200");
    }

    [Fact]
    public async Task Throws_WhenErrorCheckerThrows()
    {
        // Spec: errors surfaced by EnsureSuccessAsync must propagate to the caller
        _fixture.Freeze<Mock<IQueryApiErrorChecker>>()
            .Setup(x => x.EnsureSuccessAsync(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClientException("Neo.ClientError.General.Unknown", "server error"));

        SetupChain();
        var subject = _fixture.Create<RunInTransactionHandler>();
        var act = () => subject.RunInTransactionAsync(new Query("RETURN 1"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ClientException>();
    }
}
