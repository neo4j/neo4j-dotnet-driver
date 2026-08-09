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
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using DriverAuthTokenAndExpiration = Neo4j.Driver.AuthTokenAndExpiration;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewBearerAuthTokenManagerRequest : IProtocolMessage;

internal record BearerAuthTokenManagerResponse(string Id) : IProtocolMessage;

internal class NewBearerAuthTokenManagerHandler : MessageHandler<NewBearerAuthTokenManagerRequest>
{
    private readonly IRegistry _registry;
    private readonly ICallbackExchanger _callbackExchanger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewBearerAuthTokenManagerHandler(
        IRegistry registry,
        ICallbackExchanger callbackExchanger,
        IDateTimeProvider dateTimeProvider,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _registry = registry;
        _callbackExchanger = callbackExchanger;
        _dateTimeProvider = dateTimeProvider;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewBearerAuthTokenManagerRequest message)
    {
        var registered = _registry.Register(CreateRegisteredManager);
        _logger.LogDebug("Created bearer auth token manager with id '{Id}'", registered.Id);
        await _responseWriter.WriteAsync(new BearerAuthTokenManagerResponse(registered.Id));
    }

    private IAuthTokenManager CreateRegisteredManager(string managerId)
    {
        ValueTask<DriverAuthTokenAndExpiration> ProvideFromManager() => ProvideTokenAsync(managerId);
        return AuthTokenManagers.Bearer(_dateTimeProvider, ProvideFromManager);
    }

    private async ValueTask<DriverAuthTokenAndExpiration> ProvideTokenAsync(string managerId)
    {
        var completion = await _callbackExchanger.SendAsync<BearerAuthTokenProviderCompleted>(
            id => new BearerAuthTokenProviderRequest(id, managerId));

        var payload = completion.Auth.Value;
        var token = payload.Auth.Value.ToAuthToken();
        return payload.ExpiresInMs is {} expiresInMs
            ? new DriverAuthTokenAndExpiration(token, DateTimeProvider.StaticInstance.Now().AddMilliseconds(expiresInMs))
            : new DriverAuthTokenAndExpiration(token);
    }
}
