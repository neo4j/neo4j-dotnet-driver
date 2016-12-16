// Copyright (c) 2002-2016 "Neo Technology,"
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
using System.Threading;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal abstract class BaseDriver : IDriver
    {
        public abstract ISession NewSession(AccessMode mode);
        public abstract void ReleaseUnmanagedResources();
        public abstract Uri Uri { get; }

        private const int True = 1;
        private const int False = 0;
        private volatile int _isDisposed = False;

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }

            // protect the driver from concurrent calling on Dispose
#pragma warning disable 420
            // The compiler warnning is ignored as we are locking on the volatile anyway,
            // so it is safe to use the volatile as ref
            if (Interlocked.CompareExchange(ref _isDisposed, True, False) == True)
#pragma warning restore 420
            {
                // the driver is already disposed
                // calling dispose method many times will not raise any exceptions
                return;
            }
            ReleaseUnmanagedResources();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ISession Session()
        {
            return Session(AccessMode.Write);
        }

        public ISession Session(AccessMode mode)
        {
            if (_isDisposed == True)
            {
                ThrowDriverClosedException();
            }

            var session = NewSession(mode);

            if (_isDisposed == True)
            {
                session.Dispose();
                ThrowDriverClosedException();
            }
            return session;
        }

        private void ThrowDriverClosedException()
        {
            throw new ObjectDisposedException(GetType().Name, "Cannot open a new session on a driver that is already disposed.");
        }
    }
}