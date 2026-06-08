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

using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Types;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class CommitTransactionHandler : ICommitTransactionHandler
{
    private readonly IQueryApiClient _client;
    private readonly ILogger _logger;
    private readonly IQueryApiRequestBuilder _requestBuilder;
    private readonly QueryApiTransactionContext _txContext;

    public CommitTransactionHandler(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiClient client,
        QueryApiTransactionContext txContext,
        ILogger logger)
    {
        _requestBuilder = requestBuilder;
        _client = client;
        _txContext = txContext;
        _logger = logger;
    }

    public async Task<string[]> CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        _logger.Debug("Building transaction commit request", _txContext.TxId);
        using var request = await _requestBuilder
            .PostAsync($"query/v2/tx/{_txContext.TxId}/commit", null, cancellationToken)
            .ConfigureAwait(false);

        var result = await _client.ExecuteAsync<ResponseBody>(request, cancellationToken).ConfigureAwait(false);

        var bookmarks = result.Body?.Bookmarks ?? [];
        var len = bookmarks.Length;
        _logger.Debug("Committed transaction" + (len == 0 ? "" : $" and got {len} bookmarks"));
        return bookmarks;
    }

    internal record ResponseBody : QueryApiResponse
    {
        public string[]? Bookmarks { get; init; }
    }
}
