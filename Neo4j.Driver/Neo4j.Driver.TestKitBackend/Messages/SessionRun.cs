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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record SessionRunRequest : IProtocolMessage
{
    public required RegistryObject<IAsyncSession> Session { get; init; }
    public required string Cypher { get; init; }

    public Dictionary<string, ICypherValue>? Params { get; init; }

    // Cypher-envelope dict on the wire; parsed but not yet converted to native values.
    public Dictionary<string, JsonElement>? TxMeta { get; init; }

    // Absent = driver default, null = explicitly no timeout, number = timeout in ms.
    public Optional<long?> Timeout { get; init; }
}

internal record ResultResponse(string Id, string[]? Keys) : IProtocolMessage;

internal class SessionRunHandler : DetachedOperationHandler<SessionRunRequest>
{
    private readonly IRegistry _registry;
    private readonly ICypherToNativeMapper _cypherToNativeMapper;
    private readonly ILogger _logger;

    public SessionRunHandler(
        IRegistry registry,
        IContinuationCoordinator coordinator,
        IResponseWriter responseWriter,
        ICypherToNativeMapper cypherToNativeMapper,
        IDriverErrorMapper driverErrorMapper,
        ILogger logger)
        : base(coordinator, responseWriter, driverErrorMapper, logger)
    {
        _registry = registry;
        _cypherToNativeMapper = cypherToNativeMapper;
        _logger = logger;
    }

    protected override async Task<IProtocolMessage> ExecuteAsync(SessionRunRequest message)
    {
        _logger.LogDebug(
            "Running query '{Cypher}' on session with id '{SessionId}'",
            message.Cypher,
            message.Session.Id);

        var cursor = await message.Session.Object.RunAsync(message.Cypher, _cypherToNativeMapper.Map(message.Params));

        var keys = await cursor.KeysAsync();
        var registeredResult = _registry.Register(cursor);
        _logger.LogDebug("Query result id '{ResultId}' returned keys: {@keys}", registeredResult.Id, keys);

        return new ResultResponse(registeredResult.Id, keys);
    }
}
