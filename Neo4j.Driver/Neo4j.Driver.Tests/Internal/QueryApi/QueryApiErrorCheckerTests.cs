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
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// The Query API returns 202 Accepted on success and 401 Unauthorized on authentication failure.
/// Application-level errors (e.g. syntax errors) appear in the response body at 202. Spec:
/// https://neo4j.com/docs/query-api/current/#_response_status_codes
/// </summary>
public class QueryApiErrorCheckerTests
{
    private static QueryApiErrorChecker Checker => new(new QueryApiJsonSerializer(), new Mock<ILogger>().Object);

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, object body)
    {
        return new HttpResponseMessage(status) { Content = new QueryApiJsonSerializer().Serialize(body) };
    }

    public class EnsureSuccessAsync
    {
        [Fact]
        public async Task DoesNotThrow_WhenStatusIs202Accepted()
        {
            // 202 Accepted is the only success code for the Query API
            var response = new HttpResponseMessage(HttpStatusCode.Accepted);

            var act = () => Checker.EnsureSuccessAsync(response);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ThrowsAuthenticationException_WhenStatusIs401_AndBodyContainsErrorCode()
        {
            // 401 with a structured error body: Neo.ClientError.Security.Unauthorized maps to AuthenticationException
            // Spec: https://neo4j.com/docs/query-api/current/#_authentication_errors
            var response = JsonResponse(
                HttpStatusCode.Unauthorized,
                new
                {
                    errors = new[]
                    {
                        new
                        {
                            code = "Neo.ClientError.Security.Unauthorized",
                            message = "No authentication was supplied."
                        }
                    }
                });

            var act = () => Checker.EnsureSuccessAsync(response);

            await act.Should()
                .ThrowAsync<AuthenticationException>()
                .WithMessage("*No authentication was supplied.*");
        }

        [Fact]
        public async Task ThrowsAuthenticationException_WhenStatusIs401_AndBodyIsUnparseable()
        {
            // When the 401 body cannot be parsed, we fall back to a generic Unauthorized error
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("not json", Encoding.UTF8, "text/plain"),
                ReasonPhrase = "Unauthorized"
            };

            var act = () => Checker.EnsureSuccessAsync(response);

            await act.Should().ThrowAsync<AuthenticationException>();
        }

        [Fact]
        public async Task ThrowsServiceUnavailableException_WhenStatusIsUnexpected()
        {
            // Any status other than 202 or 401 is treated as a service-level failure
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

            var act = () => Checker.EnsureSuccessAsync(response);

            await act.Should()
                .ThrowAsync<ServiceUnavailableException>()
                .WithMessage("*503*");
        }

        [Fact]
        public async Task ThrowsServiceUnavailableException_WithStatusCode_WhenServerReturnsUnexpected5xx()
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            var act = () => Checker.EnsureSuccessAsync(response);

            await act.Should()
                .ThrowAsync<ServiceUnavailableException>()
                .WithMessage("*500*");
        }
    }

    public class ThrowIfAnyError
    {
        [Fact]
        public void ThrowsClientException_ForSyntaxError()
        {
            // Application-level errors from the response body are mapped to the correct exception type
            // Spec: https://neo4j.com/docs/query-api/current/#_errors
            var act = () => Checker.ThrowIfAnyError(
                "Neo.ClientError.Statement.SyntaxError",
                "Invalid input 'RETUN': expected 'RETURN'");

            act.Should()
                .Throw<ClientException>()
                .WithMessage("*Invalid input*");
        }

        [Fact]
        public void ThrowsTransientException_ForTransientDatabaseUnavailable()
        {
            var act = () => Checker.ThrowIfAnyError(
                "Neo.TransientError.General.DatabaseUnavailable",
                "Database is temporarily unavailable.");

            act.Should().Throw<TransientException>();
        }
    }
}
