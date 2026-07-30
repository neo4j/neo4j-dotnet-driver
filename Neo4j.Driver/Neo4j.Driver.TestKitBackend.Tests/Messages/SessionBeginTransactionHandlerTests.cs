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
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
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
        sessionMock.Setup(s => s.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

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
}
