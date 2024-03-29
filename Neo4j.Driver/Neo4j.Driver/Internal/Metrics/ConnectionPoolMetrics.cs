﻿// Copyright (c) "Neo4j"
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
using System.Threading;

namespace Neo4j.Driver.Internal.Metrics;

internal class ConnectionPoolMetrics : IInternalConnectionPoolMetrics
{
    private long _acquired;

    private int _acquiring;
    private long _closed;

    private int _closing;
    private long _created;
    private int _creating;
    private bool _disposed;
    private long _failedToCreate;
    private IInternalMetrics _metrics;

    private IConnectionPool _pool;
    private long _timedOutToAcquire;

    public ConnectionPoolMetrics(string poolId, IConnectionPool pool, IInternalMetrics metrics)
    {
        Id = poolId;
        _pool = pool;
        _metrics = metrics;
    }

    public int Creating => _creating;
    public long Created => Interlocked.Read(ref _created);
    public long FailedToCreate => Interlocked.Read(ref _failedToCreate);

    public int Closing => _closing;
    public long Closed => Interlocked.Read(ref _closed);

    public int Acquiring => _acquiring;
    public long Acquired => Interlocked.Read(ref _acquired);
    public long TimedOutToAcquire => Interlocked.Read(ref _timedOutToAcquire);

    public string Id { get; }
    public int InUse => _pool?.NumberOfInUseConnections ?? 0;
    public int Idle => _pool?.NumberOfIdleConnections ?? 0;

    public void ConnectionCreating()
    {
        Interlocked.Increment(ref _creating);
    }

    public void ConnectionCreated()
    {
        Interlocked.Increment(ref _created);
        Interlocked.Decrement(ref _creating);
    }

    public void ConnectionFailedToCreate()
    {
        Interlocked.Increment(ref _failedToCreate);
        Interlocked.Decrement(ref _creating);
    }

    public void ConnectionClosing()
    {
        Interlocked.Increment(ref _closing);
    }

    public void ConnectionClosed()
    {
        Interlocked.Increment(ref _closed);
        Interlocked.Decrement(ref _closing);
    }

    public void PoolAcquiring()
    {
        Interlocked.Increment(ref _acquiring);
    }

    public void PoolAcquired()
    {
        Interlocked.Decrement(ref _acquiring);
        Interlocked.Increment(ref _acquired);
    }

    public void PoolFailedToAcquire()
    {
        Interlocked.Decrement(ref _acquiring);
    }

    public void PoolTimedOutToAcquire()
    {
        Interlocked.Increment(ref _timedOutToAcquire);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ConnectionPoolMetrics()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _pool = null;
            _metrics.RemovePoolMetrics(Id);
            _metrics = null;
        }

        //Mark as disposed
        _disposed = true;
    }

    public override string ToString()
    {
        return this.ToDictionary().ToContentString();
    }
}
