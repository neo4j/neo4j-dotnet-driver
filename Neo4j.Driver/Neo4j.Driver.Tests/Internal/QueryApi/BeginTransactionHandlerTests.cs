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
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Beginning a transaction reserves a server-side transaction and returns its ID, plus the optional
/// <c>neo4j-cluster-affinity</c> header that must be forwarded on all subsequent requests. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-begin-transaction
/// </summary>
public class BeginTransactionHandlerTests
{
    /// <summary>
    /// Minimum chain: PostAsync("query/v2/tx") → request → SendAsync(request) → response.
    /// Returns a mocker with the standard success body (transaction ID "tx-1") already set up on the deserializer.
    /// </summary>
    private static AutoMocker CreateChain(
        out HttpRequestMessage request,
        out HttpResponseMessage response,
        string txId = "tx-1")
    {
        var mocker = new AutoMocker();
        var req = new HttpRequestMessage();
        var resp = new HttpResponseMessage { Content = new ByteArrayContent([]) };
        request = req;
        response = resp;

        mocker.GetMock<IQueryApiRequestBuilder>()
            .Setup(x => x.PostAsync("query/v2/tx", default))
            .ReturnsAsync(req);

        mocker.GetMock<IQueryApiHttpClient>()
            .Setup(x => x.SendAsync(req, default))
            .ReturnsAsync(resp);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<BeginTransactionHandler.ResponseBody>(It.IsAny<Stream>(), default))
            .ReturnsAsync(new BeginTransactionHandler.ResponseBody
            {
                Transaction = new BeginTransactionHandler.TransactionInfo { Id = txId }
            });

        return mocker;
    }

    [Fact]
    public async Task ReturnsTransactionId_FromDeserializedBody()
    {
        // Spec: response body contains transaction.id — the handle for subsequent requests
        var mocker = CreateChain(out _, out _, txId: "tx-abc-123");

        var context = await mocker.CreateInstance<BeginTransactionHandler>().BeginTransactionAsync([]);

        context.TxId.Should().Be("tx-abc-123");
    }

    [Fact]
    public async Task ReturnsClusterAffinity_WhenResponseCarriesAffinityHeader()
    {
        // Spec: Aura instances return neo4j-cluster-affinity on BEGIN — it must be echoed back on subsequent requests.
        // Chain: SendAsync(request) → response → Extract(response) → "shard-99"
        var mocker = CreateChain(out _, out var response);

        mocker.GetMock<IClusterAffinityApplicator>()
            .Setup(x => x.Extract(response))
            .Returns("shard-99");

        var context = await mocker.CreateInstance<BeginTransactionHandler>().BeginTransactionAsync([]);

        context.ClusterAffinity.Should().Be("shard-99");
    }

    [Fact]
    public async Task ReturnsNullClusterAffinity_WhenResponseHasNoAffinityHeader()
    {
        var mocker = CreateChain(out _, out var response);

        mocker.GetMock<IClusterAffinityApplicator>()
            .Setup(x => x.Extract(response))
            .Returns((string?)null);

        var context = await mocker.CreateInstance<BeginTransactionHandler>().BeginTransactionAsync([]);

        context.ClusterAffinity.Should().BeNull();
    }

    [Fact]
    public async Task Serializes_Bookmarks_WhenProvided()
    {
        // Spec: bookmarks enable causal consistency at transaction start
        var bookmarks = new List<string> { "neo4j:bookmark:v1:tx50" };
        var mocker = CreateChain(out _, out _);
        BeginTransactionHandler.RequestBody? capturedBody = null;
        mocker.GetMock<IJsonSerializer>()
            .Setup(x => x.Serialize(It.IsAny<BeginTransactionHandler.RequestBody>()))
            .Callback<BeginTransactionHandler.RequestBody>(b => capturedBody = b);

        await mocker.CreateInstance<BeginTransactionHandler>().BeginTransactionAsync(bookmarks);

        capturedBody.Should().NotBeNull();
        capturedBody!.Bookmarks.Should().Equal("neo4j:bookmark:v1:tx50");
    }

    [Fact]
    public async Task Omits_Bookmarks_WhenListIsEmpty()
    {
        var mocker = CreateChain(out _, out _);
        BeginTransactionHandler.RequestBody? capturedBody = null;
        mocker.GetMock<IJsonSerializer>()
            .Setup(x => x.Serialize(It.IsAny<BeginTransactionHandler.RequestBody>()))
            .Callback<BeginTransactionHandler.RequestBody>(b => capturedBody = b);

        await mocker.CreateInstance<BeginTransactionHandler>().BeginTransactionAsync([]);

        capturedBody.Should().NotBeNull();
        capturedBody!.Bookmarks.Should().BeNull();
    }

    [Fact]
    public async Task Throws_WhenDeserializedBodyHasNoTransactionId()
    {
        // A missing transaction ID means something went wrong server-side
        var mocker = CreateChain(out _, out _);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<BeginTransactionHandler.ResponseBody>(It.IsAny<Stream>(), default))
            .ReturnsAsync(new BeginTransactionHandler.ResponseBody
            {
                Transaction = new BeginTransactionHandler.TransactionInfo { Id = null }
            });

        var act = () => mocker.CreateInstance<BeginTransactionHandler>().BeginTransactionAsync([]);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*transaction ID*");
    }
}
