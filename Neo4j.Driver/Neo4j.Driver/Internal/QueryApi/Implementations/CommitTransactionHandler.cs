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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.QueryApi;

internal class CommitTransactionHandler : ICommitTransactionHandler
{
    private readonly IQueryApiErrorChecker _errorChecker;
    private readonly IQueryApiHttpClient _httpClient;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IQueryApiRequestBuilder _requestBuilder;

    public CommitTransactionHandler(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiHttpClient httpClient,
        IQueryApiErrorChecker errorChecker,
        IJsonDeserializer jsonDeserializer)
    {
        _requestBuilder = requestBuilder;
        _httpClient = httpClient;
        _errorChecker = errorChecker;
        _jsonDeserializer = jsonDeserializer;
    }

    public async Task<string[]> CommitTransactionAsync(
        string database,
        QueryApiTransactionContext txContext,
        IAuthToken auth,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(database, txContext, auth);
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

        return body?.Bookmarks ?? [];
    }

    private HttpRequestMessage BuildRequest(string database, QueryApiTransactionContext txContext, IAuthToken auth)
    {
        return _requestBuilder.Post($"db/{database}/query/v2/tx/{txContext.TxId}/commit", auth, txContext);
    }

    private class ResponseBody
    {
        public string[]? Bookmarks { get; init; }
        public ErrorBody[]? Errors { get; init; }
    }

    private class ErrorBody
    {
        public string Code { get; } = string.Empty;
        public string Message { get; } = string.Empty;
    }
}
