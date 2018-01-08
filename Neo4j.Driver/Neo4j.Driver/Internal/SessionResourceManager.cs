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
// See the License for the specific

using System;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal partial class Session : IResultResourceHandler, ITransactionResourceHandler
    {
        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }

            TryExecute(() =>
            {
                if (_isOpen)
                {
                    // This will protect the session being disposed twice
                    _isOpen = false;
                    DisposeTransaction();
                    DisposeSessionResult();
                }
            });
            base.Dispose(true);
        }

        public Task CloseAsync()
        {
            return TryExecuteAsync(async () =>
            {
                if (_isOpen)
                {
                    // This will protect the session being disposed twice
                    _isOpen = false;
                    await DisposeTransactionAsync().ConfigureAwait(false);
                    await DisposeSessionResultAsync().ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        ///  This method will be called back by <see cref="ResultBuilder"/> after it consumed result
        /// </summary>
        public void OnResultConsumed()
        {
            Throw.ArgumentNullException.IfNull(_connection, nameof(_connection));
            DisposeConnection();
        }

        public Task OnResultConsumedAsync()
        {
            Throw.ArgumentNullException.IfNull(_connection, nameof(_connection));
            return DisposeConnectionAsync();
        }

        /// <summary>
        /// Called back in <see cref="Transaction.Dispose"/>
        /// </summary>
        public void OnTransactionDispose()
        {
            Throw.ArgumentNullException.IfNull(_transaction, nameof(_transaction));
            Throw.ArgumentNullException.IfNull(_connection, nameof(_connection));

            UpdateBookmark(_transaction.Bookmark);
            _transaction = null;

            DisposeConnection();
        }

        public Task OnTransactionDisposeAsync()
        {
            Throw.ArgumentNullException.IfNull(_transaction, nameof(_transaction));
            Throw.ArgumentNullException.IfNull(_connection, nameof(_connection));

            UpdateBookmark(_transaction.Bookmark);
            _transaction = null;

            return DisposeConnectionAsync();
        }

        /// <summary>
        /// Only set the bookmark to a new value if the new value is not null
        /// </summary>
        /// <param name="bookmark">The new bookmark</param>
        private void UpdateBookmark(Bookmark bookmark)
        {
            if (bookmark != null && !bookmark.IsEmpty())
            {
                _bookmark = bookmark;
            }
        }

        /// <summary>
        /// Clean any transaction reference.
        /// If transaction result is not commited, then rollback the transaction.
        /// </summary>
        /// <exception cref="ClientException">If error when rollback the transaction</exception>
        private void DisposeTransaction()
        {
            // When there is a open transation, this method will aslo try to close the tx
            if (_transaction != null)
            {
                try
                {
                    _transaction.Dispose();
                }
                catch (Exception e)
                {
                    throw new ClientException($"Error when disposing unclosed transaction in session: {e.Message}", e);
                }
            }
        }

        private async Task DisposeTransactionAsync()
        {
            // When there is a open transation, this method will aslo try to close the tx
            if (_transaction != null)
            {
                try
                {
                    await _transaction.RollbackAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new ClientException($"Error when disposing unclosed transaction in session: {e.Message}", e);
                }
            }
        }

        /// <summary>
        /// Clean any session.run result reference.
        /// If session.run result is not fully consumed, then pull full result into memory.
        /// </summary>
        /// <exception cref="ClientException">If error when pulling result into memory</exception>
        private void DisposeSessionResult()
        {
            if (_connection == null)
            {
                // there is no session result resources to dispose
                return;
            }

            if (_connection.IsOpen)
            {
                try
                {
                    // this will enfore to buffer all unconsumed result
                    _connection.Sync();
                }
                catch (Exception e)
                {
                    throw new ClientException(
                        $"Error when pulling unconsumed session.run records into memory in session: {e.Message}", e);
                }
                finally
                {
                    // there is a possibility that when error happens e.g. ProtocolError, the resources are not closed.
                    DisposeConnection();
                }
            }
            else
            {
                DisposeConnection();
            }
        }

        private async Task DisposeSessionResultAsync()
        {
            if (_connection == null)
            {
                // there is no session result resources to dispose
                return;
            }

            if (_connection.IsOpen)
            {
                try
                {
                    // this will enfore to buffer all unconsumed result
                    await _connection.SyncAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new ClientException(
                        $"Error when pulling unconsumed session.run records into memory in session: {e.Message}", e);
                }
                finally
                {
                    // there is a possibility that when error happens e.g. ProtocolError, the resources are not closed.
                    await DisposeConnectionAsync().ConfigureAwait(false);
                }
            }
            else
            {
                await DisposeConnectionAsync().ConfigureAwait(false);
            }
        }

        private void DisposeConnection()
        {
            // always try to close connection used by the result too
            _connection?.Close();
            _connection = null;
        }

        private async Task DisposeConnectionAsync()
        {
            // always try to close connection used by the result too
            if (_connection != null)
            {
                await _connection.CloseAsync().ConfigureAwait(false);
            }
            _connection = null;
        }

        private void EnsureCanRunMoreStatements()
        {
            EnsureSessionIsOpen();
            EnsureNoOpenTransaction();
            DisposeSessionResult();
        }

        private Task EnsureCanRunMoreStatementsAsync()
        {
            EnsureSessionIsOpen();
            EnsureNoOpenTransaction();
            return DisposeSessionResultAsync();
        }

        private void EnsureNoOpenTransaction()
        {
            if (_transaction != null)
            {
                throw new ClientException("Please close the currently open transaction object before running " +
                                          "more statements/transactions in the current session.");
            }
        }

        private void EnsureSessionIsOpen()
        {
            if (!_isOpen)
            {
                throw new ClientException(
                    "Cannot running more statements in the current session as it has already been disposed." +
                    "Make sure that you do not have a bad reference to a disposed session " +
                    "and retry your statement in another new session.");
            }
        }
    }
}
