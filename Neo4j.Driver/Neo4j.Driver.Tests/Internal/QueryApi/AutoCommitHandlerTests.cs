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
/// Auto-commit queries execute a single Cypher statement outside an explicit transaction. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-run-autocommit-query
/// </summary>
public class AutoCommitHandlerTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    // Freezes the minimum mock chain needed to exercise the handler without crashing:
    private HttpResponseMessage SetupChain(string path = "query/v2")
    {
        var request = new HttpRequestMessage();
        var response = new HttpResponseMessage { Content = new ByteArrayContent([]) };

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiHttpClient>>()
            .Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _fixture.Freeze<Mock<IJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Returns(new StringContent(""));

        return response;
    }

    [Fact]
    public async Task Serializes_Statement_InRequestBody()
    {
        // Spec: request body must include a "statement" field
        AutoCommitHandler.RequestBody? capturedBody = null;
        SetupChain();
        _fixture.Freeze<Mock<IJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Callback<AutoCommitHandler.RequestBody>(b => capturedBody = b)
            .Returns(new StringContent(""));

        var subject = _fixture.Create<AutoCommitHandler>();
        await subject.AutoCommitAsync(new Query("MATCH (n) RETURN n"), [], TestContext.Current.CancellationToken);

        capturedBody.Should().NotBeNull();
        capturedBody!.Statement.Should().Be("MATCH (n) RETURN n");
    }

    [Fact]
    public async Task Serializes_Parameters_WhenQueryHasParameters()
    {
        // Spec: parameters are passed as a key-value map under "parameters"
        var query = new Query("MATCH (n {id: $id})", new Dictionary<string, object> { ["id"] = 42 });
        AutoCommitHandler.RequestBody? capturedBody = null;
        SetupChain();
        _fixture.Freeze<Mock<IJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Callback<AutoCommitHandler.RequestBody>(b => capturedBody = b)
            .Returns(new StringContent(""));

        var subject = _fixture.Create<AutoCommitHandler>();
        await subject.AutoCommitAsync(query, [], TestContext.Current.CancellationToken);

        capturedBody.Should().NotBeNull();
        capturedBody!.Parameters.Should().ContainKey("id");
    }

    [Fact]
    public async Task Omits_Parameters_WhenQueryHasNone()
    {
        // Null fields are omitted from the request body (WhenWritingNull option)
        AutoCommitHandler.RequestBody? capturedBody = null;
        SetupChain();
        _fixture.Freeze<Mock<IJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Callback<AutoCommitHandler.RequestBody>(b => capturedBody = b)
            .Returns(new StringContent(""));

        var subject = _fixture.Create<AutoCommitHandler>();
        await subject.AutoCommitAsync(new Query("RETURN 1"), [], TestContext.Current.CancellationToken);

        capturedBody.Should().NotBeNull();
        capturedBody!.Parameters.Should().BeNull();
    }

    [Fact]
    public async Task Serializes_Bookmarks_WhenProvided()
    {
        // Spec: bookmarks enable causal consistency across requests
        var bookmarks = new List<string> { "neo4j:bookmark:v1:tx100", "neo4j:bookmark:v1:tx101" };
        AutoCommitHandler.RequestBody? capturedBody = null;
        SetupChain();
        _fixture.Freeze<Mock<IJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Callback<AutoCommitHandler.RequestBody>(b => capturedBody = b)
            .Returns(new StringContent(""));

        var subject = _fixture.Create<AutoCommitHandler>();
        await subject.AutoCommitAsync(new Query("RETURN 1"), bookmarks, TestContext.Current.CancellationToken);

        capturedBody.Should().NotBeNull();
        capturedBody!.Bookmarks.Should().Equal("neo4j:bookmark:v1:tx100", "neo4j:bookmark:v1:tx101");
    }

    [Fact]
    public async Task Omits_Bookmarks_WhenListIsEmpty()
    {
        AutoCommitHandler.RequestBody? capturedBody = null;
        SetupChain();
        _fixture.Freeze<Mock<IJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Callback<AutoCommitHandler.RequestBody>(b => capturedBody = b)
            .Returns(new StringContent(""));

        var subject = _fixture.Create<AutoCommitHandler>();
        await subject.AutoCommitAsync(new Query("RETURN 1"), [], TestContext.Current.CancellationToken);

        capturedBody.Should().NotBeNull();
        capturedBody!.Bookmarks.Should().BeNull();
    }

    [Fact]
    public async Task Returns_FieldsRowsAndBookmarks_FromDeserializedBody()
    {
        // Spec: successful response contains data.fields and data.values
        var expectedBody = new QueryApiResultBody
        {
            Data = new QueryApiDataBody
            {
                Fields = ["name", "age"],
                Values = [[JsonDocument.Parse("\"Alice\"").RootElement, JsonDocument.Parse("30").RootElement]]
            },
            Bookmarks = ["neo4j:bookmark:v1:tx55"]
        };

        SetupChain();
        _fixture.Freeze<Mock<IJsonDeserializer>>()
            .Setup(x => x.DeserializeAsync<QueryApiResultBody>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBody);

        var subject = _fixture.Create<AutoCommitHandler>();
        var result = await subject.AutoCommitAsync(
            new Query("MATCH (n) RETURN n.name, n.age"),
            [],
            TestContext.Current.CancellationToken);

        result.Fields.Should().Equal("name", "age");
        result.Rows.Should().HaveCount(1);
        result.Bookmarks.Should().Equal("neo4j:bookmark:v1:tx55");
    }

    [Fact]
    public async Task Returns_EmptyResponse_WhenDeserializedBodyIsNull()
    {
        SetupChain();
        _fixture.Freeze<Mock<IJsonDeserializer>>()
            .Setup(x => x.DeserializeAsync<QueryApiResultBody>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryApiResultBody?)null);

        var subject = _fixture.Create<AutoCommitHandler>();
        var result = await subject.AutoCommitAsync(new Query("RETURN 1"), [], TestContext.Current.CancellationToken);

        result.Fields.Should().BeEmpty();
        result.Rows.Should().BeEmpty();
        result.Bookmarks.Should().BeEmpty();
    }

    [Fact]
    public async Task PassesResponse_ToErrorChecker()
    {
        // Chain: SendAsync(request) → response → EnsureSuccessAsync(response)
        HttpResponseMessage? capturedResponse = null;
        _fixture.Freeze<Mock<IQueryApiErrorChecker>>()
            .Setup(x => x.EnsureSuccessAsync(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .Callback<HttpResponseMessage, CancellationToken>((r, _) => capturedResponse = r);

        var response = SetupChain();
        var subject = _fixture.Create<AutoCommitHandler>();
        await subject.AutoCommitAsync(new Query("RETURN 1"), [], TestContext.Current.CancellationToken);

        capturedResponse.Should().BeSameAs(response);
    }

    [Fact]
    public async Task PropagatesBodyError_WhenResponseContainsErrorArray()
    {
        // Spec: even on 202, the response body may contain an errors array
        var bodyWithError = new QueryApiResultBody
        {
            Errors =
            [
                new QueryApiErrorBody("Neo.ClientError.Statement.SyntaxError", "Invalid Cypher")
            ]
        };

        SetupChain();
        _fixture.Freeze<Mock<IJsonDeserializer>>()
            .Setup(x => x.DeserializeAsync<QueryApiResultBody>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bodyWithError);

        _fixture.Freeze<Mock<IQueryApiErrorChecker>>()
            .Setup(x => x.ThrowIfAnyError("Neo.ClientError.Statement.SyntaxError", "Invalid Cypher"))
            .Throws(new ClientException("SyntaxError", "Invalid Cypher"));

        var subject = _fixture.Create<AutoCommitHandler>();
        var act = () => subject.AutoCommitAsync(new Query("RETUN 1"), [], TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ClientException>();
    }
}
