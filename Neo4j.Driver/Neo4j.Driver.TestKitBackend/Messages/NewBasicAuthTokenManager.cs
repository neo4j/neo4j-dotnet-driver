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
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.ObjectStorage;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewBasicAuthTokenManagerRequest : IProtocolMessage;

internal record BasicAuthTokenManagerResponse(string Id) : IProtocolMessage;

internal class NewBasicAuthTokenManagerHandler : MessageHandler<NewBasicAuthTokenManagerRequest>
{
    private readonly IObjectStore _objectStore;
    private readonly IOutboundRoundTrip _roundTrip;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewBasicAuthTokenManagerHandler(
        IObjectStore objectStore,
        IOutboundRoundTrip roundTrip,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _objectStore = objectStore;
        _roundTrip = roundTrip;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewBasicAuthTokenManagerRequest message)
    {
        var stored = _objectStore.Store(CreateStoredManager);
        _logger.LogDebug("Created basic auth token manager with id '{Id}'", stored.Id);
        await _responseWriter.WriteAsync(new BasicAuthTokenManagerResponse(stored.Id));
    }

    private IAuthTokenManager CreateStoredManager(string managerId)
    {
        ValueTask<IAuthToken> ProvideFromManager() => ProvideTokenAsync(managerId);
        return AuthTokenManagers.Basic(ProvideFromManager);
    }

    private async ValueTask<IAuthToken> ProvideTokenAsync(string managerId)
    {
        var providerRequest = new BasicAuthTokenProviderRequest(managerId);
        return await _roundTrip.SendExpectingAsync<IAuthToken>(providerRequest);
    }
}
