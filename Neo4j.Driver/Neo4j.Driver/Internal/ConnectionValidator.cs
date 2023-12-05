// Copyright (c) "Neo4j"
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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Internal;

internal interface IConnectionValidator
{
    /// <summary>Healthy check on the connection before pooling</summary>
    /// <param name="connection">The connection to be checked.</param>
    /// <returns>True if the connection is good to be pooled, otherwise false.</returns>
    Task<bool> OnReleaseAsync(IPooledConnection connection);

    /// <summary>Healthy check before lending the connection outside the pool.</summary>
    /// <param name="connection">The connection to be checked.</param>
    /// <returns>True if the connection is in a good state to be used by transactions and sessions, otherwise false.</returns>
    Task<bool> OnRequireAsync(IPooledConnection connection);
}

internal class ConnectionValidator : IConnectionValidator
{
    private readonly TimeSpan _connIdleTimeout;
    private readonly TimeSpan _maxConnLifetime;
    private readonly TimeSpan? _connLivenessCheckTimeout;
    private readonly ILogger _logger;

    public ConnectionValidator(DriverContext driverContext)
    {
        _connIdleTimeout = driverContext.Config.ConnectionIdleTimeout;
        _maxConnLifetime = driverContext.Config.MaxConnectionLifetime;
        _connLivenessCheckTimeout = driverContext.Config.ConnectionLivenessCheckTimeout;
        _logger = driverContext.Logger;
    }

    public async Task<bool> OnReleaseAsync(IPooledConnection connection)
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

        if (!AuthStatusIsRecoverable(connection))
        {
            return false;
        }

        RestartIdleTimer(connection);

        return true;
    }

    public async Task<bool> OnRequireAsync(IPooledConnection connection)
    {
        var isRequirable = connection.IsOpen &&
            !HasBeenIdleForTooLong(connection) &&
            !HasBeenAliveForTooLong(connection) &&
            !MarkedStale(connection) &&
            AuthStatusIsRecoverable(connection) &&
            await LivenessCheckPassed(connection).ConfigureAwait(false);

        if (isRequirable)
        {
            ResetIdleTimer(connection);
        }

        return isRequirable;
    }

    private bool AuthStatusIsRecoverable(IConnection connection)
    {
        return connection.AuthorizationStatus is AuthorizationStatus.FreshlyAuthenticated
                or AuthorizationStatus.Pooled ||
            connection.SupportsReAuth();
    }

    private bool MarkedStale(IPooledConnection connection)
    {
        return connection.StaleCredentials;
    }

    private void RestartIdleTimer(IPooledConnection connection)
    {
        if (IsTimeoutDetectionEnabled(_connIdleTimeout))
        {
            connection.IdleTimer.Start();
        }
    }

    private void ResetIdleTimer(IPooledConnection connection)
    {
        if (IsTimeoutDetectionEnabled(_connIdleTimeout))
        {
            connection.IdleTimer.Reset();
        }
    }

    private bool HasBeenAliveForTooLong(IPooledConnection connection)
    {
        if (IsTimeoutDetectionDisabled(_maxConnLifetime))
        {
            return false;
        }

        if (connection.LifetimeTimer.ElapsedMilliseconds > _maxConnLifetime.TotalMilliseconds)
        {
            return true;
        }

        return false;
    }

    private bool HasBeenIdleForTooLong(IPooledConnection connection)
    {
        if (IsTimeoutDetectionDisabled(_connIdleTimeout))
        {
            return false;
        }

        if (connection.IdleTimer.ElapsedMilliseconds > _connIdleTimeout.TotalMilliseconds)
        {
            return true;
        }

        return false;
    }

    private static bool IsTimeoutDetectionEnabled(TimeSpan timeout) => timeout.TotalMilliseconds >= 0;

    private static bool IsTimeoutDetectionDisabled(TimeSpan timeout) => !IsTimeoutDetectionEnabled(timeout);

    private async Task<bool> LivenessCheckPassed(IPooledConnection connection)
    {
        if (_connLivenessCheckTimeout is not null &&
            connection.IdleTimer.ElapsedMilliseconds > _connLivenessCheckTimeout.Value.TotalMilliseconds)
        {
            _logger.Debug(
                "Connection has been idle for {0}ms, performing liveness check.",
                connection.IdleTimer.ElapsedMilliseconds);
            await connection.ResetAsync().ConfigureAwait(false);
            ResetIdleTimer(connection);
        }

        return true;
    }
}
