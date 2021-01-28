// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License");
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
using System.Threading;

namespace Neo4j.Driver.Internal.Metrics
{
    internal class ConnectionPoolMetrics : IInternalConnectionPoolMetrics
    {
        private bool _disposed = false;
        private int _creating;
        private long _created;
        private long _failedToCreate;

        private int _closing;
        private long _closed;

        private int _acquiring;
        private long _acquired;
        private long _timedOutToAcquire;

        public int Creating => _creating;
        public long Created => Interlocked.Read(ref _created);
        public long FailedToCreate => Interlocked.Read(ref _failedToCreate);

        public int Closing => _closing;
        public long Closed => Interlocked.Read(ref _closed);

        public int Acquiring => _acquiring;
        public long Acquired => Interlocked.Read(ref _acquired);
        public long TimedOutToAcquire => Interlocked.Read(ref _timedOutToAcquire);

        public string Id { get; }

        private IConnectionPool _pool;
        private IInternalMetrics _metrics;
        public int InUse => _pool?.NumberOfInUseConnections ?? 0;
        public int Idle => _pool?.NumberOfIdleConnections ?? 0;

        ~ConnectionPoolMetrics() => Dispose(false);

        public ConnectionPoolMetrics(string poolId, IConnectionPool pool, IInternalMetrics metrics)
        {
            Id = poolId;
            _pool = pool;
            _metrics = metrics;
        }

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

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

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
}
