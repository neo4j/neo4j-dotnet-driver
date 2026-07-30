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
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record VerifyAuthenticationRequest : IProtocolMessage
{
    public required RegistryObject<IDriver> Driver { get; init; }
    public required IWireType<AuthorizationToken> AuthorizationToken { get; init; }
}

internal record DriverIsAuthenticatedResponse(string Id, bool Authenticated) : IProtocolMessage;

internal class VerifyAuthenticationHandler : MessageHandler<VerifyAuthenticationRequest>
{
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public VerifyAuthenticationHandler(IResponseWriter responseWriter, ILogger logger)
    {
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(VerifyAuthenticationRequest message)
    {
        var authenticated =
            await message.Driver.Object.VerifyAuthenticationAsync(message.AuthorizationToken.Value.ToAuthToken());

        _logger.LogDebug(
            "Verified authentication for driver with id '{Id}': {Authenticated}",
            message.Driver.Id,
            authenticated);

        await _responseWriter.WriteAsync(new DriverIsAuthenticatedResponse(message.Driver.Id, authenticated));
    }
}
