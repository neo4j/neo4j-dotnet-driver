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
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.ObjectStorage;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewAuthTokenManagerRequest : IProtocolMessage;

internal record AuthTokenManagerResponse(string Id) : IProtocolMessage;

internal class NewAuthTokenManagerHandler : MessageHandler<NewAuthTokenManagerRequest>
{
    private readonly IObjectStore _objectStore;
    private readonly IOutboundRoundTrip _roundTrip;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewAuthTokenManagerHandler(
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

    public override async Task ProcessAsync(NewAuthTokenManagerRequest message)
    {
        var id = _objectStore.Store<IAuthTokenManager>(storageId =>
            new TestKitAuthTokenManager(
                () => GetAuthAsync(storageId),
                (token, exception) => HandleSecurityExceptionAsync(storageId, token, exception)));

        _logger.LogDebug("Created auth token manager with id '{Id}'", id);
        await _responseWriter.WriteAsync(new AuthTokenManagerResponse(id));
    }

    private async ValueTask<IAuthToken> GetAuthAsync(string storageId)
    {
        var authRequest = new AuthTokenManagerGetAuthRequest(storageId);
        return await _roundTrip.SendExpectingAsync<IAuthToken>(authRequest);
    }

    private async ValueTask<bool> HandleSecurityExceptionAsync(
        string storageId,
        IAuthToken token,
        SecurityException exception)
    {
        var handleSecurityExceptionRequest = new AuthTokenManagerHandleSecurityExceptionRequest(
            storageId,
            ToWireToken(token),
            exception.Code);

        return await _roundTrip.SendExpectingAsync<bool>(handleSecurityExceptionRequest);
    }

    private static AuthorizationToken ToWireToken(IAuthToken token)
    {
        var content = ((AuthToken)token).Content;
        return new AuthorizationToken
        {
            Scheme = (string)content["scheme"],
            Principal = content.TryGetValue("principal", out var principal) ? (string)principal : null,
            Credentials = content.TryGetValue("credentials", out var credentials) ? (string)credentials : null,
            Realm = content.TryGetValue("realm", out var realm) ? (string)realm : null,
            Parameters = content.TryGetValue("parameters", out var parameters)
                ? (Dictionary<string, object>)parameters
                : null
        };
    }
}

internal class TestKitAuthTokenManager : IAuthTokenManager
{
    private readonly Func<ValueTask<IAuthToken>> _getAuth;
    private readonly Func<IAuthToken, SecurityException, ValueTask<bool>> _handleSecurityException;

    public TestKitAuthTokenManager(
        Func<ValueTask<IAuthToken>> getAuth,
        Func<IAuthToken, SecurityException, ValueTask<bool>> handleSecurityException)
    {
        _getAuth = getAuth;
        _handleSecurityException = handleSecurityException;
    }

    public ValueTask<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return _getAuth();
    }

    public ValueTask<bool> HandleSecurityExceptionAsync(
        IAuthToken token,
        SecurityException exception,
        CancellationToken cancellationToken = default)
    {
        return _handleSecurityException(token, exception);
    }
}
