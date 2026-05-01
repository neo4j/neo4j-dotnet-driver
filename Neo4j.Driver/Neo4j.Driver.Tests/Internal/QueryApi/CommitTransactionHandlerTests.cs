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
/// Committing a transaction finalises all statements run within it and returns updated bookmarks. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-commit-transaction
/// </summary>
public class CommitTransactionHandlerTests
{
    private static readonly QueryApiTransactionContext DefaultTxContext = new("tx-55", null);

    /// <summary>
    /// Minimum chain: PostAsync("query/v2/tx/{txId}/commit") → request → SendAsync(request) → response.
    /// </summary>
    private static AutoMocker CreateChain(
        out HttpRequestMessage request,
        out HttpResponseMessage response,
        QueryApiTransactionContext? txContext = null)
    {
        txContext ??= DefaultTxContext;
        var mocker = new AutoMocker();
        var req = new HttpRequestMessage();
        var resp = new HttpResponseMessage { Content = new ByteArrayContent([]) };
        request = req;
        response = resp;

        mocker.Use(txContext);

        mocker.GetMock<IQueryApiRequestBuilder>()
            .Setup(x => x.PostAsync($"query/v2/tx/{txContext.TxId}/commit", default))
            .ReturnsAsync(req);

        mocker.GetMock<IQueryApiHttpClient>()
            .Setup(x => x.SendAsync(req, default))
            .ReturnsAsync(resp);

        return mocker;
    }

    [Fact]
    public async Task ReturnsBookmarks_FromDeserializedBody()
    {
        // Spec: the commit response contains updated bookmarks for causal consistency
        var mocker = CreateChain(out _, out _);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<CommitTransactionHandler.ResponseBody>(It.IsAny<Stream>(), default))
            .ReturnsAsync(new CommitTransactionHandler.ResponseBody
            {
                Bookmarks = ["neo4j:bookmark:v1:tx300", "neo4j:bookmark:v1:tx301"]
            });

        var bookmarks = await mocker.CreateInstance<CommitTransactionHandler>().CommitTransactionAsync();

        bookmarks.Should().Equal("neo4j:bookmark:v1:tx300", "neo4j:bookmark:v1:tx301");
    }

    [Fact]
    public async Task ReturnsEmptyBookmarks_WhenBodyHasNoBookmarks()
    {
        var mocker = CreateChain(out _, out _);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<CommitTransactionHandler.ResponseBody>(It.IsAny<Stream>(), default))
            .ReturnsAsync(new CommitTransactionHandler.ResponseBody());

        var bookmarks = await mocker.CreateInstance<CommitTransactionHandler>().CommitTransactionAsync();

        bookmarks.Should().BeEmpty();
    }

    [Fact]
    public async Task PassesResponse_ToErrorChecker()
    {
        // Chain: SendAsync(request) → response → EnsureSuccessAsync(response)
        var mocker = CreateChain(out _, out var response);
        await mocker.CreateInstance<CommitTransactionHandler>().CommitTransactionAsync();

        mocker.GetMock<IQueryApiErrorChecker>()
            .Verify(x => x.EnsureSuccessAsync(response, default), Times.Once);
    }
}
