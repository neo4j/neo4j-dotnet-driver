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

internal record NewAuthTokenManagerRequest : IProtocolMessage;

internal class NewAuthTokenManagerHandler : MessageHandler<NewAuthTokenManagerRequest>
{
    private readonly IRegistry _registry;
    private readonly IContinuationCoordinator _coordinator;
    private readonly IAuthTokenManagerFactory _managerFactory;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewAuthTokenManagerHandler(
        IRegistry registry,
        IContinuationCoordinator coordinator,
        IAuthTokenManagerFactory managerFactory,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _registry = registry;
        _coordinator = coordinator;
        _managerFactory = managerFactory;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewAuthTokenManagerRequest message)
    {
        // The manager needs its own registry id for the callback requests, which only exists
        // once the manager is registered — hence the captured local.
        var managerId = "";
        var manager = _managerFactory.Create(() => GetAuthAsync(managerId));

        var registered = _registry.Register(manager);
        managerId = registered.Id;

        _logger.LogDebug("Created auth token manager with id '{Id}'", registered.Id);
        await _responseWriter.WriteAsync(new AuthTokenManagerResponse(registered.Id));
    }

    // Runs on a driver thread mid-operation: borrows the open request's response slot to send
    // the callback request, then pauses until the ...Completed handler resolves it (spec §6).
    private async ValueTask<IAuthToken> GetAuthAsync(string managerId)
    {
        var pending = _coordinator.RegisterCallback();
        _coordinator.CompleteNextResponse(new AuthTokenManagerGetAuthRequest(pending.Id, managerId));

        var completion = (AuthTokenManagerGetAuthCompletedRequest)await pending.Completion;
        return completion.Auth.Value.ToAuthToken();
    }
}

internal interface IAuthTokenManagerFactory
{
    IAuthTokenManager Create(Func<ValueTask<IAuthToken>> getAuth);
}

internal class AuthTokenManagerFactory : IAuthTokenManagerFactory
{
    public IAuthTokenManager Create(Func<ValueTask<IAuthToken>> getAuth)
    {
        return new TestKitAuthTokenManager(getAuth);
    }
}

// The driver-facing face of a testkit-side custom auth token manager: every interface call is
// relayed to testkit as a callback request.
internal class TestKitAuthTokenManager : IAuthTokenManager
{
    private readonly Func<ValueTask<IAuthToken>> _getAuth;

    public TestKitAuthTokenManager(Func<ValueTask<IAuthToken>> getAuth)
    {
        _getAuth = getAuth;
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
        throw new NotSupportedException(
            "The AuthTokenManagerHandleSecurityException callback is not implemented yet.");
    }
}
