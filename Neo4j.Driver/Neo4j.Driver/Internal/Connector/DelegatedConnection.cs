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

    public void Configure(string database, AccessMode? mode)
    {
        Delegate.Configure(database, mode);
    }

    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Delegate.InitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await OnErrorAsync(e).ConfigureAwait(false);
        }
    }

    public async Task EnqueueAsync(
        IRequestMessage message1,
        IResponseHandler handler1,
        IRequestMessage message2 = null,
        IResponseHandler handler2 = null)
    {
        try
        {
            await Delegate.EnqueueAsync(message1, handler1, message2, handler2).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await OnErrorAsync(e).ConfigureAwait(false);
        }
    }

    public virtual bool IsOpen => Delegate.IsOpen;

    public IServerInfo Server => Delegate.Server;
    public IBoltProtocol BoltProtocol => Delegate.BoltProtocol;

    public bool UtcEncodedDateTime => Delegate.UtcEncodedDateTime;

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

    public Task LoginAsync(string userAgent, IAuthToken authToken)
    {
        return BoltProtocol.LoginAsync(this, userAgent, authToken);
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
        string impersonatedUser,
        Bookmarks bookmarks)
    {
        return BoltProtocol.GetRoutingTableAsync(this, database, impersonatedUser, bookmarks);
    }

    public Task<IResultCursor> RunInAutoCommitTransactionAsync(AutoCommitParams autoCommitParams)
    {
        return BoltProtocol.RunInAutoCommitTransactionAsync(this, autoCommitParams);
    }

    public Task BeginTransactionAsync(
        string database,
        Bookmarks bookmarks,
        TransactionConfig config,
        string impersonatedUser)
    {
        return BoltProtocol.BeginTransactionAsync(this, database, bookmarks, config, impersonatedUser);
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

    internal virtual Task OnErrorAsync(Exception error)
    {
        return Task.CompletedTask;
    }
    
    public override string ToString()
    {
        return Delegate.ToString();
    }
}
