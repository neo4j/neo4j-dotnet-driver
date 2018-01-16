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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal class PooledConnection : DelegatedConnection, IPooledConnection
    {
        private readonly IConnectionReleaseManager _releaseManager;
        private readonly IConnectionListener _connMetricsListener;
        private readonly IListenerEvent _connEvent;

        public PooledConnection(IConnection conn, IConnectionReleaseManager releaseManager = null, IConnectionListener connMetricsListener = null)
            :base (conn)
        {
            _releaseManager = releaseManager;
            // IdleTimer starts to count when the connection is put back to the pool.
            IdleTimer = new StopwatchBasedTimer();
            // LifetimeTimer starts to count once the connection is created.
            LifetimeTimer = new StopwatchBasedTimer();
            LifetimeTimer.Start();

            _connMetricsListener = connMetricsListener;
            if (_connMetricsListener != null)
            {
                _connEvent = new SimpleTimerEvent();
            }
        }
        public Guid Id { get; } = Guid.NewGuid();

        public void ClearConnection()
        {
            Reset();
            Sync();
        }

        public Task ClearConnectionAsync()
        {
            Reset();
            return SyncAsync();
        }

        public void OnRequire()
        {
            _connMetricsListener?.OnAcquire(_connEvent);
        }

        public void OnRelease()
        {
            _connMetricsListener?.OnRelease(_connEvent);
        }

        public override bool IsOpen => Delegate.IsOpen && !HasUnrecoverableError;

        public override void Destroy()
        {
            // stops the timmer
            IdleTimer.Reset();
            LifetimeTimer.Reset();
            base.Destroy();
        }

        public override Task DestroyAsync()
        {
            // stops the timmer
            IdleTimer.Reset();
            LifetimeTimer.Reset();

            return base.DestroyAsync();
        }

        public override void Close()
        {
            _releaseManager?.Release(this);
        }

        public override Task CloseAsync()
        {
            return _releaseManager?.ReleaseAsync(this);
        }

        /// <summary>
        /// Return true if unrecoverable error has been received on this connection, otherwise false.
        /// The connection that has been marked as has unrecoverable errors will be eventally closed when returning back to the pool.
        /// </summary>
        internal bool HasUnrecoverableError { private set; get; }

        public override void OnError(Exception error)
        {
            if (error.IsRecoverableError())
            {
                Delegate.AckFailure();
            }
            else
            {
                HasUnrecoverableError = true;
            }

            if (error.IsConnectionError())
            {
                throw new ServiceUnavailableException(
                    $"Connection with the server breaks due to {error.GetType().Name}: {error.Message}", error);
            }
            else
            {
                throw error;
            }
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        public ITimer IdleTimer { get; }
        public ITimer LifetimeTimer { get; }
    }

    internal class StopwatchBasedTimer : ITimer
    {
        private readonly Stopwatch _stopwatch;
        public StopwatchBasedTimer()
        {
            _stopwatch = new Stopwatch();
        }

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
        public void Reset()
        {
            _stopwatch.Reset();
        }

        public void Start()
        {
            _stopwatch.Start();
        }
    }
}
