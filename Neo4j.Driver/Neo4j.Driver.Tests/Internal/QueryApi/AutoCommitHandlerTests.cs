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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiTestHelpers;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Auto-commit queries execute a single Cypher statement outside an explicit transaction. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-run-autocommit-query
/// </summary>
public class AutoCommitHandlerTests
{
    private static readonly IAuthToken AnyAuth = AuthTokens.Basic("user", "pass");

    private static AutoMocker CreateMocker(FakeQueryApiHttpClient httpClient)
    {
        var mocker = new AutoMocker();
        mocker.Use<IQueryApiHttpClient>(httpClient);
        mocker.Use<IQueryApiUrlBuilder>(UrlBuilder);
        var json = new QueryApiJsonSerializer();
        mocker.Use<IJsonDeserializer>(json);
        mocker.Use<IJsonSerializer>(json);
        return mocker;
    }

    [Fact]
    public async Task SendsPost_ToAutoCommitEndpoint()
    {
        // POST /db/{database}/query/v2
        var httpClient = new FakeQueryApiHttpClient(
            AcceptedWith(
                new
                {
                    data = new { fields = Array.Empty<string>(), values = Array.Empty<object[]>() }
                }));
        var handler = CreateMocker(httpClient).CreateInstance<AutoCommitHandler>();

        await handler.AutoCommitAsync("movies", new Query("RETURN 1"), [], AnyAuth);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.RequestUri!.PathAndQuery.Should().Be("/db/movies/query/v2");
    }

    [Fact]
    public async Task RequestBody_ContainsStatement()
    {
        // Spec: request body must include a "statement" field
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse()));
        var handler = CreateMocker(httpClient).CreateInstance<AutoCommitHandler>();

        await handler.AutoCommitAsync("neo4j", new Query("MATCH (n) RETURN n"), [], AnyAuth);

        var body = JsonDocument.Parse(httpClient.LastRequestBody!).RootElement;
        body.GetProperty("statement").GetString().Should().Be("MATCH (n) RETURN n");
    }

    [Fact]
    public async Task RequestBody_IncludesParameters_WhenQueryHasParameters()
    {
        // Spec: parameters are passed as a key-value map under "parameters"
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse()));
        var query = new Query("MATCH (n) WHERE n.id = $id RETURN n", new Dictionary<string, object> { ["id"] = 42 });
        var handler = CreateMocker(httpClient).CreateInstance<AutoCommitHandler>();

        await handler.AutoCommitAsync("neo4j", query, [], AnyAuth);

        var body = JsonDocument.Parse(httpClient.LastRequestBody!).RootElement;
        body.GetProperty("parameters").GetProperty("id").GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task RequestBody_OmitsParameters_WhenQueryHasNoParameters()
    {
        // Null fields are omitted from the request body (WhenWritingNull option)
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse()));
        var handler = CreateMocker(httpClient).CreateInstance<AutoCommitHandler>();

        await handler.AutoCommitAsync("neo4j", new Query("RETURN 1"), [], AnyAuth);

        var body = JsonDocument.Parse(httpClient.LastRequestBody!).RootElement;
        body.TryGetProperty("parameters", out var _).Should().BeFalse();
    }

    [Fact]
    public async Task RequestBody_IncludesBookmarks_WhenProvided()
    {
        // Spec: bookmarks enable causal consistency across requests
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse()));
        var bookmarks = new List<string> { "neo4j:bookmark:v1:tx100", "neo4j:bookmark:v1:tx101" };
        var handler = CreateMocker(httpClient).CreateInstance<AutoCommitHandler>();

        await handler.AutoCommitAsync("neo4j", new Query("RETURN 1"), bookmarks, AnyAuth);

        var body = JsonDocument.Parse(httpClient.LastRequestBody!).RootElement;
        var parsedBookmarks = body.GetProperty("bookmarks")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        parsedBookmarks.Should().Equal("neo4j:bookmark:v1:tx100", "neo4j:bookmark:v1:tx101");
    }

    [Fact]
    public async Task RequestBody_OmitsBookmarks_WhenListIsEmpty()
    {
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse()));
        var handler = CreateMocker(httpClient).CreateInstance<AutoCommitHandler>();

        await handler.AutoCommitAsync("neo4j", new Query("RETURN 1"), [], AnyAuth);

        var body = JsonDocument.Parse(httpClient.LastRequestBody!).RootElement;
        body.TryGetProperty("bookmarks", out var _).Should().BeFalse();
    }

    [Fact]
    public async Task Response_ReturnsFieldNamesAndRows()
    {
        // Spec: successful response contains data.fields and data.values
        var httpClient = new FakeQueryApiHttpClient(
            AcceptedWith(
                new
                {
                    data = new
                    {
                        fields = new[] { "name", "age" },
                        values = new[] { new object[] { "Alice", 30 }, new object[] { "Bob", 25 } }
                    },
                    bookmarks = new[] { "neo4j:bookmark:v1:tx55" }
                }));
        var handler = CreateMocker(httpClient).CreateInstance<AutoCommitHandler>();

        var result = await handler.AutoCommitAsync("neo4j", new Query("MATCH (n) RETURN n.name, n.age"), [], AnyAuth);

        result.Fields.Should().Equal("name", "age");
        result.Rows.Should().HaveCount(2);
        result.Bookmarks.Should().Equal("neo4j:bookmark:v1:tx55");
    }

    [Fact]
    public async Task Response_ReturnsEmptyFields_WhenDataIsAbsent()
    {
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(new {}));
        var handler = CreateMocker(httpClient).CreateInstance<AutoCommitHandler>();

        var result = await handler.AutoCommitAsync("neo4j", new Query("RETURN 1"), [], AnyAuth);

        result.Fields.Should().BeEmpty();
        result.Rows.Should().BeEmpty();
        result.Bookmarks.Should().BeEmpty();
    }

    [Fact]
    public async Task CallsAuthApplicator_WithProvidedToken()
    {
        var token = AuthTokens.Basic("alice", "secret");
        var mocker = CreateMocker(new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse())));

        await mocker.CreateInstance<AutoCommitHandler>().AutoCommitAsync("neo4j", new Query("RETURN 1"), [], token);

        mocker.GetMock<IAuthApplicator>().Verify(x => x.Apply(It.IsAny<HttpRequestMessage>(), token), Times.Once);
    }

    [Fact]
    public async Task CallsErrorChecker_OnResponse()
    {
        var mocker = CreateMocker(new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse())));

        await mocker.CreateInstance<AutoCommitHandler>().AutoCommitAsync("neo4j", new Query("RETURN 1"), [], AnyAuth);

        mocker.GetMock<IQueryApiErrorChecker>()
            .Verify(x => x.EnsureSuccessAsync(It.IsAny<HttpResponseMessage>(), default), Times.Once);
    }

    [Fact]
    public async Task PropagatesBodyError_WhenResponseContainsErrorArray()
    {
        // Spec: even on 202, the response body may contain an errors array
        var response = AcceptedWith(
            new
            {
                errors = new[] { new { code = "Neo.ClientError.Statement.SyntaxError", message = "Invalid Cypher" } }
            });
        var mocker = CreateMocker(new FakeQueryApiHttpClient(response));
        mocker.GetMock<IQueryApiErrorChecker>()
            .Setup(x => x.ThrowIfAnyError(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new ClientException("SyntaxError", "Invalid Cypher"));

        var act = () => mocker.CreateInstance<AutoCommitHandler>()
            .AutoCommitAsync("neo4j", new Query("RETUN 1"), [], AnyAuth);

        await act.Should().ThrowAsync<ClientException>();
    }

    private static object EmptyDataResponse()
    {
        return new
        {
            data = new { fields = Array.Empty<string>(), values = Array.Empty<object[]>() }
        };
    }
}
