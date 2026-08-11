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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectStorage;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record ForcedRoutingTableUpdateRequest : IProtocolMessage
{
    public required Stored<IDriver> Driver { get; init; }
    public string? Database { get; init; }
    public string[]? Bookmarks { get; init; }
}

internal class ForcedRoutingTableUpdateHandler : MessageHandler<ForcedRoutingTableUpdateRequest>
{
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public ForcedRoutingTableUpdateHandler(IResponseWriter responseWriter, ILogger logger)
    {
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(ForcedRoutingTableUpdateRequest message)
    {
        var driver = (Internal.IInternalDriver)message.Driver.Object;
        var bookmarks = message.Bookmarks is { } bm ? Bookmarks.From(bm) : Bookmarks.Empty;
        await driver.ForceRoutingTableUpdateAsync(message.Database, bookmarks);
        _logger.LogDebug(
            "Forced routing table update for driver with id '{Id}', database '{Database}'",
            message.Driver.Id,
            message.Database);

        await _responseWriter.WriteAsync(new DriverResponse(message.Driver.Id));
    }
}
