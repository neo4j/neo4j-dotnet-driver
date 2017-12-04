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
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal partial class Session
    {
        public T ReadTransaction<T>(Func<ITransaction, T> work)
        {
            return RunTransaction(AccessMode.Read, work);
        }

        public T ReadTransaction<T>(Func<ITransaction, T> work, TimeSpan timeout)
        {
            return RunTransaction(AccessMode.Read, work, timeout);
        }

        public void ReadTransaction(Action<ITransaction> work)
        {
            RunTransaction(AccessMode.Read, work);
        }

        public void ReadTransaction(Action<ITransaction> work, TimeSpan timeout)
        {
            RunTransaction(AccessMode.Read, work, timeout);
        }

        public T WriteTransaction<T>(Func<ITransaction, T> work)
        {
            return RunTransaction(AccessMode.Write, work);
        }

        public T WriteTransaction<T>(Func<ITransaction, T> work, TimeSpan timeout)
        {
            return RunTransaction(AccessMode.Write, work, timeout);
        }

        public void WriteTransaction(Action<ITransaction> work)
        {
            RunTransaction(AccessMode.Write, work);
        }

        public void WriteTransaction(Action<ITransaction> work, TimeSpan timeout)
        {
            RunTransaction(AccessMode.Write, work, timeout);
        }

        private void RunTransaction(AccessMode mode, Action<ITransaction> work, TimeSpan? timeout = null)
        {
            RunTransaction<object>(mode, tx=>
            {
                work(tx);
                return null;
            }, timeout);
        }

        private T RunTransaction<T>(AccessMode mode, Func<ITransaction, T> work, TimeSpan? timeout = null)
        {
            return TryExecute(() => _retryLogic.Retry(() =>
            {
                using (var tx = BeginTransactionWithoutLogging(mode, timeout))
                {
                    try
                    {
                        var result = work(tx);
                        tx.Success();
                        return result;
                    }
                    catch
                    {
                        tx.Failure();
                        throw;
                    }
                }
            }));
        }

        // Async
        public Task<T> ReadTransactionAsync<T>(Func<ITransaction, Task<T>> work)
        {
            return RunTransactionAsync(AccessMode.Read, work);
        }

        public Task ReadTransactionAsync(Func<ITransaction, Task> work, TimeSpan timeout)
        {
            return RunTransactionAsync(AccessMode.Read, work, timeout);
        }

        public Task ReadTransactionAsync(Func<ITransaction, Task> work)
        {
            return RunTransactionAsync(AccessMode.Read, work);
        }

        public Task<T> ReadTransactionAsync<T>(Func<ITransaction, Task<T>> work, TimeSpan timeout)
        {
            return RunTransactionAsync(AccessMode.Read, work, timeout);
        }

        public Task<T> WriteTransactionAsync<T>(Func<ITransaction, Task<T>> work)
        {
            return RunTransactionAsync(AccessMode.Write, work);
        }

        public Task<T> WriteTransactionAsync<T>(Func<ITransaction, Task<T>> work, TimeSpan timeout)
        {
            return RunTransactionAsync(AccessMode.Write, work, timeout);
        }

        public Task WriteTransactionAsync(Func<ITransaction, Task> work)
        {
            return RunTransactionAsync(AccessMode.Write, work);
        }

        public Task WriteTransactionAsync(Func<ITransaction, Task> work, TimeSpan timeout)
        {
            return RunTransactionAsync(AccessMode.Write, work, timeout);
        }

        private Task RunTransactionAsync(AccessMode mode, Func<ITransaction, Task> work, TimeSpan? timeout = null)
        {
            return RunTransactionAsync(mode, async tx =>
            {
                await work(tx).ConfigureAwait(false);
                var ignored = 1;
                return ignored;
            }, timeout);
        }

        private Task<T> RunTransactionAsync<T>(AccessMode mode, Func<ITransaction, Task<T>> work, TimeSpan? timeout = null)
        {
            return TryExecuteAsync(async () => await _retryLogic.RetryAsync(async () =>
            {
                var tx = await BeginTransactionWithoutLoggingAsync(mode, timeout).ConfigureAwait(false);
                {
                    try
                    {
                        var result = await work(tx).ConfigureAwait(false);
                        await tx.CommitAsync().ConfigureAwait(false);
                        return result;
                    }
                    catch
                    {
                        await tx.RollbackAsync().ConfigureAwait(false);
                        throw;
                    }
                }
            }).ConfigureAwait(false));
        }
    }
}
