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
internal class QueryApiProtocolAdapter : IQueryApiProtocolAdapter
{
    private readonly IConnectivityVerifier _connectivityVerifier;
    private readonly IResolutionScope _scope;
    private readonly IQueryApiSessionFactory _sessionFactory;

    public QueryApiProtocolAdapter(
        IConnectivityVerifier connectivityVerifier,
        IQueryApiSessionFactory sessionFactory,
        IResolutionScope scope)
    {
        _connectivityVerifier = connectivityVerifier ??
            throw new ArgumentNullException(nameof(connectivityVerifier));

        _sessionFactory = sessionFactory ??
            throw new ArgumentNullException(nameof(sessionFactory));

        _scope = scope ??
            throw new ArgumentNullException(nameof(scope));
    }

    public IInternalAsyncSession CreateSession(SessionConfig config, bool reactive, bool telemetryEnabled)
    {
        return reactive 
            ? throw new NotSupportedException("Reactive sessions are not supported by the Query API.") 
            : _sessionFactory.CreateSession(config, telemetryEnabled);
    }

    public Task<bool> SupportsMultiDbAsync() => Task.FromResult(true);
    
    public Task<IServerInfo> VerifyConnectivityAndGetInfoAsync()
    {
        return _connectivityVerifier.VerifyAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _scope.DisposeAsync();
    }
}
