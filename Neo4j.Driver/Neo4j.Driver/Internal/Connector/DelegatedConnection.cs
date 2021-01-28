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
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal abstract class DelegatedConnection : IConnection
    {
        protected IConnection Delegate { get; set; }

        protected DelegatedConnection(IConnection connection)
        {
            Delegate = connection;
        }

        public AccessMode? Mode
        {
            get => Delegate.Mode;
            set => Delegate.Mode = value;
        }

        public abstract void OnError(Exception error);

        public virtual Task OnErrorAsync(Exception error)
        {
            OnError(error);
            return TaskHelper.GetCompletedTask();
        }

        public void Sync()
        {
            try
            {
                Delegate.Sync();
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        public Task SyncAsync()
        {
            return TaskWithErrorHandling(() => Delegate.SyncAsync());
        }

        public void Send()
        {
            try
            {
                Delegate.Send();
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        public Task SendAsync()
        {
            return TaskWithErrorHandling(() => Delegate.SendAsync());
        }

        public void ReceiveOne()
        {
            try
            {
                Delegate.ReceiveOne();
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        public Task ReceiveOneAsync()
        {
            return TaskWithErrorHandling(() => Delegate.ReceiveOneAsync());
        }

        public void Init()
        {
            try
            {
                Delegate.Init();
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        public Task InitAsync()
        {
            return TaskWithErrorHandling(() => Delegate.InitAsync());
        }

        public void Enqueue(IRequestMessage message1, IMessageResponseCollector responseCollector,
            IRequestMessage message2 = null)
        {
            try
            {
                Delegate.Enqueue(message1, responseCollector, message2);
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        public void Reset()
        {
            try
            {
                Delegate.Reset();
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        public virtual bool IsOpen => Delegate.IsOpen;

        public IServerInfo Server => Delegate.Server;
        public IBoltProtocol BoltProtocol => Delegate.BoltProtocol;

        public void ResetMessageReaderAndWriterForServerV3_1()
        {
            Delegate.ResetMessageReaderAndWriterForServerV3_1();
        }

        public void UpdateId(string newConnId)
        {
            Delegate.UpdateId(newConnId);
        }

        public virtual void Destroy()
        {
            Delegate.Destroy();
        }

        public virtual Task DestroyAsync()
        {
            return Delegate.DestroyAsync();
        }

        public virtual void Close()
        {
            Delegate.Close();
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
    }
}