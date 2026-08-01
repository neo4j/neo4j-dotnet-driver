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
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class TransactionCommitHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<TransactionCommitHandler>();

    public TransactionCommitHandlerTests()
    {
        _autoMocker.Use<IContinuationCoordinator>(new ContinuationCoordinator());
    }

    [Fact]
    public async Task Commits_the_transaction_and_responds_with_its_id()
    {
        var txMock = _autoMocker.GetMock<IAsyncTransaction>();

        var handler = _autoMocker.CreateInstance<TransactionCommitHandler>();
        var request = new TransactionCommitRequest
        {
            Tx = new RegistryObject<IAsyncTransaction>("tx-1", txMock.Object)
        };

        await handler.ProcessAsync(request);

        txMock.Verify(t => t.CommitAsync(), Times.Once);
        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new TransactionResponse("tx-1")), Times.Once);
    }

    [Fact]
    public async Task Writes_the_mapped_driver_error_when_commit_throws()
    {
        var exception = new ClientException("boom");
        var txMock = _autoMocker.GetMock<IAsyncTransaction>();
        txMock.Setup(t => t.CommitAsync()).ThrowsAsync(exception);

        var errorResponse = new DriverErrorResponse { Id = "error-1", ErrorType = "ClientError" };
        _autoMocker.GetMock<IDriverErrorMapper>().Setup(m => m.Map(exception)).Returns(errorResponse);

        var handler = _autoMocker.CreateInstance<TransactionCommitHandler>();
        var request = new TransactionCommitRequest
        {
            Tx = new RegistryObject<IAsyncTransaction>("tx-1", txMock.Object)
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(errorResponse), Times.Once);
    }
}
