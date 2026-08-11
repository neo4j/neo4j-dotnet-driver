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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record SessionBeginTransactionRequest : IProtocolMessage
{
    public required Stored<IAsyncSession> Session { get; init; }

    public Dictionary<string, ICypherValue>? TxMeta { get; init; }

    // Absent = driver default, null = explicitly no timeout, number = timeout in ms.
    public Optional<long?> Timeout { get; init; }
}

internal record TransactionResponse(string Id) : IProtocolMessage;

internal class SessionBeginTransactionHandler : MessageHandler<SessionBeginTransactionRequest>
{
    private readonly IObjectStore _objectStore;
    private readonly ITransactionConfigMapper _transactionConfigMapper;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public SessionBeginTransactionHandler(
        IObjectStore objectStore,
        IResponseWriter responseWriter,
        ITransactionConfigMapper transactionConfigMapper,
        ILogger logger)
    {
        _objectStore = objectStore;
        _responseWriter = responseWriter;
        _transactionConfigMapper = transactionConfigMapper;
        _logger = logger;
    }

    public override async Task ProcessAsync(SessionBeginTransactionRequest message)
    {
        var transaction = await message.Session.Object.BeginTransactionAsync(
            _transactionConfigMapper.Map(message.TxMeta, message.Timeout));

        var registered = _objectStore.Register(transaction);
        _logger.LogDebug(
            "Began transaction with id '{Id}' on session with id '{SessionId}'",
            registered.Id,
            message.Session.Id);

        await _responseWriter.WriteAsync(new TransactionResponse(registered.Id));
    }
}
