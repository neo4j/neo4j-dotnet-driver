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

    public QueryApiErrorChecker(IJsonDeserializer jsonDeserializer)
    {
        _jsonDeserializer = jsonDeserializer;
    }

    public async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            ErrorResponseBody? parsed = null;
            try
            {
                parsed = await _jsonDeserializer
                    .DeserializeAsync<ErrorResponseBody>(
                        await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // If the body cannot be parsed, fall through to a generic auth exception.
            }

            var first = parsed?.Errors is { Length: > 0 } e ? e[0] : null;
            throw ErrorExtensions.ParseServerException(
                new FailureMessage(
                    first?.Code ?? "Neo.ClientError.Security.Unauthorized",
                    first?.Message ?? response.ReasonPhrase ?? "Unauthorized"));
        }

        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            throw new ServiceUnavailableException($"Unexpected HTTP {(int)response.StatusCode} from the Query API.");
        }
    }

    public void ThrowIfAnyError(string code, string message)
    {
        throw ErrorExtensions.ParseServerException(new FailureMessage(code, message));
    }

    private class ErrorResponseBody
    {
        public ErrorBody[]? Errors { get; init; }
    }

    private class ErrorBody
    {
        public string Code { get; } = string.Empty;
        public string Message { get; } = string.Empty;
    }
}
