// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Diagnostics;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Connector
{
    internal class PooledConnection : DelegatedConnection, IPooledConnection
    {
        private readonly IConnectionReleaseManager _releaseManager;
        private readonly IConnectionListener _connMetricsListener;
        private readonly IListenerEvent _connEvent;

        public PooledConnection(IConnection conn, IConnectionReleaseManager releaseManager = null,
            IConnectionListener connMetricsListener = null)
            : base(conn)
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

        public async Task ClearConnectionAsync()
        {
            await ResetAsync().ConfigureAwait(false);
            await SyncAsync().ConfigureAwait(false);
        }

        public void OnAcquire()
        {
            _connMetricsListener?.ConnectionAcquired(_connEvent);
        }

        public void OnRelease()
        {
            _connMetricsListener?.ConnectionReleased(_connEvent);
        }

        public override bool IsOpen => Delegate.IsOpen && !HasUnrecoverableError;

        public override Task DestroyAsync()
        {
            // stops the timmer
            IdleTimer.Reset();
            LifetimeTimer.Reset();

            return base.DestroyAsync();
        }

        public override Task CloseAsync()
        {
            return _releaseManager?.ReleaseAsync(this);
        }

        /// <summary>
        /// Return true if unrecoverable error has been received on this connection, otherwise false.
        /// The connection that has been marked as has unrecoverable errors will be eventually closed when returning back to the pool.
        /// </summary>
        internal bool HasUnrecoverableError { private set; get; }

        public override Task OnErrorAsync(Exception error)
        {
            if (!error.IsRecoverableError())
            {
                HasUnrecoverableError = true;
            }

            if (error is Neo4jException)
            {
                return Task.FromException(error);
            }

            if (error.IsConnectionError())
            {
                return Task.FromException(new ServiceUnavailableException(
                    $"Connection with the server breaks due to {error.GetType().Name}: {error.Message}", error));
            }
            else
            {
                return Task.FromException(error);
            }
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