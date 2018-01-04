// Copyright (c) 2002-2018 "Neo Technology,"
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
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal abstract class LoggerBase : IDisposable
    {
        protected ILogger Logger { get; private set; }

        protected LoggerBase(ILogger logger)
        {
            Logger = logger;
        }

        protected void TryExecute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger?.Error(ex.Message, ex);
                throw;
            }
        }

        protected T TryExecute<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Logger?.Error(ex.Message, ex);
                throw;
            }
        }

        protected async Task TryExecuteAsync(Func<Task> func)
        {
            try
            {
                await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger?.Error(ex.Message, ex);
                throw;
            }
        }

        protected async Task<T> TryExecuteAsync<T>(Func<Task<T>> func)
        {
            try
            {
                return await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger?.Error(ex.Message, ex);
                throw;
            }
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Logger = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
