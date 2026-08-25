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

#nullable enable
using System;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class QueryApiSessionFactory : IQueryApiSessionFactory
{
    private readonly IResolutionScope _resolutionScope;
    private readonly ISessionIdGenerator _sessionIdGenerator;
    private readonly ILoggingContextTracker _driverTracker;
    private readonly ILogger _logger;

    public QueryApiSessionFactory(
        IResolutionScope resolutionScope,
        ISessionIdGenerator sessionIdGenerator,
        ILoggingContextTracker driverTracker,
        ILogger logger)
    {
        _resolutionScope = resolutionScope;
        _sessionIdGenerator = sessionIdGenerator;
        _driverTracker = driverTracker;
        _logger = logger;
    }

    public IInternalAsyncSession CreateSession(SessionConfig config, bool telemetryEnabled)
    {
        var sessionId = _sessionIdGenerator.Generate();
        _logger.LogDebug("Building session scope {sessionId}", sessionId);

        var authTokenManager = config.AuthToken is {}
            ? AuthTokenManagers.Static(config.AuthToken)
            : config.DriverContext.AuthTokenManager;

        var sessionScope = _resolutionScope.CreateChildScope(r => r
            .RegisterInstance(_driverTracker.CreateChild())
            .RegisterInstance(config)
            .RegisterInstance(authTokenManager)
            .RegisterType<ISessionContext, QueryApiSessionContext>());

        sessionScope.Resolve<ILoggingContextTracker>().Add("session", sessionId);

        var session = sessionScope.Resolve<IInternalAsyncSession>();
        session.Disposed += GetSessionDisposedHandler(sessionScope);
        return session;
    }

    private static AsyncEventHandler GetSessionDisposedHandler(IAsyncDisposable sessionScope)
    {
        return SessionDisposed;

        async Task SessionDisposed(object? o, EventArgs eventArgs)
        {
            await sessionScope.DisposeAsync().ConfigureAwait(false);
        }
    }
}
