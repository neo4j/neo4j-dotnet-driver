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

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiSessionFactory : IQueryApiSessionFactory
{
    private readonly IBookmarkTracker _bookmarkTracker;
    private readonly ILoggingContextTracker _driverTracker;
    private readonly IQueryApiHttpTransport _httpTransport;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServerInfo _serverInfo;
    private readonly ISessionIdGenerator _sessionIdGenerator;

    public QueryApiSessionFactory(
        ISessionIdGenerator sessionIdGenerator,
        ILoggingContextTracker driverTracker,
        ILoggerFactory loggerFactory,
        IQueryApiHttpTransport httpTransport,
        IServerInfo serverInfo,
        IBookmarkTracker bookmarkTracker,
        ILogger logger)
    {
        _sessionIdGenerator = sessionIdGenerator;
        _driverTracker = driverTracker;
        _loggerFactory = loggerFactory;
        _httpTransport = httpTransport;
        _serverInfo = serverInfo;
        _bookmarkTracker = bookmarkTracker;
        _logger = logger;
    }

    public IInternalAsyncSession CreateSession(SessionConfig config, bool telemetryEnabled)
    {
        var sessionId = _sessionIdGenerator.Generate();
        _logger.LogDebug("Building session scope {sessionId}", sessionId);

        var authTokenManager = config.AuthToken is {}
            ? AuthTokenManagers.Static(config.AuthToken)
            : config.DriverContext.AuthTokenManager;

        var sessionTracker = _driverTracker.CreateChild();
        sessionTracker.Add("session", sessionId);

        var composition = new QueryApiSessionComposition(
            config,
            authTokenManager,
            sessionTracker,
            _loggerFactory,
            _httpTransport,
            _serverInfo,
            _bookmarkTracker);

        return composition.Session();
    }
}
