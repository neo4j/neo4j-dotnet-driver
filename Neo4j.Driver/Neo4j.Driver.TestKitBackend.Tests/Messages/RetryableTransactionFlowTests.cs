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

using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Retry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

// SessionReadTransactionHandler and RetryablePositiveHandler only make sense as a pair — this
// pins the handshake between them via a real IRetryCoordinator (the collaboration is the point;
// mocking the coordinator away here would just re-assert the implementation).
public class RetryableTransactionFlowTests
{
    [Fact]
    public async Task A_single_successful_attempt_round_trips_RetryableTry_then_RetryableDone()
    {
        var coordinator = new RetryCoordinator();
        var registryMock = new Mock<IRegistry>();
        var responseWriterMock = new Mock<IResponseWriter>();
        var loggerMock = new Mock<ILogger>();

        var txMock = new Mock<IAsyncTransaction>();
        var sessionMock = new Mock<IAsyncSession>();
        sessionMock
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), null))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        registryMock
            .Setup(r => r.Register(txMock.Object))
            .Returns(new RegistryObject<IAsyncTransaction>("tx-1", txMock.Object));

        var readHandler = new SessionReadTransactionHandler(
            registryMock.Object,
            coordinator,
            responseWriterMock.Object,
            loggerMock.Object);

        var positiveHandler = new RetryablePositiveHandler(coordinator, responseWriterMock.Object);

        var sessionHandle = new RegistryObject<IAsyncSession>("session-1", sessionMock.Object);

        await WithTimeoutAsync(
            readHandler.ProcessAsync(new SessionReadTransactionRequest { Session = sessionHandle }));

        responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-1")), Times.Once);

        await WithTimeoutAsync(
            positiveHandler.ProcessAsync(new RetryablePositiveRequest { Session = sessionHandle }));

        responseWriterMock.Verify(w => w.WriteAsync(new RetryableDoneResponse()), Times.Once);
    }

    private static async Task WithTimeoutAsync(Task task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.Same(task, completed);
        await task;
    }
}
