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
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiProtocolAdapter : IQueryApiProtocolAdapter
{
    private readonly IQueryApiSessionFactory _sessionFactory;
    private readonly IVerifyConnectivityHandler _verifyConnectivityHandler;

    public QueryApiProtocolAdapter(
        IVerifyConnectivityHandler verifyConnectivityHandler,
        IQueryApiSessionFactory sessionFactory)
    {
        _verifyConnectivityHandler = verifyConnectivityHandler ??
            throw new ArgumentNullException(nameof(verifyConnectivityHandler));

        _sessionFactory = sessionFactory ?? 
            throw new ArgumentNullException(nameof(sessionFactory));
    }

    public IInternalAsyncSession CreateSession(SessionConfig config, bool reactive, bool telemetryEnabled)
    {
        return !reactive 
            ? _sessionFactory.CreateSession(config, telemetryEnabled) 
            : throw new NotSupportedException("Reactive sessions are not supported by the Query API.");
    }

    public Task<bool> SupportsMultiDbAsync() => Task.FromResult(true);


    public Task<IServerInfo> VerifyConnectivityAndGetInfoAsync()
    {
        return _verifyConnectivityHandler.VerifyConnectivityAsync();
    }


    public ValueTask DisposeAsync()
    {
        return default;
    }
}
