// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Diagnostics;
using System.Threading;
using HdrHistogram;
using static Neo4j.Driver.Internal.Metrics.ConnectionMetrics;
using static Neo4j.Driver.Internal.Metrics.Histogram;

namespace Neo4j.Driver.Internal.Metrics
{
    internal class ConnectionPoolMetrics : IConnectionPoolMetrics, IConnectionPoolListener
    {
        private long _created;
        private long _closed;
        private long _failedToCreate;
        private int _toCreate;
        private int _toClose;

        private IConnectionPool _pool;

        public long Created => _created;
        public long Closed => _closed;
        public long FailedToCreate => _failedToCreate;

        public int ToCreate => _toCreate;
        public int ToClose => _toClose;

        public int InUse => _pool?.NumberOfInUseConnections ?? 0;
        public int Idle => _pool?.NumberOfIdleConnections ?? 0;

        private readonly Histogram _histogram;
        public IHistogram AcquisitionTimeHistogram => _histogram.Snapshot();

        public string UniqueName { get; }
        public string PoolStatus => _pool == null ? Internal.PoolStatus.Closed.Name : _pool.Status.Name;

        public ConnectionPoolMetrics(Uri uri, IConnectionPool pool, TimeSpan connAcquisitionTimeout)
        {
            UniqueName = uri.ToString();
            _pool = pool;
            _histogram = new Histogram(connAcquisitionTimeout.Ticks);
        }

        public void BeforeConnectionCreated()
        {
            Interlocked.Increment(ref _toCreate);
        }

        public void AfterConnectionCreatedSuccessfully()
        {
            Interlocked.Increment(ref _created);
            Interlocked.Decrement(ref _toCreate);
        }

        public void AfterConnectionFailedToCreate()
        {
            Interlocked.Increment(ref _failedToCreate);
            Interlocked.Decrement(ref _toCreate);
        }

        public void BeforeConnectionClosed()
        {
            Interlocked.Increment(ref _toClose);
        }

        public void AfterConnectionClosed()
        {
            Interlocked.Increment(ref _closed);
            Interlocked.Decrement(ref _toClose);
        }

        public void BeforeAcquire(IListenerEvent listenerEvent)
        {
            listenerEvent.Start();
        }

        public void AfterAcquire(IListenerEvent listenerEvent)
        {
            _histogram.RecordValue(listenerEvent.GetElapsed());
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
