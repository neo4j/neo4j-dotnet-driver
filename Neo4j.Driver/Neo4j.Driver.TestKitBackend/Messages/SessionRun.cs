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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record SessionRunRequest : IProtocolMessage
{
    public required RegistryObject<IAsyncSession> Session { get; init; }
    public required string Cypher { get; init; }

    // Cypher-envelope dicts on the wire; parsed but not yet converted to native values (M7).
    public Dictionary<string, JsonElement>? Params { get; init; }
    public Dictionary<string, JsonElement>? TxMeta { get; init; }

    // Absent = driver default, null = explicitly no timeout, number = timeout in ms.
    public Optional<long?> Timeout { get; init; }
}

internal record ResultResponse(string Id, string[]? Keys) : IProtocolMessage;

internal class SessionRunHandler : MessageHandler<SessionRunRequest>
{
    private readonly IRegistry _registry;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public SessionRunHandler(IRegistry registry, IResponseWriter responseWriter, ILogger logger)
    {
        _registry = registry;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(SessionRunRequest message)
    {
        var cursor = await message.Session.Object.RunAsync(message.Cypher);
        var registryObject = _registry.Register(cursor);
        var keys = await cursor.KeysAsync();
        _logger.LogDebug("Ran query on session with id '{SessionId}', result id '{ResultId}'", message.Session.Id, registryObject.Id);
        await _responseWriter.WriteAsync(new ResultResponse(registryObject.Id, keys));
    }
}
