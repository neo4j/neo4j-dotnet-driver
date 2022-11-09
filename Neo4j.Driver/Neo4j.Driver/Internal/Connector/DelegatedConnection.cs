// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Util;
using System.Collections.Generic;
using System.Threading;

namespace Neo4j.Driver.Internal.Connector
{
    internal abstract class DelegatedConnection : IConnection
    {
        protected IConnection Delegate { get; set; }

        protected DelegatedConnection(IConnection connection)
        {
            Delegate = connection;
        }

        public AccessMode? Mode => Delegate.Mode;

        public string Database => Delegate.Database;

        public IDictionary<string, string> RoutingContext => Delegate.RoutingContext;
        
        public virtual Task OnErrorAsync(Exception error)
        {
            return Task.CompletedTask;
        }

        public Task SyncAsync()
        {
            return TaskWithErrorHandling(Delegate.SyncAsync);
        }

        public Task SendAsync()
        {
            return TaskWithErrorHandling(Delegate.SendAsync);
        }

        public Task ReceiveOneAsync()
        {
            return TaskWithErrorHandling(Delegate.ReceiveOneAsync);
        }

        public BoltProtocolVersion Version => Delegate.Version;

        public void Configure(string database, AccessMode? mode)
        {
            Delegate.Configure(database, mode);
        }

        public Task InitAsync(CancellationToken cancellationToken = default)
        {
            return TaskWithErrorHandling(() => Delegate.InitAsync(cancellationToken));
        }

        public Task EnqueueAsync(IRequestMessage message1, IResponseHandler handler1,
            IRequestMessage message2 = null, IResponseHandler handler2 = null)
        {
            try
            {
                return Delegate.EnqueueAsync(message1, handler1, message2, handler2);
            }
            catch (Exception e)
            {
                return OnErrorAsync(e);
            }
        }

        public Task ResetAsync()
        {
            try
            {
                return Delegate.ResetAsync();
            }
            catch (Exception e)
            {
                return OnErrorAsync(e);
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

        internal async Task TaskWithErrorHandling(Func<Task> task)
        {
            try
            {
                await task().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await OnErrorAsync(e).ConfigureAwait(false);
            }
        }

        public override string ToString()
        {
            return Delegate.ToString();
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
            return Delegate.LoginAsync(userAgent, authToken);
        }

        public Task LogoutAsync()
        {
            return Delegate.LogoutAsync();
        }

        public Task<IReadOnlyDictionary<string, object>> GetRoutingTable(string database, string impersonatedUser, Bookmarks bookmarks)
        {
            return Delegate.GetRoutingTable(database, impersonatedUser, bookmarks);
        }

        public Task<IResultCursor> RunInAutoCommitTransactionAsync(AutoCommitParams autoCommitParams)
        {
            return Delegate.RunInAutoCommitTransactionAsync(autoCommitParams);
        }

        public Task BeginTransactionAsync(string database, Bookmarks bookmarks, TransactionConfig config, string impersonatedUser)
        {
            return Delegate.BeginTransactionAsync(database, bookmarks, config, impersonatedUser);
        }

        public Task<IResultCursor> RunInExplicitTransactionAsync(Query query, bool reactive, long fetchSize)
        {
            return Delegate.RunInExplicitTransactionAsync(query, reactive, fetchSize);
        }

        public Task CommitTransactionAsync(IBookmarksTracker bookmarksTracker)
        {
            return Delegate.CommitTransactionAsync(bookmarksTracker);
        }

        public Task RollbackTransactionAsync()
        {
            return Delegate.RollbackTransactionAsync();
        }
    }
}