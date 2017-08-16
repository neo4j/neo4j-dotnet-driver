using System;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal
{
    internal class ConnectionValidator
    {
        private readonly TimeSpan _connIdleTimeout;
        private readonly TimeSpan _maxConnLifeTime;

        public ConnectionValidator(TimeSpan connIdleTimeout, TimeSpan maxConnLifeTime)
        {
            _connIdleTimeout = connIdleTimeout;
            _maxConnLifeTime = maxConnLifeTime;
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
            if (!IsConnectionLifeTimeDetectionEnabled())
            {
                return false;
            }
            if (connection.LifeTimeTimer.ElapsedMilliseconds > _maxConnLifeTime.TotalMilliseconds)
            {
                return true;
            }

            return false;
        }

        private bool IsConnectionLifeTimeDetectionEnabled()
        {
            return _maxConnLifeTime.TotalMilliseconds >= 0;
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