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

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiErrorChecker : IQueryApiErrorChecker
{
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly ILogger _logger;

    public QueryApiErrorChecker(IJsonDeserializer jsonDeserializer, ILogger logger)
    {
        _jsonDeserializer = jsonDeserializer;
        _logger = logger;
    }

    public async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.StatusCode is HttpStatusCode.Accepted or HttpStatusCode.OK)
        {
            return;
        }

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var first = await TryParseFirstErrorAsync(responseText, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw ErrorExtensions.ParseServerException(
                new FailureMessage(
                    first?.Code ?? "Neo.ClientError.Security.Unauthorized",
                    first?.Message ?? response.ReasonPhrase ?? "Unauthorized"));
        }

        if (first is not null)
        {
            throw ErrorExtensions.ParseServerException(new FailureMessage(first.Code, first.Message));
        }

        var method = response.RequestMessage?.Method;
        var uri = response.RequestMessage?.RequestUri;
        var message = $"HTTP {(int)response.StatusCode} {method} {uri}: {responseText}";

        _logger.LogDebug("{message}", message);
        throw new ServiceUnavailableException(message);
    }

    private async Task<ErrorBody?> TryParseFirstErrorAsync(string responseText, CancellationToken cancellationToken)
    {
        try
        {
            var parsed = await _jsonDeserializer
                .DeserializeAsync<ErrorResponseBody>(responseText, cancellationToken)
                .ConfigureAwait(false);

            return parsed?.Errors is { Length: > 0 } e ? e[0] : null;
        }
        catch
        {
            return null;
        }
    }

    public void ThrowIfErrors(QueryApiErrorBody[]? errors)
    {
        if (errors is { Length: > 0 })
        {
            throw ErrorExtensions.ParseServerException(new FailureMessage(errors[0].Code, errors[0].Message));
        }
    }

    internal class ErrorResponseBody
    {
        public ErrorBody[]? Errors { get; init; }
    }

    internal class ErrorBody
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
