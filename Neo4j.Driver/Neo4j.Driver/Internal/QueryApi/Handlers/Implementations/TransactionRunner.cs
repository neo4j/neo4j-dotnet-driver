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

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class TransactionRunner : ITransactionRunner
{
    private readonly IQueryApiClient _client;
    private readonly IQueryApiResultCursorBuilder _cursorBuilder;
    private readonly ILogger _logger;
    private readonly IQueryApiRequestBuilder _requestBuilder;
    private readonly IQueryApiTransactionContextTracker _txContextTracker;

    public TransactionRunner(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiClient client,
        IQueryApiResultCursorBuilder cursorBuilder,
        IQueryApiTransactionContextTracker txContextTracker,
        ILogger logger)
    {
        _requestBuilder = requestBuilder;
        _client = client;
        _cursorBuilder = cursorBuilder;
        _txContextTracker = txContextTracker;
        _logger = logger;
    }

    public async Task<IResultCursor> RunAsync(Query query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Running query in tx {txId}: {query}", _txContextTracker.Context!.TxId, query.Text);

        using var request = await BuildRequestAsync(query, cancellationToken).ConfigureAwait(false);
        var result = await _client.ExecuteAsync<QueryApiResultBody>(request, cancellationToken).ConfigureAwait(false);

        var body = result.Body;
        var resultSet = new QueryApiResultSet
        {
            Fields = body.Data?.Fields ?? [],
            Rows = body.Data?.Values ?? [],
            Bookmarks = body.Bookmarks ?? []
        };

        _logger.LogDebug(
            "Run complete: {fieldCount} field(s), {rowCount} row(s)",
            resultSet.Fields.Length,
            resultSet.Rows.Length);

        return _cursorBuilder.Build(resultSet, query);
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(Query query, CancellationToken cancellationToken)
    {
        var body = new RequestBody
        {
            Statement = query.Text,
            Parameters = query.Parameters.Count > 0 ? new QueryApiParameterDictionary(query.Parameters) : null
        };

        var request = await _requestBuilder
            .PostAsync($"query/v2/tx/{_txContextTracker.Context!.TxId}", body, cancellationToken)
            .ConfigureAwait(false);

        return request;
    }

    internal record RequestBody : IQueryApiRequestBody
    {
        public string? Statement { get; init; }
        public QueryApiParameterDictionary? Parameters { get; init; }

        public IReadOnlyCollection<object?> GetParameterValues()
        {
            return Parameters is null ? [] : [.. Parameters.Values];
        }
    }
}
