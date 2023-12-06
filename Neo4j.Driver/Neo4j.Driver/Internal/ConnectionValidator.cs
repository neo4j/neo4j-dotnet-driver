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
    AcquireStatus GetConnectionLifetimeStatus(IPooledConnection connection);
}

internal class ConnectionValidator : IConnectionValidator
{
    private readonly long _connIdleTimeout;
    private readonly long _livenessTimeout;
    private readonly long _maxConnLifetime;

    public ConnectionValidator(
        TimeSpan connIdleTimeout,
        TimeSpan maxConnLifetime,
        TimeSpan? livenessCheckTimeout = null)
    {
        _connIdleTimeout = connIdleTimeout >= TimeSpan.Zero ? (long)connIdleTimeout.TotalMilliseconds : long.MaxValue;
        _maxConnLifetime = maxConnLifetime >= TimeSpan.Zero ? (long)maxConnLifetime.TotalMilliseconds : long.MaxValue;
        _livenessTimeout = livenessCheckTimeout.HasValue
            ? (long)livenessCheckTimeout.Value.TotalMilliseconds
            : long.MaxValue;
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

    public AcquireStatus GetConnectionLifetimeStatus(IPooledConnection connection)
    {
        var idleTime = connection?.IdleTimer.ElapsedMilliseconds ?? 0L;

        var isRequirable = connection.IsOpen &&
            !HasBeenIdleForTooLong(idleTime) &&
            !HasBeenAliveForTooLong(connection) &&
            !MarkedStale(connection) &&
            AuthStatusIsRecoverable(connection);

        if (!isRequirable)
        {
            return AcquireStatus.Unhealthy;
        }

        ResetIdleTimer(connection);

        return idleTime >= _livenessTimeout ? AcquireStatus.RequiresLivenessProbe : AcquireStatus.Healthy;
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
        if (_connIdleTimeout < long.MaxValue)
        {
            connection.IdleTimer.Start();
        }
    }

    private void ResetIdleTimer(IPooledConnection connection)
    {
        if (_connIdleTimeout < long.MaxValue)
        {
            connection.IdleTimer.Reset();
        }
    }

    private bool HasBeenAliveForTooLong(IPooledConnection connection)
    {
        return connection.LifetimeTimer.ElapsedMilliseconds > _maxConnLifetime;
    }

    private bool HasBeenIdleForTooLong(double connectionIdleTime)
    {
        return connectionIdleTime > _connIdleTimeout;
    }
}
