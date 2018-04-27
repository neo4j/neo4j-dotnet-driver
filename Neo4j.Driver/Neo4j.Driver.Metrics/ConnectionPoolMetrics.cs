// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
    internal class ConnectionPoolMetrics : IConnectionPoolMetrics, IConnectionPoolListener
    {
        private int _creating;
        private long _created;
        private long _failedToCreate;

        private int _closing;
        private long _closed;

        private int _acquiring;
        private long _acquired;
        private long _timedOutToAcquire;

        public int Creating => _creating;
        public long Created => _created;
        public long FailedToCreate => _failedToCreate;

        public int Closing => _closing;
        public long Closed => _closed;

        public int Acquiring => _acquiring;
        public long Acquired => _acquired;
        public long TimedOutToAcquire => _timedOutToAcquire;

        public string UniqueName { get; }

        private IConnectionPool _pool;
        public int InUse => _pool?.NumberOfInUseConnections ?? 0;
        public int Idle => _pool?.NumberOfIdleConnections ?? 0;
        public PoolStatus PoolStatus => _pool?.Status.Code ?? PoolStatus.Closed;

        private readonly Histogram _histogram;
        public IHistogram AcquisitionTimeHistogram => _histogram.Snapshot();

        public ConnectionPoolMetrics(Uri uri, IConnectionPool pool, TimeSpan connAcquisitionTimeout)
        {
            UniqueName = uri.ToString();
            _pool = pool;
            _histogram = new Histogram(connAcquisitionTimeout.Ticks);
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

        public void PoolAcquiring(IListenerEvent listenerEvent)
        {
            Interlocked.Increment(ref _acquiring);
            listenerEvent.Start();
        }

        public void PoolAcquired(IListenerEvent listenerEvent)
        {
            Interlocked.Decrement(ref _acquiring);
            Interlocked.Increment(ref _acquired);
            _histogram.RecordValue(listenerEvent.GetElapsed());
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
            _pool = null;
        }

        public override string ToString()
        {
            return this.ToDictionary().ToContentString();
        }
    }
}
