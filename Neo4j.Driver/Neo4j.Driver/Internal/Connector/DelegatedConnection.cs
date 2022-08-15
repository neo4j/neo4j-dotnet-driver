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
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
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
            RoutingContext = connection.RoutingContext;
        }

        public AccessMode? Mode
        {
            get => Delegate.Mode;
            set => Delegate.Mode = value;
        }

        public string Database
        {
            get => Delegate.Database;
            set => Delegate.Database = value;
        }

        public IDictionary<string, string> RoutingContext { get; set; }
        
        public virtual Task OnErrorAsync(Exception error)
        {
            return Task.CompletedTask;
        }

        public Task SyncAsync()
        {
            return TaskWithErrorHandling(() => Delegate.SyncAsync());
        }

        public Task SendAsync()
        {
            return TaskWithErrorHandling(() => Delegate.SendAsync());
        }

        public Task ReceiveOneAsync()
        {
            return TaskWithErrorHandling(() => Delegate.ReceiveOneAsync());
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

		public void SetRecvTimeOut(int seconds)
		{
			Delegate.SetRecvTimeOut(seconds);
		}

        public void SetUseUtcEncodedDateTime()
        {
            Delegate.SetUseUtcEncodedDateTime();
        }
    }
}