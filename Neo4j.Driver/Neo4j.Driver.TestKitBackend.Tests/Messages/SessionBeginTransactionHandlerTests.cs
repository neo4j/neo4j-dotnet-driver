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
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class SessionBeginTransactionHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<SessionBeginTransactionHandler>();

    public SessionBeginTransactionHandlerTests()
    {
        // The handler runs detached and hands its response back through the coordinator's
        // continuation - a mocked coordinator would never complete it.
        _autoMocker.Use<IContinuationCoordinator>(new ContinuationCoordinator());
    }

    [Fact]
    public async Task Begins_a_transaction_on_the_session_and_responds_with_its_id()
    {
        var transactionMock = _autoMocker.GetMock<IAsyncTransaction>();

        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        sessionMock
            .Setup(s => s.BeginTransactionAsync(It.IsAny<Action<TransactionConfigBuilder>>()))
            .ReturnsAsync(transactionMock.Object);

        var registeredTransaction = new RegistryObject<IAsyncTransaction>("tx-1", transactionMock.Object);
        _autoMocker.GetMock<IRegistry>().Setup(r => r.Register(transactionMock.Object)).Returns(registeredTransaction);

        var handler = _autoMocker.CreateInstance<SessionBeginTransactionHandler>();
        var request = new SessionBeginTransactionRequest
        {
            Session = new RegistryObject<IAsyncSession>("session-1", sessionMock.Object)
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new TransactionResponse("tx-1")), Times.Once);
    }

    [Fact]
    public async Task Begins_the_transaction_with_the_mapped_tx_metadata()
    {
        var txMeta = new Dictionary<string, ICypherValue> { ["return_bookmark"] = new CypherString("bm1") };
        var mapped = new Dictionary<string, object> { ["return_bookmark"] = "bm1" };
        _autoMocker.GetMock<ICypherToNativeMapper>().Setup(m => m.Map(txMeta)).Returns(mapped);

        var transactionMock = _autoMocker.GetMock<IAsyncTransaction>();

        Action<TransactionConfigBuilder>? configure = null;
        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        sessionMock
            .Setup(s => s.BeginTransactionAsync(It.IsAny<Action<TransactionConfigBuilder>>()))
            .Callback<Action<TransactionConfigBuilder>>(action => configure = action)
            .ReturnsAsync(transactionMock.Object);

        var registeredTransaction = new RegistryObject<IAsyncTransaction>("tx-1", transactionMock.Object);
        _autoMocker.GetMock<IRegistry>().Setup(r => r.Register(transactionMock.Object)).Returns(registeredTransaction);

        var handler = _autoMocker.CreateInstance<SessionBeginTransactionHandler>();
        var request = new SessionBeginTransactionRequest
        {
            Session = new RegistryObject<IAsyncSession>("session-1", sessionMock.Object),
            TxMeta = txMeta
        };

        await handler.ProcessAsync(request);

        Assert.NotNull(configure);
        var config = new TransactionConfig();
        configure!(new TransactionConfigBuilder(Mock.Of<INeo4jLogger>(), config));
        Assert.Equal(mapped, config.Metadata);
    }

    [Fact]
    public async Task Writes_the_mapped_driver_error_when_begin_throws()
    {
        var exception = new ClientException("boom");
        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        sessionMock
            .Setup(s => s.BeginTransactionAsync(It.IsAny<Action<TransactionConfigBuilder>>()))
            .ThrowsAsync(exception);

        var errorResponse = new DriverErrorResponse { Id = "error-1", ErrorType = "ClientError" };
        _autoMocker.GetMock<IDriverErrorMapper>().Setup(m => m.Map(exception)).Returns(errorResponse);

        var handler = _autoMocker.CreateInstance<SessionBeginTransactionHandler>();
        var request = new SessionBeginTransactionRequest
        {
            Session = new RegistryObject<IAsyncSession>("session-1", sessionMock.Object)
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(errorResponse), Times.Once);
    }
}
