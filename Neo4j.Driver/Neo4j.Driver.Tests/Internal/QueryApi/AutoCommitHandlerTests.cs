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

using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi;
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

    private HttpResponseMessage SetupChain()
    {
        var response = new HttpResponseMessage { Content = new ByteArrayContent([]) };

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpRequestMessage());

        _fixture.Freeze<Mock<IQueryApiHttpClient>>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        return response;
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
    public async Task Throws_WhenErrorCheckerThrows()
    {
        _fixture.Freeze<Mock<IQueryApiErrorChecker>>()
            .Setup(x => x.EnsureSuccessAsync(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClientException("Neo.ClientError.General.Unknown", "server error"));

        SetupChain();
        var subject = _fixture.Create<AutoCommitHandler>();
        var act = () => subject.AutoCommitAsync(new Query("RETURN 1"), [], TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ClientException>();
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
            .Setup(x => x.ThrowIfErrors(It.Is<QueryApiErrorBody[]?>(e => e != null && e[0].Code == "Neo.ClientError.Statement.SyntaxError")))
            .Throws(new ClientException("SyntaxError", "Invalid Cypher"));

        var subject = _fixture.Create<AutoCommitHandler>();
        var act = () => subject.AutoCommitAsync(new Query("RETUN 1"), [], TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ClientException>();
    }

    [Fact]
    public async Task RequestBody_IncludesImpersonatedUserAndAccessMode_FromSessionContext()
    {
        // Spec: impersonatedUser and accessMode must be forwarded from the session context on every request
        _fixture.Freeze<Mock<ISessionContext>>()
            .Setup(x => x.ImpersonatedUser).Returns("banana_bob");
        _fixture.Freeze<Mock<ISessionContext>>()
            .Setup(x => x.AccessMode).Returns(AccessMode.Read);

        object? capturedBody = null;
        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, object, CancellationToken>((_, body, _) => capturedBody = body)
            .ReturnsAsync(new HttpRequestMessage());

        _fixture.Freeze<Mock<IQueryApiHttpClient>>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage { Content = new ByteArrayContent([]) });

        var subject = _fixture.Create<AutoCommitHandler>();
        await subject.AutoCommitAsync(new Query("RETURN 1"), [], TestContext.Current.CancellationToken);

        var body = capturedBody.Should().BeOfType<AutoCommitHandler.RequestBody>().Subject;
        body.ImpersonatedUser.Should().Be("banana_bob");
        body.AccessMode.Should().Be("Read");
    }
}
