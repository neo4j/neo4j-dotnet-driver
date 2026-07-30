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
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record VerifyConnectivityRequest : IProtocolMessage
{
    public required RegistryObject<IDriver> Driver { get; init; }
}

internal class VerifyConnectivityHandler : MessageHandler<VerifyConnectivityRequest>
{
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public VerifyConnectivityHandler(IResponseWriter responseWriter, ILogger logger)
    {
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(VerifyConnectivityRequest message)
    {
        await message.Driver.Object.VerifyConnectivityAsync();
        _logger.LogDebug("Verified connectivity for driver with id '{Id}'", message.Driver.Id);
        await _responseWriter.WriteAsync(new DriverResponse(message.Driver.Id));
    }
}
