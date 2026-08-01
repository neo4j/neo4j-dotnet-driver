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

using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Summary;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ResultConsumeHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ResultConsumeHandler>();

    public ResultConsumeHandlerTests()
    {
        _autoMocker.Use<IContinuationCoordinator>(new ContinuationCoordinator());
    }

    [Fact]
    public async Task Writes_the_mapped_driver_error_when_consume_throws()
    {
        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        var exception = new ClientException("boom");
        cursorMock.Setup(c => c.ConsumeAsync()).ThrowsAsync(exception);

        var errorResponse = new DriverErrorResponse { Id = "error-1", ErrorType = "ClientError" };
        _autoMocker.GetMock<IDriverErrorMapper>().Setup(m => m.Map(exception)).Returns(errorResponse);

        var handler = _autoMocker.CreateInstance<ResultConsumeHandler>();
        var request = new ResultConsumeRequest
        {
            Result = new RegistryObject<IResultCursor>("result-1", cursorMock.Object)
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(errorResponse), Times.Once);
    }

    [Fact]
    public async Task Writes_the_mapped_summary_on_success()
    {
        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        var summary = Mock.Of<IResultSummary>();
        cursorMock.Setup(c => c.ConsumeAsync()).ReturnsAsync(summary);

        var mapped = new SummaryResponse(
            new SummaryQueryResponse("RETURN 1", new Dictionary<string, ICypherValue>()),
            "r",
            null,
            null,
            [],
            null,
            new SummaryServerInfoResponse(null, null, null),
            new SummaryCountersResponse(false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false),
            0,
            0,
            []);

        _autoMocker.GetMock<ISummaryMapper>().Setup(m => m.Map(summary)).Returns(mapped);

        var handler = _autoMocker.CreateInstance<ResultConsumeHandler>();
        var request = new ResultConsumeRequest
        {
            Result = new RegistryObject<IResultCursor>("result-1", cursorMock.Object)
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(mapped), Times.Once);
    }
}
