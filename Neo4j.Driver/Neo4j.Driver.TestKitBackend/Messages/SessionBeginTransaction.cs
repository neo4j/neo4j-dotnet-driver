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

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record SessionBeginTransactionRequest : IProtocolMessage
{
    public required RegistryObject<IAsyncSession> Session { get; init; }

    // Cypher-envelope dict on the wire; parsed but not yet converted to native values (M11).
    public Dictionary<string, JsonElement>? TxMeta { get; init; }

    // Absent = driver default, null = explicitly no timeout, number = timeout in ms.
    public Optional<long?> Timeout { get; init; }
}

internal record TransactionResponse(string Id) : IProtocolMessage;

// Detached because beginning a transaction can acquire a connection, which may call back into
// testkit mid-operation (auth manager / resolver callbacks, spec §6).
internal class SessionBeginTransactionHandler : DetachedOperationHandler<SessionBeginTransactionRequest>
{
    private readonly IRegistry _registry;
    private readonly ILogger _logger;

    public SessionBeginTransactionHandler(
        IRegistry registry,
        IContinuationCoordinator coordinator,
        IResponseWriter responseWriter,
        IDriverErrorMapper driverErrorMapper,
        ILogger logger)
        : base(coordinator, responseWriter, driverErrorMapper, logger)
    {
        _registry = registry;
        _logger = logger;
    }

    protected override async Task<IProtocolMessage> ExecuteAsync(SessionBeginTransactionRequest message)
    {
        var transaction = await message.Session.Object.BeginTransactionAsync();
        var registered = _registry.Register(transaction);
        _logger.LogDebug(
            "Began transaction with id '{Id}' on session with id '{SessionId}'",
            registered.Id,
            message.Session.Id);

        return new TransactionResponse(registered.Id);
    }
}
