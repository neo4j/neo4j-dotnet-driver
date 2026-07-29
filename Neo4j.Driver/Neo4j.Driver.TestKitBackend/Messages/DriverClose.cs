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

internal record DriverCloseRequest : IProtocolMessage
{
    public required RegistryObject<IDriver> Driver { get; init; }
}

internal class DriverCloseHandler : MessageHandler<DriverCloseRequest>
{
    private readonly IRegistry _registry;
    private readonly ILogger _logger;

    public DriverCloseHandler(IRegistry registry, ILogger logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public override async Task<IProtocolMessage?> ProcessAsync(DriverCloseRequest message)
    {
        await message.Driver.Object.DisposeAsync();
        _registry.Remove(message.Driver.Id);
        _logger.LogDebug("Closed driver with id '{Id}'", message.Driver.Id);
        return new DriverResponse(message.Driver.Id);
    }
}
