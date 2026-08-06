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
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewBasicAuthTokenManagerRequest : IProtocolMessage;

internal record BasicAuthTokenManagerResponse(string Id) : IProtocolMessage;

internal class NewBasicAuthTokenManagerHandler : MessageHandler<NewBasicAuthTokenManagerRequest>
{
    private readonly IRegistry _registry;
    private readonly ICallbackExchanger _callbackExchanger;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewBasicAuthTokenManagerHandler(
        IRegistry registry,
        ICallbackExchanger callbackExchanger,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _registry = registry;
        _callbackExchanger = callbackExchanger;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewBasicAuthTokenManagerRequest message)
    {
        var registered = _registry.Register(CreateRegisteredManager);
        _logger.LogDebug("Created basic auth token manager with id '{Id}'", registered.Id);
        await _responseWriter.WriteAsync(new BasicAuthTokenManagerResponse(registered.Id));
    }

    private IAuthTokenManager CreateRegisteredManager(string managerId)
    {
        ValueTask<IAuthToken> ProvideFromManager() => ProvideTokenAsync(managerId);
        return AuthTokenManagers.Basic(ProvideFromManager);
    }

    private async ValueTask<IAuthToken> ProvideTokenAsync(string managerId)
    {
        var completion = await _callbackExchanger.SendAsync<BasicAuthTokenProviderCompleted>(
            id => new BasicAuthTokenProviderRequest(id, managerId));

        return completion.Auth.Value.ToAuthToken();
    }
}
