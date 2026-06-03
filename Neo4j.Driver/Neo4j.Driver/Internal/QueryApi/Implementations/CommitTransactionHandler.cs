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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class CommitTransactionHandler : ICommitTransactionHandler
{
    private readonly IQueryApiErrorChecker _errorChecker;
    private readonly IQueryApiHttpClient _httpClient;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly ILogger _logger;
    private readonly IQueryApiRequestBuilder _requestBuilder;
    private readonly QueryApiTransactionContext _txContext;

    public CommitTransactionHandler(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiHttpClient httpClient,
        IQueryApiErrorChecker errorChecker,
        IJsonDeserializer jsonDeserializer,
        QueryApiTransactionContext txContext,
        ILogger logger)
    {
        _requestBuilder = requestBuilder;
        _httpClient = httpClient;
        _errorChecker = errorChecker;
        _jsonDeserializer = jsonDeserializer;
        _txContext = txContext;
        _logger = logger;
    }

    public async Task<string[]> CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Building transaction commit request", _txContext.TxId);
        using var request = await _requestBuilder
            .PostAsync($"query/v2/tx/{_txContext.TxId}/commit", null, cancellationToken)
            .ConfigureAwait(false);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await _errorChecker.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var body = await _jsonDeserializer
            .DeserializeAsync<ResponseBody>(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);

        if (body?.Errors is { Length: > 0 } errors)
        {
            _errorChecker.ThrowIfAnyError(errors[0].Code, errors[0].Message);
        }

        var bookmarks = body?.Bookmarks ?? [];
        _logger.Debug("Committed transaction" + (bookmarks.Length == 0 ? "" : " and got bookmarks"));
        return bookmarks;
    }

    internal record ResponseBody
    {
        public string[]? Bookmarks { get; init; }
        public QueryApiErrorBody[]? Errors { get; init; }
    }
}
