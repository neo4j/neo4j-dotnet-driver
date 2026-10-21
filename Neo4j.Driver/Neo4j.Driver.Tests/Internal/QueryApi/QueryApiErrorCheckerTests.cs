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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// The Query API returns 202 Accepted on success and 401 Unauthorized on authentication failure.
/// Application-level errors (e.g. syntax errors) appear in the response body at 202. Spec:
/// https://neo4j.com/docs/query-api/current/#_response_status_codes
/// </summary>
public class QueryApiErrorCheckerTests
{
    private static QueryApiErrorChecker BuildChecker(IJsonDeserializer? deserializer = null) =>
        new(deserializer ?? new Mock<IJsonDeserializer>().Object, new Mock<ILogger>().Object);

    private static HttpResponseMessage StringResponse(HttpStatusCode status, string body = "") =>
        new(status) { Content = new StringContent(body) };

    public class EnsureSuccessAsync
    {
        [Fact]
        public async Task DoesNotThrow_WhenStatusIs202Accepted()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted);
            await BuildChecker().Invoking(x => x.EnsureSuccessAsync(response)).Should().NotThrowAsync();
        }

        [Fact]
        public async Task DoesNotThrow_WhenStatusIs200Ok()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            await BuildChecker().Invoking(x => x.EnsureSuccessAsync(response)).Should().NotThrowAsync();
        }

        [Fact]
        public async Task ThrowsAuthenticationException_WhenStatusIs401_AndBodyContainsErrorCode()
        {
            const string responseBody = "test-response-body";
            var errorBody = new QueryApiErrorChecker.ErrorResponseBody
            {
                Errors =
                [
                    new QueryApiErrorChecker.ErrorBody
                    {
                        Code = "Neo.ClientError.Security.Unauthorized",
                        Message = "No authentication was supplied."
                    }
                ]
            };

            var mockDeserializer = new Mock<IJsonDeserializer>();
            mockDeserializer
                .Setup(x => x.DeserializeAsync<QueryApiErrorChecker.ErrorResponseBody>(
                    responseBody, It.IsAny<CancellationToken>()))
                .ReturnsAsync(errorBody);

            var response = StringResponse(HttpStatusCode.Unauthorized, responseBody);

            await BuildChecker(mockDeserializer.Object)
                .Invoking(x => x.EnsureSuccessAsync(response))
                .Should().ThrowAsync<AuthenticationException>()
                .WithMessage("*No authentication was supplied.*");
        }

        [Fact]
        public async Task ThrowsAuthenticationException_WhenStatusIs401_AndBodyIsUnparseable()
        {
            const string responseBody = "not json";

            var mockDeserializer = new Mock<IJsonDeserializer>();
            mockDeserializer
                .Setup(x => x.DeserializeAsync<QueryApiErrorChecker.ErrorResponseBody>(
                    responseBody, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("Invalid JSON"));

            var response = StringResponse(HttpStatusCode.Unauthorized, responseBody);
            response.ReasonPhrase = "Unauthorized";

            await BuildChecker(mockDeserializer.Object)
                .Invoking(x => x.EnsureSuccessAsync(response))
                .Should().ThrowAsync<AuthenticationException>();
        }

        [Fact]
        public async Task ThrowsClientException_WhenStatusIs400_AndBodyContainsErrorCode()
        {
            const string responseBody = "test-response-body";
            var errorBody = new QueryApiErrorChecker.ErrorResponseBody
            {
                Errors =
                [
                    new QueryApiErrorChecker.ErrorBody
                    {
                        Code = "Neo.ClientError.Statement.SyntaxError",
                        Message = "Invalid input 'Invalid': expected ..."
                    }
                ]
            };

            var mockDeserializer = new Mock<IJsonDeserializer>();
            mockDeserializer
                .Setup(x => x.DeserializeAsync<QueryApiErrorChecker.ErrorResponseBody>(
                    responseBody, It.IsAny<CancellationToken>()))
                .ReturnsAsync(errorBody);

            var response = StringResponse(HttpStatusCode.BadRequest, responseBody);

            await BuildChecker(mockDeserializer.Object)
                .Invoking(x => x.EnsureSuccessAsync(response))
                .Should().ThrowAsync<ClientException>()
                .WithMessage("*Invalid input*");
        }

        [Fact]
        public async Task ThrowsServiceUnavailableException_WhenStatusIsUnexpected()
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

            await BuildChecker()
                .Invoking(x => x.EnsureSuccessAsync(response))
                .Should().ThrowAsync<ServiceUnavailableException>()
                .WithMessage("*503*");
        }

        [Fact]
        public async Task ThrowsServiceUnavailableException_WithStatusCode_WhenServerReturnsUnexpected5xx()
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            await BuildChecker()
                .Invoking(x => x.EnsureSuccessAsync(response))
                .Should().ThrowAsync<ServiceUnavailableException>()
                .WithMessage("*500*");
        }
    }

    public class ThrowIfErrors
    {
        [Fact]
        public void ThrowsClientException_ForSyntaxError()
        {
            var act = () => BuildChecker().ThrowIfErrors(
                [new QueryApiErrorBody("Neo.ClientError.Statement.SyntaxError", "Invalid input 'RETUN': expected 'RETURN'")]);

            act.Should().Throw<ClientException>().WithMessage("*Invalid input*");
        }

        [Fact]
        public void ThrowsTransientException_ForTransientDatabaseUnavailable()
        {
            var act = () => BuildChecker().ThrowIfErrors(
                [new QueryApiErrorBody("Neo.TransientError.General.DatabaseUnavailable", "Database is temporarily unavailable.")]);

            act.Should().Throw<TransientException>();
        }

        [Fact]
        public void DoesNotThrow_WhenErrorsIsNull()
        {
            BuildChecker().Invoking(x => x.ThrowIfErrors(null)).Should().NotThrow();
        }

        [Fact]
        public void DoesNotThrow_WhenErrorsIsEmpty()
        {
            BuildChecker().Invoking(x => x.ThrowIfErrors([])).Should().NotThrow();
        }
    }
}
