// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Threading;
using System.Threading.Tasks;
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

        public abstract void OnError(Exception error);

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
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                // TODO: Create a helper for this pattern
                Delegate.SyncAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        try
                        {
                            OnError(t.Exception);
                        }
                        catch (AggregateException exc)
                        {
                            tcs.SetException(exc.GetBaseException());
                        }
                        catch (Exception exc)
                        {
                            tcs.SetException(exc);
                        }
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        tcs.SetResult(true);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception e)
            {
                OnError(e);
            }

            return tcs.Task;
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
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                // TODO: Create a helper for this pattern
                Delegate.SendAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        try
                        {
                            OnError(t.Exception);
                        }
                        catch (AggregateException exc)
                        {
                            tcs.SetException(exc.GetBaseException());
                        }
                        catch (Exception exc)
                        {
                            tcs.SetException(exc);
                        }
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        tcs.SetResult(true);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception e)
            {
                OnError(e);
            }

            return tcs.Task;
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

        public Task ReceiveOneAsync(CancellationToken ctx = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                Delegate.ReceiveOneAsync(ctx).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        try
                        {
                            OnError(t.Exception);
                        }
                        catch (AggregateException exc)
                        {
                            tcs.SetException(exc.GetBaseException());
                        }
                        catch (Exception exc)
                        {
                            tcs.SetException(exc);
                        }
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        tcs.SetResult(true);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception e)
            {
                OnError(e);
            }

            return tcs.Task;
        }

        public void Run(string statement, IDictionary<string, object> parameters = null, IMessageResponseCollector resultBuilder = null,
            bool pullAll = true)
        {
            try
            {
                Delegate.Run(statement, parameters, resultBuilder, pullAll);
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        public virtual bool IsOpen => Delegate.IsOpen;

        public IServerInfo Server => Delegate.Server;

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
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                Delegate.InitAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        try
                        {
                            OnError(t.Exception);
                        }
                        catch (AggregateException exc)
                        {
                            tcs.SetException(exc.GetBaseException());
                        }
                        catch (Exception exc)
                        {
                            tcs.SetException(exc);
                        }
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        tcs.SetResult(true);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception e)
            {
                OnError(e);
            }

            return tcs.Task;
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

        public void AckFailure()
        {
            try
            {
                Delegate.AckFailure();
            }
            catch (Exception e)
            {
                OnError(e);
            }
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
    }
}
