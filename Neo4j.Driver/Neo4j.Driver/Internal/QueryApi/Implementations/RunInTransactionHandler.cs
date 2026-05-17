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
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using ILogger = Neo4j.Driver.Internal.QueryApi.Abstractions.ILogger;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class RunInTransactionHandler : IRunInTransactionHandler
{
    private readonly IQueryApiErrorChecker _errorChecker;
    private readonly IQueryApiHttpClient _httpClient;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILogger _logger;
    private readonly IQueryApiRequestBuilder _requestBuilder;
    private readonly QueryApiTransactionContext _txContext;

    public RunInTransactionHandler(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiHttpClient httpClient,
        IQueryApiErrorChecker errorChecker,
        IJsonDeserializer jsonDeserializer,
        IJsonSerializer jsonSerializer,
        QueryApiTransactionContext txContext,
        ILogger logger)
    {
        _requestBuilder = requestBuilder;
        _httpClient = httpClient;
        _errorChecker = errorChecker;
        _jsonDeserializer = jsonDeserializer;
        _jsonSerializer = jsonSerializer;
        _txContext = txContext;
        _logger = logger;
    }

    public async Task<QueryApiResultSet> RunInTransactionAsync(
        Query query,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Running in transaction {txId}: {query}", _txContext.TxId, query.Text);

        using var request = await BuildRequestAsync(query, cancellationToken).ConfigureAwait(false);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await _errorChecker.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var body = await _jsonDeserializer
            .DeserializeAsync<QueryApiResultBody>(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);

        if (body?.Errors is { Length: > 0 } errors)
        {
            _errorChecker.ThrowIfAnyError(errors[0].Code, errors[0].Message);
        }

        var result = new QueryApiResultSet
        {
            Fields = body?.Data?.Fields ?? [],
            Rows = body?.Data?.Values ?? [],
            Bookmarks = body?.Bookmarks ?? []
        };

        _logger.Debug("Run complete in transaction {txId}: {fieldCount} field(s), {rowCount} row(s)", _txContext.TxId, result.Fields.Length, result.Rows.Length);

        return result;
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(Query query, CancellationToken cancellationToken)
    {
        var body = new RequestBody
        {
            Statement = query.Text,
            Parameters = query.Parameters.Count > 0 ? query.Parameters : null
        };

        var request = await _requestBuilder.PostAsync($"query/v2/tx/{_txContext.TxId}", cancellationToken).ConfigureAwait(false);
        request.Content = _jsonSerializer.Serialize(body);
        return request;
    }

    internal record RequestBody
    {
        public string? Statement { get; init; }
        public IDictionary<string, object>? Parameters { get; init; }
    }
}
