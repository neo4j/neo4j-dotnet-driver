// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
// you may not use this file except in compliance with the License.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Internal.Routing;

internal class ClusterConnectionPool : IClusterConnectionPool
{
    private readonly ILogger _logger;
    private readonly IConnectionPoolFactory _poolFactory;

    private readonly ConcurrentDictionary<Uri, IConnectionPool> _pools = new();

    private int _closedMarker;

    public ClusterConnectionPool(
        IEnumerable<Uri> initUris,
        IPooledConnectionFactory connectionFactory,
        RoutingSettings routingSetting,
        ConnectionPoolSettings poolSettings,
        ILogger logger
    ) : this(
        initUris,
        new ConnectionPoolFactory(connectionFactory, poolSettings, routingSetting.RoutingContext, logger),
        logger)
    {
    }

    // test only
    internal ClusterConnectionPool(
        IConnectionPoolFactory poolFactory,
        ConcurrentDictionary<Uri, IConnectionPool> clusterPool,
        ILogger logger = null
    ) :
        this(Enumerable.Empty<Uri>(), poolFactory, logger)
    {
        _pools = clusterPool;
    }

    private ClusterConnectionPool(
        IEnumerable<Uri> initUris,
        IConnectionPoolFactory poolFactory,
        ILogger logger)
    {
        _logger = logger;
        _poolFactory = poolFactory;
        Add(initUris);
    }

    private bool IsClosed => _closedMarker > 0;

    public Task<IConnection> AcquireAsync(
        Uri uri,
        AccessMode mode,
        string database,
        string impersonatedUser,
        Bookmarks bookmarks)
    {
        if (!_pools.TryGetValue(uri, out var pool))
        {
            return Task.FromResult((IConnection)null);
        }

        return pool.AcquireAsync(mode, database, impersonatedUser, bookmarks);
    }

    public async Task AddAsync(IEnumerable<Uri> servers)
    {
        foreach (var uri in servers)
        {
            _pools.AddOrUpdate(uri, _poolFactory.Create, ActivateConnectionPool);
        }

        if (IsClosed)
        {
            // Anything added after dispose should be directly cleaned.
            await ClearAsync().ConfigureAwait(false);
            throw new ObjectDisposedException(
                GetType().Name,
                $"Failed to create connections with servers {servers.ToContentString()} as the driver has already started to dispose.");
        }
    }

    public async Task UpdateAsync(IEnumerable<Uri> added, IEnumerable<Uri> removed)
    {
        await AddAsync(added).ConfigureAwait(false);
        foreach (var uri in removed)
        {
            if (_pools.TryGetValue(uri, out var pool))
            {
                await pool.DeactivateAsync().ConfigureAwait(false);
                if (pool.NumberOfInUseConnections == 0)
                {
                    await PurgeAsync(uri).ConfigureAwait(false);
                }
            }
        }
    }

    public Task DeactivateAsync(Uri uri)
    {
        if (_pools.TryGetValue(uri, out var pool))
        {
            return pool.DeactivateAsync();
        }

        return Task.CompletedTask;
    }

    public int NumberOfInUseConnections(Uri uri)
    {
        if (_pools.TryGetValue(uri, out var pool))
        {
            return pool.NumberOfInUseConnections;
        }

        return 0;
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(CloseAsync());
    }

    private void Add(IEnumerable<Uri> servers)
    {
        foreach (var uri in servers)
        {
            _pools.AddOrUpdate(uri, _poolFactory.Create, ActivateConnectionPool);
        }
    }

    internal Task CloseAsync()
    {
        if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
        {
            return ClearAsync();
        }

        return Task.CompletedTask;
    }

    private Task ClearAsync()
    {
        var clearTasks = new List<Task>();

        var uris = _pools.Keys;
        foreach (var uri in uris)
        {
            clearTasks.Add(PurgeAsync(uri));
        }

        return Task.WhenAll(clearTasks);
    }

    private Task PurgeAsync(Uri uri)
    {
        var removed = _pools.TryRemove(uri, out var toRemove);
        if (removed)
        {
            return toRemove.DisposeAsync().AsTask();
        }

        return Task.CompletedTask;
    }

    public override string ToString()
    {
        return _pools.ToContentString();
    }

    private static IConnectionPool ActivateConnectionPool(Uri uri, IConnectionPool pool)
    {
        pool.Activate();
        return pool;
    }
}
