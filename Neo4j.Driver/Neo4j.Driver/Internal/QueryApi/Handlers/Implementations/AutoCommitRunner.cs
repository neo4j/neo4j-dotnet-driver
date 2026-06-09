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
internal class AutoCommitRunner : IAutoCommitRunner
{
    private readonly IBookmarkTracker _bookmarkTracker;
    private readonly IQueryApiClient _client;
    private readonly IQueryApiResultCursorBuilder _cursorBuilder;
    private readonly ILogger _logger;
    private readonly IQueryApiRequestBuilder _requestBuilder;
    private readonly ISessionContext _sessionContext;

    public AutoCommitRunner(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiClient client,
        IQueryApiResultCursorBuilder cursorBuilder,
        ISessionContext sessionContext,
        IBookmarkTracker bookmarkTracker,
        ILogger logger)
    {
        _requestBuilder = requestBuilder;
        _client = client;
        _cursorBuilder = cursorBuilder;
        _sessionContext = sessionContext;
        _bookmarkTracker = bookmarkTracker;
        _logger = logger;
    }

    public async Task<IResultCursor> RunAsync(Query query, CancellationToken cancellationToken = default)
    {
        _logger.Debug("Auto-commit: {query}", query.Text);

        var bookmarks = _bookmarkTracker.CurrentBookmarks.Values;
        using var request = await BuildRequestAsync(query, bookmarks, cancellationToken).ConfigureAwait(false);
        var result = await _client.ExecuteAsync<QueryApiResultBody>(request, cancellationToken).ConfigureAwait(false);

        var body = result.Body;
        var resultSet = new QueryApiResultSet
        {
            Fields = body?.Data?.Fields ?? [],
            Rows = body?.Data?.Values ?? [],
            Bookmarks = body?.Bookmarks ?? []
        };

        _bookmarkTracker.UpdateBookmarks(resultSet.Bookmarks);

        _logger.Debug(
            "Auto-commit complete: {fieldCount} field(s), {rowCount} row(s)",
            resultSet.Fields.Length,
            resultSet.Rows.Length);

        return _cursorBuilder.Build(resultSet, query);
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(
        Query query,
        IReadOnlyList<string> bookmarks,
        CancellationToken cancellationToken)
    {
        // TODO: parameters are serialised using each value's runtime type. This works for .NET
        // primitives but not for Neo4j-specific types (LocalDate, Duration, Point, INode, etc.).
        // A custom JsonConverter is needed for those types.
        var body = new RequestBody
        {
            Statement = query.Text,
            Parameters = query.Parameters.Count > 0 ? query.Parameters : null,
            Bookmarks = bookmarks.Count > 0 ? [.. bookmarks] : null,
            ImpersonatedUser = _sessionContext.ImpersonatedUser,
            AccessMode = _sessionContext.AccessMode == AccessMode.Read ? "Read" : "Write"
        };

        var request = await _requestBuilder.PostAsync("query/v2", body, cancellationToken).ConfigureAwait(false);
        return request;
    }

    internal record RequestBody
    {
        public string? Statement { get; init; }
        public IDictionary<string, object>? Parameters { get; init; }
        public string[]? Bookmarks { get; init; }
        public string? ImpersonatedUser { get; init; }
        public string? AccessMode { get; init; }
    }
}
