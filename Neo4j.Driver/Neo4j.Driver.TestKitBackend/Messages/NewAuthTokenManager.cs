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
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewAuthTokenManagerRequest : IProtocolMessage;

internal class NewAuthTokenManagerHandler : MessageHandler<NewAuthTokenManagerRequest>
{
    private readonly IRegistry _registry;
    private readonly IContinuationCoordinator _coordinator;
    private readonly Func<Func<ValueTask<IAuthToken>>, Func<IAuthToken, SecurityException, ValueTask<bool>>,
        IAuthTokenManager> _createManager;

    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewAuthTokenManagerHandler(
        IRegistry registry,
        IContinuationCoordinator coordinator,
        Func<Func<ValueTask<IAuthToken>>, Func<IAuthToken, SecurityException, ValueTask<bool>>, IAuthTokenManager>
            createManager,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _registry = registry;
        _coordinator = coordinator;
        _createManager = createManager;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewAuthTokenManagerRequest message)
    {
        var managerId = "";
        var manager = _createManager(
            () => GetAuthAsync(managerId),
            (token, exception) => HandleSecurityExceptionAsync(managerId, token, exception));

        var registered = _registry.Register(manager);
        managerId = registered.Id;

        _logger.LogDebug("Created auth token manager with id '{Id}'", registered.Id);
        await _responseWriter.WriteAsync(new AuthTokenManagerResponse(registered.Id));
    }

    private async ValueTask<IAuthToken> GetAuthAsync(string managerId)
    {
        var pending = _coordinator.RegisterCallback();
        _coordinator.CompleteNextResponse(new AuthTokenManagerGetAuthRequest(pending.Id, managerId));

        var completion = (AuthTokenManagerGetAuthCompletedRequest)await pending.Completion;
        return completion.Auth.Value.ToAuthToken();
    }

    private async ValueTask<bool> HandleSecurityExceptionAsync(
        string managerId,
        IAuthToken token,
        SecurityException exception)
    {
        var pending = _coordinator.RegisterCallback();
        _coordinator.CompleteNextResponse(
            new AuthTokenManagerHandleSecurityExceptionRequest(
                pending.Id,
                managerId,
                ToWireToken(token),
                exception.Code));

        var completion = (AuthTokenManagerHandleSecurityExceptionCompletedRequest)await pending.Completion;
        return completion.Handled;
    }

    private AuthorizationToken ToWireToken(IAuthToken token)
    {
        var content = ((AuthToken)token).Content;
        return new AuthorizationToken(
            (string)content["scheme"],
            (string)content["principal"],
            (string)content["credentials"],
            content.TryGetValue("realm", out var realm) ? (string)realm : null);
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
