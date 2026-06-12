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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class TransactionBeginner : ITransactionBeginner
{
    private readonly IClusterAffinityExtractor _affinityExtractor;
    private readonly IBookmarkTracker _bookmarkTracker;
    private readonly IQueryApiClient _client;
    private readonly IQueryApiTransactionContextTracker _contextTracker;
    private readonly ILogger _logger;
    private readonly IQueryApiRequestBuilder _requestBuilder;
    private readonly ISessionContext _sessionContext;

    public TransactionBeginner(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiClient client,
        IClusterAffinityExtractor affinityExtractor,
        ISessionContext sessionContext,
        IBookmarkTracker bookmarkTracker,
        IQueryApiTransactionContextTracker contextTracker,
        ILogger logger)
    {
        _requestBuilder = requestBuilder;
        _client = client;
        _affinityExtractor = affinityExtractor;
        _sessionContext = sessionContext;
        _bookmarkTracker = bookmarkTracker;
        _contextTracker = contextTracker;
        _logger = logger;
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        var bookmarks = _bookmarkTracker.CurrentBookmarks.Values;
        _logger.LogDebug("Beginning transaction with {bookmarkCount} bookmark(s)", bookmarks.Length);

        using var request = await BuildRequestAsync(bookmarks, cancellationToken).ConfigureAwait(false);
        var result = await _client.ExecuteAsync<ResponseBody>(request, cancellationToken).ConfigureAwait(false);

        if (result.Body.Transaction?.Id is not {} txId)
        {
            throw new InvalidOperationException("Server did not return a transaction ID.");
        }

        var context = new QueryApiTransactionContext(txId, _affinityExtractor.Extract(result.ResponseHeaders));
        _contextTracker.Set(context);
        _logger.LogDebug("Transaction begun");
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(
        System.Collections.Generic.IReadOnlyList<string> bookmarks,
        CancellationToken cancellationToken)
    {
        var body = new RequestBody(bookmarks.ToArray(), _sessionContext.ImpersonatedUser);
        var request = await _requestBuilder.PostAsync("query/v2/tx", body, cancellationToken).ConfigureAwait(false);
        return request;
    }

    internal record RequestBody(string[]? Bookmarks, string? ImpersonatedUser);

    internal record ResponseBody : QueryApiResponse
    {
        public TransactionInfo? Transaction { get; init; }
    }

    internal record TransactionInfo(string? Id = null);
}
