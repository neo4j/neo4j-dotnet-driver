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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Beginning a transaction reserves a server-side transaction and returns its ID, plus the optional
/// <c>neo4j-cluster-affinity</c> header that must be forwarded on all subsequent requests. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-begin-transaction
/// </summary>
public class BeginTransactionHandlerTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    private void SetupChain(string txId = "tx-1", HttpResponseHeaders? headers = null)
    {
        headers ??= new HttpResponseMessage().Headers;
        var request = new HttpRequestMessage();

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiClient>>()
            .Setup(x => x.ExecuteAsync<BeginTransactionHandler.ResponseBody>(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<BeginTransactionHandler.ResponseBody>(
                new BeginTransactionHandler.ResponseBody
                {
                    Transaction = new BeginTransactionHandler.TransactionInfo { Id = txId }
                },
                headers));
    }

    [Fact]
    public async Task ReturnsTransactionId_FromDeserializedBody()
    {
        // Spec: response body contains transaction.id — the handle for subsequent requests
        SetupChain(txId: "tx-abc-123");

        var subject = _fixture.Create<BeginTransactionHandler>();
        var context = await subject.BeginTransactionAsync([], TestContext.Current.CancellationToken);

        context.TxId.Should().Be("tx-abc-123");
    }

    [Fact]
    public async Task ReturnsClusterAffinity_WhenResponseCarriesAffinityHeader()
    {
        SetupChain();

        _fixture.Freeze<Mock<IClusterAffinityExtractor>>()
            .Setup(x => x.Extract(It.IsAny<HttpResponseHeaders>())).Returns("shard-99");

        var subject = _fixture.Create<BeginTransactionHandler>();
        var context = await subject.BeginTransactionAsync([], TestContext.Current.CancellationToken);

        context.ClusterAffinity.Should().Be("shard-99");
    }

    [Fact]
    public async Task ReturnsNullClusterAffinity_WhenResponseHasNoAffinityHeader()
    {
        SetupChain();

        _fixture.Freeze<Mock<IClusterAffinityExtractor>>()
            .Setup(x => x.Extract(It.IsAny<HttpResponseHeaders>())).Returns((string?)null);

        var subject = _fixture.Create<BeginTransactionHandler>();
        var context = await subject.BeginTransactionAsync([], TestContext.Current.CancellationToken);

        context.ClusterAffinity.Should().BeNull();
    }

    [Fact]
    public async Task Throws_WhenDeserializedBodyHasNoTransactionId()
    {
        var request = new HttpRequestMessage();

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiClient>>()
            .Setup(x => x.ExecuteAsync<BeginTransactionHandler.ResponseBody>(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<BeginTransactionHandler.ResponseBody>(
                new BeginTransactionHandler.ResponseBody
                {
                    Transaction = new BeginTransactionHandler.TransactionInfo { Id = null }
                },
                new HttpResponseMessage().Headers));

        var subject = _fixture.Create<BeginTransactionHandler>();
        var act = () => subject.BeginTransactionAsync([], TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*transaction ID*");
    }

    [Fact]
    public async Task RequestBody_IncludesImpersonatedUser_FromSessionContext()
    {
        _fixture.Freeze<Mock<ISessionContext>>()
            .Setup(x => x.ImpersonatedUser).Returns("banana_bob");

        var request = new HttpRequestMessage();
        object? capturedBody = null;

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, object, CancellationToken>((_, body, _) => capturedBody = body)
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiClient>>()
            .Setup(x => x.ExecuteAsync<BeginTransactionHandler.ResponseBody>(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<BeginTransactionHandler.ResponseBody>(
                new BeginTransactionHandler.ResponseBody
                {
                    Transaction = new BeginTransactionHandler.TransactionInfo { Id = "tx-1" }
                },
                new HttpResponseMessage().Headers));

        var subject = _fixture.Create<BeginTransactionHandler>();
        await subject.BeginTransactionAsync([], TestContext.Current.CancellationToken);

        var body = capturedBody.Should().BeOfType<BeginTransactionHandler.RequestBody>().Subject;
        body.ImpersonatedUser.Should().Be("banana_bob");
    }
}
