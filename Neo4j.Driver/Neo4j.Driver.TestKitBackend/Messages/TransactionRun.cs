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

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record TransactionRunRequest : IProtocolMessage
{
    public required Stored<IAsyncTransaction> Tx { get; init; }
    public required string Cypher { get; init; }
    public Dictionary<string, ICypherValue>? Params { get; init; }
}

internal class TransactionRunHandler : MessageHandler<TransactionRunRequest>
{
    private readonly IObjectStore _objectStore;
    private readonly ICypherToNativeMapper _cypherToNativeMapper;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public TransactionRunHandler(
        IObjectStore objectStore,
        IResponseWriter responseWriter,
        ICypherToNativeMapper cypherToNativeMapper,
        ILogger logger)
    {
        _objectStore = objectStore;
        _responseWriter = responseWriter;
        _cypherToNativeMapper = cypherToNativeMapper;
        _logger = logger;
    }

    public override async Task ProcessAsync(TransactionRunRequest message)
    {
        _logger.LogDebug(
            "Running query '{Cypher}' on transaction with id '{TxId}'",
            message.Cypher,
            message.Tx.Id);

        var cursor = await message.Tx.Object.RunAsync(message.Cypher, _cypherToNativeMapper.Map(message.Params));

        var keys = await cursor.KeysAsync();
        var storedResult = _objectStore.Store(cursor);
        _logger.LogDebug("Query result id '{ResultId}' returned keys: {@keys}", storedResult.Id, keys);

        await _responseWriter.WriteAsync(new ResultResponse(storedResult.Id, keys));
    }
}
