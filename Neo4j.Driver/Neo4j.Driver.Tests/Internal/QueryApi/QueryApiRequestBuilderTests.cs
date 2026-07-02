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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Verifies the HTTP request structure produced by <see cref="QueryApiRequestBuilder"/>.
/// Spec: https://neo4j.com/docs/query-api/current/
/// </summary>
public class QueryApiRequestBuilderTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    private sealed record TestBody : IQueryApiRequestBody
    {
        public IReadOnlyCollection<object?> GetParameterValues()
        {
            return [];
        }
    }

    public QueryApiRequestBuilderTests()
    {
        _fixture.Freeze<Mock<IQueryApiJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<IQueryApiRequestBody>()))
            .Returns(new SerializedBody("{}", QueryApiMediaVersion.V1_0));
    }

    [Fact]
    public async Task PostAsync_SetsContentType_WithoutCharset()
    {
        // StringContent defaults to charset=utf-8 unless MediaTypeHeaderValue is used explicitly —
        // the server rejects requests with a charset parameter on this media type.
        var subject = _fixture.Create<QueryApiRequestBuilder>();
        var request = await subject.PostAsync("query/v2", new TestBody(), TestContext.Current.CancellationToken);

        var contentType = request.Content!.Headers.ContentType;
        contentType!.MediaType.Should().Be("application/vnd.neo4j.query.v1.0");
        contentType.CharSet.Should().BeNull();
    }

    [Fact]
    public async Task PostAsync_SetsAcceptHeader_WithTypedJsonMediaType()
    {
        var subject = _fixture.Create<QueryApiRequestBuilder>();
        var request = await subject.PostAsync("query/v2", new TestBody(), TestContext.Current.CancellationToken);

        request.Headers.Accept.Should().Contain(h => h.MediaType == "application/vnd.neo4j.query.v1.0");
    }

    [Fact]
    public async Task PostAsync_BuildsUrl_UsingDatabaseFromSessionContext()
    {
        _fixture.Freeze<Mock<ISessionContext>>()
            .Setup(x => x.Database)
            .Returns("mydb");

        _fixture.Freeze<Mock<IQueryApiUrlBuilder>>()
            .Setup(x => x.Build("db/mydb/query/v2"))
            .Returns(new Uri("https://host/db/mydb/query/v2"));

        var subject = _fixture.Create<QueryApiRequestBuilder>();
        var request = await subject.PostAsync("query/v2", new TestBody(), TestContext.Current.CancellationToken);

        request.RequestUri.Should().Be(new Uri("https://host/db/mydb/query/v2"));
    }

    [Fact]
    public async Task PostAsync_CallsAllEnrichers()
    {
        var enricher1 = new Mock<IHttpRequestEnricher>();
        var enricher2 = new Mock<IHttpRequestEnricher>();
        _fixture.Register<IEnumerable<IHttpRequestEnricher>>(() => [enricher1.Object, enricher2.Object]);

        var subject = _fixture.Create<QueryApiRequestBuilder>();
        await subject.PostAsync("query/v2", new TestBody(), TestContext.Current.CancellationToken);

        enricher1.Verify(x => x.Enrich(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        enricher2.Verify(x => x.Enrich(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostAsync_SetsBodyContent_FromSerializer()
    {
        _fixture.Freeze<Mock<IQueryApiJsonSerializer>>()
            .Setup(x => x.Serialize(It.IsAny<IQueryApiRequestBody>()))
            .Returns(new SerializedBody("{\"statement\":\"RETURN 1\"}", QueryApiMediaVersion.V1_0));

        var subject = _fixture.Create<QueryApiRequestBuilder>();
        var request = await subject.PostAsync("query/v2", new TestBody(), TestContext.Current.CancellationToken);

        var body = await request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("{\"statement\":\"RETURN 1\"}");
    }

    [Fact]
    public async Task DeleteAsync_HasNoContent()
    {
        var subject = _fixture.Create<QueryApiRequestBuilder>();
        var request = await subject.DeleteAsync("query/v2/tx/tx-1", TestContext.Current.CancellationToken);

        request.Method.Should().Be(HttpMethod.Delete);
        request.Content.Should().BeNull();
    }
}
