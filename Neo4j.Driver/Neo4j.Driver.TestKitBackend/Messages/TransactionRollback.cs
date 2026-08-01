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
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record TransactionRollbackRequest : IProtocolMessage
{
    public required RegistryObject<IAsyncTransaction> Tx { get; init; }
}

internal class TransactionRollbackHandler : BackgroundOperationHandler<TransactionRollbackRequest>
{
    private readonly ILogger _logger;

    public TransactionRollbackHandler(
        IContinuationCoordinator coordinator,
        IResponseWriter responseWriter,
        IDriverErrorMapper driverErrorMapper,
        ILogger logger)
        : base(coordinator, responseWriter, driverErrorMapper, logger)
    {
        _logger = logger;
    }

    protected override async Task<IProtocolMessage> ExecuteAsync(TransactionRollbackRequest message)
    {
        await message.Tx.Object.RollbackAsync();
        _logger.LogDebug("Rolled back transaction with id '{Id}'", message.Tx.Id);
        return new TransactionResponse(message.Tx.Id);
    }
}
