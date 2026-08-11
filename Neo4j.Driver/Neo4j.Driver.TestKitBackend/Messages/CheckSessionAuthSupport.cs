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

internal record CheckSessionAuthSupportRequest(Stored<IDriver> Driver) : IProtocolMessage;

internal record SessionAuthSupportResponse(string Id, bool Available) : IProtocolMessage;

internal class CheckSessionAuthSupportHandler : MessageHandler<CheckSessionAuthSupportRequest>
{
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public CheckSessionAuthSupportHandler(IResponseWriter responseWriter, ILogger logger)
    {
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(CheckSessionAuthSupportRequest message)
    {
        var available = await message.Driver.Object.SupportsSessionAuthAsync();
        _logger.LogDebug(
            "Checked session-auth support for driver with id '{Id}': {Available}",
            message.Driver.Id,
            available);

        await _responseWriter.WriteAsync(new SessionAuthSupportResponse(message.Driver.Id, available));
    }
}
