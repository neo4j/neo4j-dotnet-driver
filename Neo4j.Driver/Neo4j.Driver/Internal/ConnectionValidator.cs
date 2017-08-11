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
            return connection.IsOpen && !HasBeenIdleForTooLong(connection);
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
            connection.IdleTimer.Reset();
            return false;
        }
    }
}