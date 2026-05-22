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
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiSessionFactory : IQueryApiSessionFactory
{
    private readonly IResolutionScope _resolutionScope;
    private readonly ISessionIdGenerator _sessionIdGenerator;
    private readonly ILogger _logger;

    public QueryApiSessionFactory(
        IResolutionScope resolutionScope,
        ISessionIdGenerator sessionIdGenerator,
        ILogger logger)
    {
        _resolutionScope = resolutionScope;
        _sessionIdGenerator = sessionIdGenerator;
        _logger = logger;
    }

    public IInternalAsyncSession CreateSession(SessionConfig config, bool telemetryEnabled)
    {
        var sessionId = _sessionIdGenerator.Generate();
        _logger.Debug("Building session scope {sessionId}", sessionId);
        var sessionScope = _resolutionScope.CreateChildScope(r =>
        {
            r.AddLoggingContext("session", sessionId);
            r.RegisterInstance(config);
            r.RegisterType<ISessionContext, QueryApiSessionContext>();
        });

        _logger.Debug("Resolving session");
        return sessionScope.Resolve<IInternalAsyncSession>();
    }
}
