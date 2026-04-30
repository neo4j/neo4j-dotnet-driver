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

using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Rolling back a transaction discards all statements run within it. A DELETE request is used — the only Query
/// API operation that is not a POST. Spec: https://neo4j.com/docs/query-api/current/#query-api-rollback-transaction
/// </summary>
public class RollbackTransactionHandlerTests
{
    private static readonly QueryApiTransactionContext DefaultTxContext = new("tx-77", null);

    /// <summary>
    /// Minimum chain: DeleteAsync("query/v2/tx/{txId}") → request → SendAsync(request) → response.
    /// </summary>
    private static AutoMocker CreateChain(
        out HttpRequestMessage request,
        out HttpResponseMessage response,
        QueryApiTransactionContext? txContext = null)
    {
        txContext ??= DefaultTxContext;
        var mocker = new AutoMocker();
        var req = new HttpRequestMessage();
        var resp = new HttpResponseMessage();
        request = req;
        response = resp;

        mocker.Use(txContext);

        mocker.GetMock<IQueryApiRequestBuilder>()
            .Setup(x => x.DeleteAsync($"query/v2/tx/{txContext.TxId}", default))
            .ReturnsAsync(req);

        mocker.GetMock<IQueryApiHttpClient>()
            .Setup(x => x.SendAsync(req, default))
            .ReturnsAsync(resp);

        return mocker;
    }

    [Fact]
    public async Task PassesResponse_ToErrorChecker()
    {
        // Chain: SendAsync(request) → response → EnsureSuccessAsync(response)
        var mocker = CreateChain(out _, out var response);
        await mocker.CreateInstance<RollbackTransactionHandler>().RollbackTransactionAsync();

        mocker.GetMock<IQueryApiErrorChecker>()
            .Verify(x => x.EnsureSuccessAsync(response, default), Times.Once);
    }
}
