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
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Tests.Internal.Core;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiRequestBuilderTests
{
    private readonly AutoMocker _autoMocker = AutoMockerExtensions.ForTesting<QueryApiRequestBuilder>();

    private sealed record TestBody : IQueryApiRequestBody
    {
        public IReadOnlyCollection<object?> GetParameterValues()
        {
            return [];
        }
    }

    public QueryApiRequestBuilderTests()
    {
        _autoMocker.Use<IEnumerable<IHttpRequestEnricher>>(Array.Empty<IHttpRequestEnricher>());

        _autoMocker.GetMock<ISessionContext>()
            .SetupGet(x => x.Database)
            .Returns("neo4j");

        _autoMocker.GetMock<IQueryApiJsonSerializer>()
            .Setup(x => x.Serialize(It.IsAny<IQueryApiRequestBody>()))
            .Returns(new SerializedBody("{}", QueryApiMediaVersion.V1_0));
    }

    [Fact]
    public async Task PostAsync_AppliesResolvedMediaVersion_FromSerializer()
    {
        _autoMocker.GetMock<IQueryApiJsonSerializer>()
            .Setup(x => x.Serialize(It.IsAny<IQueryApiRequestBody>()))
            .Returns(new SerializedBody("{}", QueryApiMediaVersion.V1_1));

        var headerWriter = _autoMocker.GetMock<IQueryApiRequestHeaderWriter>();

        var subject = _autoMocker.CreateInstance<QueryApiRequestBuilder>();
        await subject.PostAsync("query/v2", new TestBody(), TestContext.Current.CancellationToken);

        headerWriter.Verify(
            x => x.ApplyMediaType(It.IsAny<HttpRequestMessage>(), QueryApiMediaVersion.V1_1),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_AppliesV1_0()
    {
        var headerWriter = _autoMocker.GetMock<IQueryApiRequestHeaderWriter>();

        var subject = _autoMocker.CreateInstance<QueryApiRequestBuilder>();
        await subject.DeleteAsync("query/v2/tx/tx-1", TestContext.Current.CancellationToken);

        headerWriter.Verify(
            x => x.ApplyMediaType(It.IsAny<HttpRequestMessage>(), QueryApiMediaVersion.V1_0),
            Times.Once);
    }

    [Fact]
    public async Task PostAsync_BuildsUrl_UsingDatabaseFromSessionContext()
    {
        _autoMocker.GetMock<ISessionContext>()
            .SetupGet(x => x.Database)
            .Returns("mydb");

        _autoMocker.GetMock<IQueryApiUrlBuilder>()
            .Setup(x => x.Build("db/mydb/query/v2"))
            .Returns(new Uri("https://host/db/mydb/query/v2"));

        var subject = _autoMocker.CreateInstance<QueryApiRequestBuilder>();
        var request = await subject.PostAsync("query/v2", new TestBody(), TestContext.Current.CancellationToken);

        request.RequestUri.Should().Be(new Uri("https://host/db/mydb/query/v2"));
    }

    [Fact]
    public async Task PostAsync_CallsAllEnrichers()
    {
        var enricher1 = new Mock<IHttpRequestEnricher>();
        var enricher2 = new Mock<IHttpRequestEnricher>();
        _autoMocker.Use<IEnumerable<IHttpRequestEnricher>>([enricher1.Object, enricher2.Object]);

        var subject = _autoMocker.CreateInstance<QueryApiRequestBuilder>();
        await subject.PostAsync("query/v2", new TestBody(), TestContext.Current.CancellationToken);

        enricher1.Verify(x => x.Enrich(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        enricher2.Verify(x => x.Enrich(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostAsync_SetsBodyContent_FromSerializer()
    {
        _autoMocker.GetMock<IQueryApiJsonSerializer>()
            .Setup(x => x.Serialize(It.IsAny<IQueryApiRequestBody>()))
            .Returns(new SerializedBody("{\"statement\":\"RETURN 1\"}", QueryApiMediaVersion.V1_0));

        var subject = _autoMocker.CreateInstance<QueryApiRequestBuilder>();
        var request = await subject.PostAsync("query/v2", new TestBody(), TestContext.Current.CancellationToken);

        var body = await request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("{\"statement\":\"RETURN 1\"}");
    }

    [Fact]
    public async Task DeleteAsync_HasNoContent()
    {
        var subject = _autoMocker.CreateInstance<QueryApiRequestBuilder>();
        var request = await subject.DeleteAsync("query/v2/tx/tx-1", TestContext.Current.CancellationToken);

        request.Method.Should().Be(HttpMethod.Delete);
        request.Content.Should().BeNull();
    }

    [Fact]
    public async Task PostAsync_WithNullBody_StillSetsContent()
    {
        var headerWriter = _autoMocker.GetMock<IQueryApiRequestHeaderWriter>();

        var subject = _autoMocker.CreateInstance<QueryApiRequestBuilder>();
        var request = await subject.PostAsync(
            "query/v2/tx/tx-1/commit",
            null,
            TestContext.Current.CancellationToken);

        request.Content.Should().NotBeNull();
        headerWriter.Verify(
            x => x.ApplyMediaType(It.IsAny<HttpRequestMessage>(), QueryApiMediaVersion.V1_0),
            Times.Once);
    }
}
