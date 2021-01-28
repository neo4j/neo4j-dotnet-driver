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
// See the License for the specific

using System;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Neo4j.Driver.Internal.MessageHandling;
using static Neo4j.Driver.Internal.Logging.DriverLoggerUtil;

namespace Neo4j.Driver.Internal
{
    internal partial class AsyncSession : IResultResourceHandler, ITransactionResourceHandler, IBookmarkTracker
    {
        public Task CloseAsync()
        {
            return TryExecuteAsync(_logger, async () =>
            {
                if (_isOpen)
                {
                    // This will protect the session being disposed twice
                    _isOpen = false;
                    try
                    {
                        await DisposeTransactionAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        await DisposeSessionResultAsync().ConfigureAwait(false);
                    }
                }
            }, "Failed to close the session asynchronously.");
        }

        /// <summary>
        ///  This method will be called back by <see cref="ResultCursorBuilder"/> after it consumed result
        /// </summary>
        public Task OnResultConsumedAsync()
        {
            Throw.ArgumentNullException.IfNull(_connection, nameof(_connection));
            return DisposeConnectionAsync();
        }

        /// <summary>
        /// Called back when transaction is closed
        /// </summary>
        public Task OnTransactionDisposeAsync(Bookmark bookmark)
        {
            UpdateBookmark(bookmark);
            _transaction = null;

            return DisposeConnectionAsync();
        }

        /// <summary>
        /// Only set the bookmark to a new value if the new value is not null
        /// </summary>
        /// <param name="bookmark">The new bookmark</param>
        public void UpdateBookmark(Bookmark bookmark)
        {
            if (bookmark != null && bookmark.Values.Any())
            {
                _bookmark = bookmark;
            }
        }

        /// <summary>
        /// Clean any transaction reference.
        /// If transaction result is not committed, then rollback the transaction.
        /// </summary>
        /// <exception cref="ClientException">If error when rollback the transaction</exception>
        private async Task DisposeTransactionAsync()
        {
            // When there is a open transaction, this method will also try to close the tx
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

        private async Task DisposeSessionResultAsync()
        {
            try
            {
                await DiscardUnconsumedResultAsync().ConfigureAwait(false);
            }
            finally
            {
                await DisposeConnectionAsync().ConfigureAwait(false);
            }
        }
        private async Task DiscardUnconsumedResultAsync()
        {
            if (_result != null)
            {
                IResultCursor cursor = null;
                try
                {
                    cursor = await _result.ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignored if the cursor failed to create
                }

                if (cursor != null)
                {
                    await cursor.ConsumeAsync().ConfigureAwait(false);
                }
            }
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

        private async Task EnsureCanRunMoreQuerysAsync(bool disposeUnconsumedSessionResult)
        {
            EnsureSessionIsOpen();
            EnsureNoOpenTransaction();
            if (disposeUnconsumedSessionResult)
            {
                await DisposeSessionResultAsync();
            }
            else
            {
                if (_connection != null) // after a result is consumed, connection will be set to null
                {
                    throw new ClientException("Please consume the current query result before running " +
                                              "more queries/transaction in the same session.");
                }
            }
        }

        private void EnsureNoOpenTransaction()
        {
            if (_transaction != null)
            {
                throw new ClientException("Please close the currently open transaction object before running " +
                                          "more queries/transactions in the current session.");
            }
        }

        private void EnsureSessionIsOpen()
        {
            if (!_isOpen)
            {
                throw new ClientException(
                    "Cannot running more queries in the current session as it has already been disposed. " +
                    "Make sure that you do not have a bad reference to a disposed session " +
                    "and retry your query in another new session.");
            }
        }
    }
}