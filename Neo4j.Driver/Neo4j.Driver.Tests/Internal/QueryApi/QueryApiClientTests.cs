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
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Tests.Internal.Core;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiClientTests
{
    private readonly AutoMocker _autoMocker = AutoMockerExtensions.ForTesting<QueryApiClient>();

    private sealed record TestBody : QueryApiResponse
    {
        public string? Value { get; init; }
    }

    [Fact]
    public async Task ReturnsDeserializedBody_InResult()
    {
        var expected = new TestBody { Value = "hello" };

        _autoMocker.GetMock<IQueryApiHttpTransport>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new ByteArrayContent([]) });

        _autoMocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<TestBody>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var subject = _autoMocker.CreateInstance<QueryApiClient>();
        var result = await subject.ExecuteAsync<TestBody>(
            new HttpRequestMessage(),
            TestContext.Current.CancellationToken);

        result.Body.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task CapturesResponseHeaders_InResult()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new ByteArrayContent([]) };
        response.Headers.TryAddWithoutValidation("neo4j-cluster-affinity", "shard-42");

        _autoMocker.GetMock<IQueryApiHttpTransport>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _autoMocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<TestBody>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestBody());

        var subject = _autoMocker.CreateInstance<QueryApiClient>();
        var result = await subject.ExecuteAsync<TestBody>(
            new HttpRequestMessage(),
            TestContext.Current.CancellationToken);

        result.ResponseHeaders.TryGetValues("neo4j-cluster-affinity", out var vals).Should().BeTrue();
        vals.Should().ContainSingle().Which.Should().Be("shard-42");
    }

    [Fact]
    public async Task PropagatesException_WhenTransportThrows()
    {
        _autoMocker.GetMock<IQueryApiHttpTransport>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("HTTP 503"));

        var subject = _autoMocker.CreateInstance<QueryApiClient>();
        var act = () => subject.ExecuteAsync<TestBody>(new HttpRequestMessage(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }

    [Fact]
    public async Task PropagatesException_WhenErrorCheckerThrows()
    {
        _autoMocker.GetMock<IQueryApiHttpTransport>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new ByteArrayContent([]) });

        _autoMocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<TestBody>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TestBody { Errors = [new QueryApiErrorBody("Neo.ClientError.General.Unknown", "error")] });

        _autoMocker.GetMock<IQueryApiErrorChecker>()
            .Setup(x => x.ThrowIfErrors(It.IsAny<QueryApiErrorBody[]?>()))
            .Throws(new ClientException("Neo.ClientError.General.Unknown", "error"));

        var subject = _autoMocker.CreateInstance<QueryApiClient>();
        var act = () => subject.ExecuteAsync<TestBody>(new HttpRequestMessage(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ClientException>();
    }
}
