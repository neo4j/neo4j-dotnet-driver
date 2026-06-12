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

using System;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Routing;

namespace Neo4j.Driver.Internal;

/// <summary>
/// IProtocolAdapter implementation that wraps the existing Bolt connection stack.
/// This is the strangler-fig seam: all driver traffic flows through here,
/// and the entire internal stack beneath it is unchanged.
/// </summary>
internal sealed class BoltProtocolAdapter : IProtocolAdapter
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly DriverContext _context;
    private readonly IAsyncRetryLogic _retryLogic;

    public BoltProtocolAdapter(IConnectionProvider connectionProvider, DriverContext context)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _retryLogic = new AsyncRetryLogic(
            context.Config.MaxTransactionRetryTime,
            context.Config.Neo4JLogger);
    }

    public IInternalAsyncSession CreateSession(
        SessionConfig config,
        bool reactive,
        bool telemetryEnabled)
    {
        return new AsyncSession(
            _connectionProvider,
            _context.Config.Neo4JLogger,
            _retryLogic,
            _context.Config.FetchSize,
            config,
            reactive,
            telemetryEnabled);
    }

    public Task<bool> SupportsMultiDbAsync()
    {
        return _connectionProvider.SupportsMultiDbAsync();
    }

    public Task<bool> SupportsReAuthAsync()
    {
        return _connectionProvider.SupportsReAuthAsync();
    }

    public Task<IServerInfo> VerifyConnectivityAndGetInfoAsync()
    {
        return _connectionProvider.VerifyConnectivityAndGetInfoAsync();
    }

    public IRoutingTable GetRoutingTable(string database)
    {
        return _connectionProvider.GetRoutingTable(database);
    }

    public ValueTask DisposeAsync()
    {
        return _connectionProvider.DisposeAsync();
    }
}
