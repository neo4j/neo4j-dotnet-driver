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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
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

    private HttpResponseMessage SetupChain(string txId = "tx-1")
    {
        var response = new HttpResponseMessage { Content = new ByteArrayContent([]) };

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpRequestMessage());

        _fixture.Freeze<Mock<IQueryApiHttpClient>>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _fixture.Freeze<Mock<IJsonDeserializer>>()
            .Setup(x => x.DeserializeAsync<BeginTransactionHandler.ResponseBody>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BeginTransactionHandler.ResponseBody
            {
                Transaction = new BeginTransactionHandler.TransactionInfo { Id = txId }
            });

        return response;
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
        // Spec: Aura instances return neo4j-cluster-affinity on BEGIN — it must be echoed back on subsequent requests.
        var response = SetupChain();

        _fixture.Freeze<Mock<IClusterAffinityExtractor>>()
            .Setup(x => x.Extract(response)).Returns("shard-99");

        var subject = _fixture.Create<BeginTransactionHandler>();
        var context = await subject.BeginTransactionAsync([], TestContext.Current.CancellationToken);

        context.ClusterAffinity.Should().Be("shard-99");
    }

    [Fact]
    public async Task ReturnsNullClusterAffinity_WhenResponseHasNoAffinityHeader()
    {
        var response = SetupChain();

        _fixture.Freeze<Mock<IClusterAffinityExtractor>>()
            .Setup(x => x.Extract(response)).Returns((string?)null);

        var subject = _fixture.Create<BeginTransactionHandler>();
        var context = await subject.BeginTransactionAsync([], TestContext.Current.CancellationToken);

        context.ClusterAffinity.Should().BeNull();
    }

    [Fact]
    public async Task Throws_WhenDeserializedBodyHasNoTransactionId()
    {
        // A missing transaction ID means something went wrong server-side
        SetupChain();

        _fixture.Freeze<Mock<IJsonDeserializer>>()
            .Setup(x => x.DeserializeAsync<BeginTransactionHandler.ResponseBody>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BeginTransactionHandler.ResponseBody
            {
                Transaction = new BeginTransactionHandler.TransactionInfo { Id = null }
            });

        var subject = _fixture.Create<BeginTransactionHandler>();
        var act = () => subject.BeginTransactionAsync([], TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*transaction ID*");
    }
}
