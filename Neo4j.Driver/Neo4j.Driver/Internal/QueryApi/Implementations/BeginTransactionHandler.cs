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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class BeginTransactionHandler : IBeginTransactionHandler
{
    private readonly IClusterAffinityExtractor _affinityExtractor;
    private readonly IQueryApiErrorChecker _errorChecker;
    private readonly IQueryApiHttpClient _httpClient;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly ILogger _logger;
    private readonly IQueryApiRequestBuilder _requestBuilder;

    public BeginTransactionHandler(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiHttpClient httpClient,
        IQueryApiErrorChecker errorChecker,
        IJsonDeserializer jsonDeserializer,
        IClusterAffinityExtractor affinityExtractor,
        ILogger logger)
    {
        _requestBuilder = requestBuilder;
        _httpClient = httpClient;
        _errorChecker = errorChecker;
        _jsonDeserializer = jsonDeserializer;
        _affinityExtractor = affinityExtractor;
        _logger = logger;
    }

    public async Task<QueryApiTransactionContext> BeginTransactionAsync(
        IReadOnlyList<string> bookmarks,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Beginning transaction with {bookmarkCount} bookmark(s)", bookmarks.Count);

        using var request = await BuildRequestAsync(bookmarks, cancellationToken).ConfigureAwait(false);
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

        if (body?.Transaction?.Id is not {} txId)
        {
            throw new InvalidOperationException("Server did not return a transaction ID.");
        }

        var context = new QueryApiTransactionContext(txId, _affinityExtractor.Extract(response));
        _logger.Debug("Transaction begun: {txId}", txId);
        return context;
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(IReadOnlyList<string> bookmarks, CancellationToken cancellationToken)
    {
        var body = new RequestBody
        {
            Bookmarks = bookmarks.Count > 0 ? [.. bookmarks] : null
        };

        var request = await _requestBuilder.PostAsync("query/v2/tx", body, cancellationToken).ConfigureAwait(false);
        return request;
    }

    internal record RequestBody(string[]? Bookmarks = null);

    internal record ResponseBody
    {
        public TransactionInfo? Transaction { get; init; }
        public QueryApiErrorBody[]? Errors { get; init; }
    }

    internal record TransactionInfo(string? Id = null);
}
