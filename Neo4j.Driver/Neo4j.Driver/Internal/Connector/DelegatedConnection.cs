// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Auth;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.Connector;

internal abstract class DelegatedConnection : IConnection
{
    protected DelegatedConnection(IConnection connection)
    {
        Delegate = connection;
    }

    protected IConnection Delegate { get; set; }

    public AccessMode? Mode => Delegate.Mode;

    public string Database => Delegate.Database;

    public IDictionary<string, string> RoutingContext => Delegate.RoutingContext;

    public Task NotifyTokenExpiredAsync() => Delegate.NotifyTokenExpiredAsync();

    public async Task SyncAsync()
    {
        try
        {
            await Delegate.SyncAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await OnErrorAsync(e).ConfigureAwait(false);
        }
    }

    public async Task SendAsync()
    {
        try
        {
            await Delegate.SendAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await OnErrorAsync(e).ConfigureAwait(false);
        }
    }

    public async Task ReceiveOneAsync()
    {
        try
        {
            await Delegate.ReceiveOneAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await OnErrorAsync(e).ConfigureAwait(false);
        }
    }

    public BoltProtocolVersion Version => Delegate.Version;

    public void ConfigureMode(AccessMode? mode)
    {
        Delegate.ConfigureMode(mode);
    }

    public void Configure(string database, AccessMode? mode)
    {
        Delegate.Configure(database, mode);
    }

    public async Task InitAsync(
        INotificationsConfig notificationsConfig,
        SessionConfig sessionConfig = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Delegate.InitAsync(notificationsConfig, sessionConfig, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await OnErrorAsync(e).ConfigureAwait(false);
        }
    }

    public async Task ReAuthAsync(
        IAuthToken newAuthToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Delegate.ReAuthAsync(newAuthToken, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await OnErrorAsync(e).ConfigureAwait(false);
        }
    }

    public async Task EnqueueAsync(IRequestMessage message, IResponseHandler handler)
    {
        try
        {
            await Delegate.EnqueueAsync(message, handler).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await OnErrorAsync(e).ConfigureAwait(false);
        }
    }

    public void ClearQueue()
    {
        Delegate.ClearQueue();
    }

    public virtual bool IsOpen => Delegate.IsOpen;

    public IServerInfo Server => Delegate.Server;

    public IBoltProtocol BoltProtocol => Delegate.BoltProtocol;
    public bool ReAuthorizationRequired
    {
        get => Delegate.ReAuthorizationRequired;
        set => Delegate.ReAuthorizationRequired = value;
    }

    public bool UtcEncodedDateTime => Delegate.UtcEncodedDateTime;
    public IAuthToken AuthToken => Delegate.AuthToken;

    public void UpdateId(string newConnId)
    {
        Delegate.UpdateId(newConnId);
    }

    public void UpdateVersion(ServerVersion newVersion)
    {
        Delegate.UpdateVersion(newVersion);
    }

    public virtual Task DestroyAsync()
    {
        return Delegate.DestroyAsync();
    }

    public virtual Task CloseAsync()
    {
        return Delegate.CloseAsync();
    }

    public void SetReadTimeoutInSeconds(int seconds)
    {
        Delegate.SetReadTimeoutInSeconds(seconds);
    }

    public void SetUseUtcEncodedDateTime()
    {
        Delegate.SetUseUtcEncodedDateTime();
    }

    public SessionConfig SessionConfig
    {
        get => Delegate.SessionConfig;
        set => Delegate.SessionConfig = value;
    }

    public Task ValidateCredsAsync()
    {
        return Delegate.ValidateCredsAsync();
    }

    public Task LoginAsync(string userAgent, IAuthToken authToken, INotificationsConfig notificationsConfig)
    {
        return BoltProtocol.AuthenticateAsync(this, userAgent, authToken, notificationsConfig);
    }

    public Task LogoutAsync()
    {
        return BoltProtocol.LogoutAsync(this);
    }

    public Task ResetAsync()
    {
        return BoltProtocol.ResetAsync(this);
    }

    public Task<IReadOnlyDictionary<string, object>> GetRoutingTableAsync(
        string database,
        SessionConfig sessionConfig,
        Bookmarks bookmarks)
    {
        return BoltProtocol.GetRoutingTableAsync(this, database, sessionConfig, bookmarks);
    }

    public Task<IResultCursor> RunInAutoCommitTransactionAsync(
        AutoCommitParams autoCommitParams,
        INotificationsConfig notificationsConfig)
    {
        return BoltProtocol.RunInAutoCommitTransactionAsync(this, autoCommitParams, notificationsConfig);
    }

    public Task BeginTransactionAsync(
        string database,
        Bookmarks bookmarks,
        TransactionConfig config,
        SessionConfig sessionConfig,
        INotificationsConfig notificationsConfig)
    {
        return BoltProtocol.BeginTransactionAsync(
            this,
            database,
            bookmarks,
            config,
            sessionConfig,
            notificationsConfig);
    }

    public Task<IResultCursor> RunInExplicitTransactionAsync(Query query, bool reactive, long fetchSize)
    {
        return BoltProtocol.RunInExplicitTransactionAsync(this, query, reactive, fetchSize);
    }

    public Task CommitTransactionAsync(IBookmarksTracker bookmarksTracker)
    {
        return BoltProtocol.CommitTransactionAsync(this, bookmarksTracker);
    }

    public Task RollbackTransactionAsync()
    {
        return BoltProtocol.RollbackTransactionAsync(this);
    }

    internal virtual async Task OnErrorAsync(Exception error)
    {
        if (error is TokenExpiredException te)
        {
            ReAuthorizationRequired = true;
            if (te.Notified == false)
            {
                await NotifyTokenExpiredAsync().ConfigureAwait(false);
                te.Notified = true;
            }
        }
        else if (error is AuthorizationException)
        {
            ReAuthorizationRequired = true;
        }
    }

    public override string ToString()
    {
        return Delegate.ToString();
    }
}
