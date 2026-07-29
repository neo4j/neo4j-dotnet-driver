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
using Neo4j.Driver.TestKitBackend.Protocol;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record CheckSessionAuthSupportRequest : IProtocolMessage
{
    public required RegistryObject<IDriver> Driver { get; init; }
}

internal record SessionAuthSupportResponse(string Id, bool Available) : IProtocolMessage;

internal class CheckSessionAuthSupportHandler : MessageHandler<CheckSessionAuthSupportRequest>
{
    private readonly ILogger _logger;

    public CheckSessionAuthSupportHandler(ILogger logger)
    {
        _logger = logger;
    }

    public override async Task<IProtocolMessage?> ProcessAsync(CheckSessionAuthSupportRequest message)
    {
        var available = await message.Driver.Object.SupportsSessionAuthAsync();
        _logger.LogDebug(
            "Checked session-auth support for driver with id '{Id}': {Available}",
            message.Driver.Id,
            available);

        return new SessionAuthSupportResponse(message.Driver.Id, available);
    }
}
