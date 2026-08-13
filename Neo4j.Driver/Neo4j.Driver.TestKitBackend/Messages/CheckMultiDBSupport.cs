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
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record CheckMultiDBSupportRequest : IProtocolMessage
{
    [StoredObject]
    public required IDriver Driver { get; init; }
    public required string DriverId { get; init; }
}

internal record MultiDBSupportResponse(string Id, bool Available) : IProtocolMessage;

internal class CheckMultiDBSupportHandler : MessageHandler<CheckMultiDBSupportRequest>
{
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public CheckMultiDBSupportHandler(IResponseWriter responseWriter, ILogger logger)
    {
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(CheckMultiDBSupportRequest message)
    {
        var available = await message.Driver.SupportsMultiDbAsync();
        _logger.LogDebug("Checked multi-db support for driver with id '{Id}': {Available}", message.DriverId, available);
        await _responseWriter.WriteAsync(new MultiDBSupportResponse(message.DriverId, available));
    }
}
