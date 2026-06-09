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
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Beginning a transaction reserves a server-side transaction and returns its ID, plus the optional
/// <c>neo4j-cluster-affinity</c> header that must be forwarded on all subsequent requests. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-begin-transaction
/// </summary>
public class TransactionBeginnerTests
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
            .Setup(x => x.ExecuteAsync<TransactionBeginner.ResponseBody>(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<TransactionBeginner.ResponseBody>(
                new TransactionBeginner.ResponseBody
                {
                    Transaction = new TransactionBeginner.TransactionInfo { Id = txId }
                },
                headers));
    }

    [Fact]
    public async Task BeginAsync_StoresTransactionId_InContextHolder()
    {
        // Spec: response body contains transaction.id — the handle for subsequent requests
        SetupChain(txId: "tx-abc-123");

        var holder = _fixture.Freeze<QueryApiTransactionContextHolder>();
        var subject = _fixture.Create<TransactionBeginner>();
        await subject.BeginAsync(TestContext.Current.CancellationToken);

        holder.Context.Should().NotBeNull();
        holder.Context!.TxId.Should().Be("tx-abc-123");
    }

    [Fact]
    public async Task BeginAsync_StoresClusterAffinity_InContextHolder()
    {
        SetupChain();

        _fixture.Freeze<Mock<IClusterAffinityExtractor>>()
            .Setup(x => x.Extract(It.IsAny<HttpResponseHeaders>())).Returns("shard-99");

        var holder = _fixture.Freeze<QueryApiTransactionContextHolder>();
        var subject = _fixture.Create<TransactionBeginner>();
        await subject.BeginAsync(TestContext.Current.CancellationToken);

        holder.Context!.ClusterAffinity.Should().Be("shard-99");
    }

    [Fact]
    public async Task BeginAsync_StoresNullClusterAffinity_WhenResponseHasNoAffinityHeader()
    {
        SetupChain();

        _fixture.Freeze<Mock<IClusterAffinityExtractor>>()
            .Setup(x => x.Extract(It.IsAny<HttpResponseHeaders>())).Returns((string?)null);

        var holder = _fixture.Freeze<QueryApiTransactionContextHolder>();
        var subject = _fixture.Create<TransactionBeginner>();
        await subject.BeginAsync(TestContext.Current.CancellationToken);

        holder.Context!.ClusterAffinity.Should().BeNull();
    }

    [Fact]
    public async Task BeginAsync_Throws_WhenDeserializedBodyHasNoTransactionId()
    {
        var request = new HttpRequestMessage();

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiClient>>()
            .Setup(x => x.ExecuteAsync<TransactionBeginner.ResponseBody>(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<TransactionBeginner.ResponseBody>(
                new TransactionBeginner.ResponseBody
                {
                    Transaction = new TransactionBeginner.TransactionInfo { Id = null }
                },
                new HttpResponseMessage().Headers));

        var subject = _fixture.Create<TransactionBeginner>();
        var act = () => subject.BeginAsync(TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*transaction ID*");
    }

    [Fact]
    public async Task BeginAsync_IncludesCurrentBookmarks_FromTracker_InRequest()
    {
        // Spec: current session bookmarks must be forwarded in the request for causal consistency
        var tracker = new BookmarkTracker(SessionConfig.Builder.Build());
        tracker.UpdateBookmarks(["session-bookmark"]);
        _fixture.Inject<IBookmarkTracker>(tracker);
        _fixture.Freeze<QueryApiTransactionContextHolder>();

        object? capturedBody = null;
        var request = new HttpRequestMessage();

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, object, CancellationToken>((_, body, _) => capturedBody = body)
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiClient>>()
            .Setup(x => x.ExecuteAsync<TransactionBeginner.ResponseBody>(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<TransactionBeginner.ResponseBody>(
                new TransactionBeginner.ResponseBody
                {
                    Transaction = new TransactionBeginner.TransactionInfo { Id = "tx-1" }
                },
                new HttpResponseMessage().Headers));

        var subject = _fixture.Create<TransactionBeginner>();
        await subject.BeginAsync(TestContext.Current.CancellationToken);

        var body = capturedBody.Should().BeOfType<TransactionBeginner.RequestBody>().Subject;
        body.Bookmarks.Should().Equal("session-bookmark");
    }

    [Fact]
    public async Task BeginAsync_RequestBody_IncludesImpersonatedUser_FromSessionContext()
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
            .Setup(x => x.ExecuteAsync<TransactionBeginner.ResponseBody>(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<TransactionBeginner.ResponseBody>(
                new TransactionBeginner.ResponseBody
                {
                    Transaction = new TransactionBeginner.TransactionInfo { Id = "tx-1" }
                },
                new HttpResponseMessage().Headers));

        _fixture.Freeze<QueryApiTransactionContextHolder>();
        var subject = _fixture.Create<TransactionBeginner>();
        await subject.BeginAsync(TestContext.Current.CancellationToken);

        var body = capturedBody.Should().BeOfType<TransactionBeginner.RequestBody>().Subject;
        body.ImpersonatedUser.Should().Be("banana_bob");
    }
}
