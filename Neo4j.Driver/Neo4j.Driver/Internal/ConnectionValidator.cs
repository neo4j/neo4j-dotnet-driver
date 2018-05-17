// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal
{
    internal interface IConnectionValidator
    {
        bool IsConnectionReusable(IPooledConnection connection);
        Task<bool> IsConnectionReusableAsync(IPooledConnection connection);
        bool IsValid(IPooledConnection connection);
    }

    internal class ConnectionValidator : IConnectionValidator
    {
        private readonly TimeSpan _connIdleTimeout;
        private readonly TimeSpan _maxConnLifetime;

        public ConnectionValidator(TimeSpan connIdleTimeout, TimeSpan maxConnLifetime)
        {
            _connIdleTimeout = connIdleTimeout;
            _maxConnLifetime = maxConnLifetime;
        }

        public bool IsConnectionReusable(IPooledConnection connection)
        {
            if (!connection.IsOpen)
            {
                return false;
            }

            try
            {
                connection.ClearConnection();
            }
            catch
            {
                return false;
            }

            RestartIdleTimer(connection);

            return true;
        }

        public async Task<bool> IsConnectionReusableAsync(IPooledConnection connection)
        {
            if (!connection.IsOpen)
            {
                return false;
            }

            try
            {
                await connection.ClearConnectionAsync().ConfigureAwait(false);
            }
            catch
            {
                return false;
            }

            RestartIdleTimer(connection);

            return true;
        }

        private void RestartIdleTimer(IPooledConnection connection)
        {
            if (IsConnectionIdleDetectionEnabled())
            {
                connection.IdleTimer.Start();
            }
        }

        private bool IsConnectionIdleDetectionEnabled()
        {
            return _connIdleTimeout.TotalMilliseconds >= 0;
        }

        public bool IsValid(IPooledConnection connection)
        {
            return connection.IsOpen 
                && !HasBeenIdleForTooLong(connection) 
                && !HasBeenAliveForTooLong(connection);
        }

        // I like this method name :P
        private bool HasBeenAliveForTooLong(IPooledConnection connection)
        {
            if (!IsConnectionLifetimeDetectionEnabled())
            {
                return false;
            }
            if (connection.LifetimeTimer.ElapsedMilliseconds > _maxConnLifetime.TotalMilliseconds)
            {
                return true;
            }

            return false;
        }

        private bool IsConnectionLifetimeDetectionEnabled()
        {
            return _maxConnLifetime.TotalMilliseconds >= 0;
        }

        private bool HasBeenIdleForTooLong(IPooledConnection connection)
        {
            if (!IsConnectionIdleDetectionEnabled())
            {
                return false;
            }
            if (connection.IdleTimer.ElapsedMilliseconds > _connIdleTimeout.TotalMilliseconds)
            {
                return true;
            }

            // if it has not been idle for too long, then it is good to be claimed.
            // And we will stop the timmer to prepare it to be claimed now
            connection.IdleTimer.Reset();
            return false;
        }
    }
}
