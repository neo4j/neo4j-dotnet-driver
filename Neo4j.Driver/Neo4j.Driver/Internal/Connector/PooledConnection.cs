﻿// Copyright (c) "Neo4j"
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
using System.Diagnostics;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Services;

namespace Neo4j.Driver.Internal.Connector;

internal class PooledConnection : DelegatedConnection, IPooledConnection
{
    private readonly IConnectionReleaseManager _releaseManager;
    private bool _staleCredentials;

    public PooledConnection(
        IConnection conn,
        IConnectionReleaseManager releaseManager = null)
        : base(conn)
    {
        _releaseManager = releaseManager;
        
        // IdleTimer starts to count when the connection is put back to the pool.
        IdleTimer = DateTimeProvider.StaticInstance.NewTimer();
        // LifetimeTimer starts to count once the connection is created.
        LifetimeTimer = DateTimeProvider.StaticInstance.NewTimer();
        LifetimeTimer.Start();
    }

    /// <summary>
    /// Return true if unrecoverable error has been received on this connection, otherwise false. The connection that
    /// has been marked as has unrecoverable errors will be eventually closed when returning back to the pool. <br/><br/>
    /// </summary>
    internal bool HasUnrecoverableError { get; private set; }

    public async Task ClearConnectionAsync()
    {
        await ResetAsync().ConfigureAwait(false);
        await SyncAsync().ConfigureAwait(false);
    }

    public override bool IsOpen => Delegate.IsOpen && !HasUnrecoverableError;

    public override Task DestroyAsync()
    {
        // stops the timer
        IdleTimer.Reset();
        LifetimeTimer.Reset();

        return base.DestroyAsync();
    }

    public override Task CloseAsync()
    {
        if (_releaseManager == null)
        {
            return Task.CompletedTask;
        }

        return _releaseManager.ReleaseAsync(this);
    }

    public ITimer IdleTimer { get; }
    public ITimer LifetimeTimer { get; }

    public bool StaleCredentials
    {
        get => _staleCredentials;
        set
        {
            if (value)
            {
                HasUnrecoverableError = true;
            }

            _staleCredentials = value;
        }
    }

    internal override Task OnErrorAsync(Exception error)
    {
        if (!(error.IsRecoverableError() || SupportsReauth(error)))
        {
            HasUnrecoverableError = true;
        }

        if (error is Neo4jException)
        {
            _releaseManager.OnPoolMemberException(this, error);
            throw error;
        }

        if (error.IsConnectionError())
        {
            throw new ServiceUnavailableException(
                $"Connection with the server breaks due to {error.GetType().Name}: {error.Message} " +
                "Please ensure that your database is listening on the correct host and port " +
                "and that you have compatible encryption settings both on Neo4j server and driver. " +
                "Note that the default encryption setting has changed in Neo4j 4.0.",
                error);
        }

        throw error;
    }

    private bool SupportsReauth(Exception error)
    {
        return AuthorizationStatus != AuthorizationStatus.None &&
            Version >= BoltProtocolVersion.V5_1 &&
            error is AuthorizationException or TokenExpiredException;
    }
}

internal class StopwatchBasedTimer : ITimer
{
    private readonly Stopwatch _stopwatch = new();

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
