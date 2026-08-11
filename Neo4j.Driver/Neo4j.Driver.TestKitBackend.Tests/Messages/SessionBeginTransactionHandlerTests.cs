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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class SessionBeginTransactionHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<SessionBeginTransactionHandler>();

    [Fact]
    public async Task Begins_a_transaction_on_the_session_and_responds_with_its_id()
    {
        var transactionMock = _autoMocker.GetMock<IAsyncTransaction>();

        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        sessionMock
            .Setup(s => s.BeginTransactionAsync(It.IsAny<Action<TransactionConfigBuilder>>()))
            .ReturnsAsync(transactionMock.Object);

        var registeredTransaction = new Stored<IAsyncTransaction>("tx-1", transactionMock.Object);
        _autoMocker.GetMock<IObjectStore>().Setup(r => r.Register(transactionMock.Object)).Returns(registeredTransaction);

        var handler = _autoMocker.CreateInstance<SessionBeginTransactionHandler>();
        var request = new SessionBeginTransactionRequest
        {
            Session = new Stored<IAsyncSession>("session-1", sessionMock.Object)
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new TransactionResponse("tx-1")), Times.Once);
    }

    [Fact]
    public async Task Applies_the_mapped_transaction_config_to_BeginTransactionAsync()
    {
        var txMeta = new Dictionary<string, ICypherValue> { ["return_bookmark"] = new CypherString("bm1") };
        var timeout = Optional<long?>.Specified(17);
        Action<TransactionConfigBuilder> configure = _ => { };
        _autoMocker.GetMock<ITransactionConfigMapper>()
            .Setup(m => m.Map(txMeta, timeout))
            .Returns(configure);

        var transactionMock = _autoMocker.GetMock<IAsyncTransaction>();
        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        sessionMock
            .Setup(s => s.BeginTransactionAsync(configure))
            .ReturnsAsync(transactionMock.Object);

        var registeredTransaction = new Stored<IAsyncTransaction>("tx-1", transactionMock.Object);
        _autoMocker.GetMock<IObjectStore>().Setup(r => r.Register(transactionMock.Object)).Returns(registeredTransaction);

        var handler = _autoMocker.CreateInstance<SessionBeginTransactionHandler>();
        var request = new SessionBeginTransactionRequest
        {
            Session = new Stored<IAsyncSession>("session-1", sessionMock.Object),
            TxMeta = txMeta,
            Timeout = timeout
        };

        await handler.ProcessAsync(request);

        sessionMock.Verify(s => s.BeginTransactionAsync(configure), Times.Once);
    }
}
