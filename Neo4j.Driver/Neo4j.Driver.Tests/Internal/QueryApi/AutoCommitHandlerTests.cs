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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
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
    /// <summary>
    /// Sets up the minimum mock chain needed to exercise the handler without crashing:
    /// PostAsync(path) → request → SendAsync(request) → response.
    /// EnsureSuccessAsync and DeserializeAsync default to safe returns via AutoMocker.
    /// </summary>
    private static AutoMocker CreateChain(
        out HttpRequestMessage request,
        out HttpResponseMessage response,
        string path = "query/v2")
    {
        var mocker = new AutoMocker();
        var req = new HttpRequestMessage();
        var resp = new HttpResponseMessage { Content = new ByteArrayContent([]) };
        request = req;
        response = resp;

        mocker.GetMock<IQueryApiRequestBuilder>()
            .Setup(x => x.PostAsync(path, default))
            .ReturnsAsync(req);

        mocker.GetMock<IQueryApiHttpClient>()
            .Setup(x => x.SendAsync(req, default))
            .ReturnsAsync(resp);

        return mocker;
    }

    [Fact]
    public async Task Serializes_Statement_InRequestBody()
    {
        // Spec: request body must include a "statement" field
        var mocker = CreateChain(out _, out _);
        AutoCommitHandler.RequestBody? capturedBody = null;
        mocker.GetMock<IJsonSerializer>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Callback<AutoCommitHandler.RequestBody>(b => capturedBody = b);

        await mocker.CreateInstance<AutoCommitHandler>().AutoCommitAsync(new Query("MATCH (n) RETURN n"), []);

        capturedBody.Should().NotBeNull();
        capturedBody!.Statement.Should().Be("MATCH (n) RETURN n");
    }

    [Fact]
    public async Task Serializes_Parameters_WhenQueryHasParameters()
    {
        // Spec: parameters are passed as a key-value map under "parameters"
        var query = new Query("MATCH (n {id: $id})", new Dictionary<string, object> { ["id"] = 42 });
        var mocker = CreateChain(out _, out _);
        AutoCommitHandler.RequestBody? capturedBody = null;
        mocker.GetMock<IJsonSerializer>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Callback<AutoCommitHandler.RequestBody>(b => capturedBody = b);

        await mocker.CreateInstance<AutoCommitHandler>().AutoCommitAsync(query, []);

        capturedBody.Should().NotBeNull();
        capturedBody!.Parameters.Should().ContainKey("id");
    }

    [Fact]
    public async Task Omits_Parameters_WhenQueryHasNone()
    {
        // Null fields are omitted from the request body (WhenWritingNull option)
        var mocker = CreateChain(out _, out _);
        AutoCommitHandler.RequestBody? capturedBody = null;
        mocker.GetMock<IJsonSerializer>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Callback<AutoCommitHandler.RequestBody>(b => capturedBody = b);

        await mocker.CreateInstance<AutoCommitHandler>().AutoCommitAsync(new Query("RETURN 1"), []);

        capturedBody.Should().NotBeNull();
        capturedBody!.Parameters.Should().BeNull();
    }

    [Fact]
    public async Task Serializes_Bookmarks_WhenProvided()
    {
        // Spec: bookmarks enable causal consistency across requests
        var bookmarks = new List<string> { "neo4j:bookmark:v1:tx100", "neo4j:bookmark:v1:tx101" };
        var mocker = CreateChain(out _, out _);
        AutoCommitHandler.RequestBody? capturedBody = null;
        mocker.GetMock<IJsonSerializer>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Callback<AutoCommitHandler.RequestBody>(b => capturedBody = b);

        await mocker.CreateInstance<AutoCommitHandler>().AutoCommitAsync(new Query("RETURN 1"), bookmarks);

        capturedBody.Should().NotBeNull();
        capturedBody!.Bookmarks.Should().Equal("neo4j:bookmark:v1:tx100", "neo4j:bookmark:v1:tx101");
    }

    [Fact]
    public async Task Omits_Bookmarks_WhenListIsEmpty()
    {
        var mocker = CreateChain(out _, out _);
        AutoCommitHandler.RequestBody? capturedBody = null;
        mocker.GetMock<IJsonSerializer>()
            .Setup(x => x.Serialize(It.IsAny<AutoCommitHandler.RequestBody>()))
            .Callback<AutoCommitHandler.RequestBody>(b => capturedBody = b);

        await mocker.CreateInstance<AutoCommitHandler>().AutoCommitAsync(new Query("RETURN 1"), []);

        capturedBody.Should().NotBeNull();
        capturedBody!.Bookmarks.Should().BeNull();
    }

    [Fact]
    public async Task Returns_FieldsRowsAndBookmarks_FromDeserializedBody()
    {
        // Spec: successful response contains data.fields and data.values
        var mocker = CreateChain(out _, out _);
        var expectedBody = new QueryApiResultBody
        {
            Data = new QueryApiDataBody
            {
                Fields = ["name", "age"],
                Values = [[JsonDocument.Parse("\"Alice\"").RootElement, JsonDocument.Parse("30").RootElement]]
            },
            Bookmarks = ["neo4j:bookmark:v1:tx55"]
        };

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<QueryApiResultBody>(It.IsAny<Stream>(), default))
            .ReturnsAsync(expectedBody);

        var result = await mocker.CreateInstance<AutoCommitHandler>()
            .AutoCommitAsync(new Query("MATCH (n) RETURN n.name, n.age"), []);

        result.Fields.Should().Equal("name", "age");
        result.Rows.Should().HaveCount(1);
        result.Bookmarks.Should().Equal("neo4j:bookmark:v1:tx55");
    }

    [Fact]
    public async Task Returns_EmptyResponse_WhenDeserializedBodyIsNull()
    {
        var mocker = CreateChain(out _, out _);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<QueryApiResultBody>(It.IsAny<Stream>(), default))
            .ReturnsAsync((QueryApiResultBody?)null);

        var result = await mocker.CreateInstance<AutoCommitHandler>()
            .AutoCommitAsync(new Query("RETURN 1"), []);

        result.Fields.Should().BeEmpty();
        result.Rows.Should().BeEmpty();
        result.Bookmarks.Should().BeEmpty();
    }

    [Fact]
    public async Task PassesResponse_ToErrorChecker()
    {
        // Chain: SendAsync(request) → response → EnsureSuccessAsync(response)
        var mocker = CreateChain(out _, out var response);
        await mocker.CreateInstance<AutoCommitHandler>().AutoCommitAsync(new Query("RETURN 1"), []);

        mocker.GetMock<IQueryApiErrorChecker>()
            .Verify(x => x.EnsureSuccessAsync(response, default), Times.Once);
    }

    [Fact]
    public async Task PropagatesBodyError_WhenResponseContainsErrorArray()
    {
        // Spec: even on 202, the response body may contain an errors array
        var mocker = CreateChain(out _, out _);
        var bodyWithError = new QueryApiResultBody
        {
            Errors =
            [
                new QueryApiErrorBody("Neo.ClientError.Statement.SyntaxError", "Invalid Cypher")
            ]
        };

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<QueryApiResultBody>(It.IsAny<Stream>(), default))
            .ReturnsAsync(bodyWithError);

        mocker.GetMock<IQueryApiErrorChecker>()
            .Setup(x => x.ThrowIfAnyError("Neo.ClientError.Statement.SyntaxError", "Invalid Cypher"))
            .Throws(new ClientException("SyntaxError", "Invalid Cypher"));

        var act = () => mocker.CreateInstance<AutoCommitHandler>()
            .AutoCommitAsync(new Query("RETUN 1"), []);

        await act.Should().ThrowAsync<ClientException>();
    }
}
