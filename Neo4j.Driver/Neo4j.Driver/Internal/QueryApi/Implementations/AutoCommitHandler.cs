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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.QueryApi;

internal class AutoCommitHandler : IAutoCommitHandler
{
    private readonly IQueryApiErrorChecker _errorChecker;
    private readonly IQueryApiHttpClient _httpClient;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IQueryApiRequestBuilder _requestBuilder;

    public AutoCommitHandler(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiHttpClient httpClient,
        IQueryApiErrorChecker errorChecker,
        IJsonDeserializer jsonDeserializer,
        IJsonSerializer jsonSerializer)
    {
        _requestBuilder = requestBuilder;
        _httpClient = httpClient;
        _errorChecker = errorChecker;
        _jsonDeserializer = jsonDeserializer;
        _jsonSerializer = jsonSerializer;
    }

    public async Task<QueryApiResponse> AutoCommitAsync(
        Query query,
        IReadOnlyList<string> bookmarks,
        IAuthToken auth,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(query, bookmarks, auth);
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

        return new QueryApiResponse
        {
            Fields = body?.Data?.Fields ?? [],
            Rows = body?.Data?.Values ?? [],
            Bookmarks = body?.Bookmarks ?? []
        };
    }

    private HttpRequestMessage BuildRequest(
        Query query,
        IReadOnlyList<string> bookmarks,
        IAuthToken auth)
    {
        // TODO: parameters are serialised using each value's runtime type. This works for .NET
        // primitives but not for Neo4j-specific types (LocalDate, Duration, Point, INode, etc.).
        // A custom JsonConverter is needed for those types.
        var body = new RequestBody
        {
            Statement = query.Text,
            Parameters = query.Parameters.Count > 0 ? query.Parameters : null,
            Bookmarks = bookmarks.Count > 0 ? [.. bookmarks] : null
        };

        var request = _requestBuilder.Post("query/v2", auth);
        request.Content = _jsonSerializer.Serialize(body);
        return request;
    }

    private class RequestBody
    {
        public string? Statement { get; init; }
        public IDictionary<string, object>? Parameters { get; init; }
        public string[]? Bookmarks { get; init; }
    }

    private class ResponseBody
    {
        public DataBody? Data { get; init; }
        public string[]? Bookmarks { get; init; }
        public ErrorBody[]? Errors { get; init; }
    }

    private class DataBody
    {
        public string[] Fields { get; } = [];
        public JsonElement[][]? Values { get; init; }
    }

    private class ErrorBody
    {
        public string Code { get; } = string.Empty;
        public string Message { get; } = string.Empty;
    }
}
