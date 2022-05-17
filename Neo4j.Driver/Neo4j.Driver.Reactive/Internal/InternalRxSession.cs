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
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Neo4j.Driver.Internal
{
    internal class InternalRxSession : IRxSession
    {
        private readonly IInternalAsyncSession _session;
        private readonly IRxRetryLogic _retryLogic;

        public InternalRxSession(IInternalAsyncSession session, IRxRetryLogic retryLogic)
        {
            _session = session;
            _retryLogic = retryLogic;
        }

        public Bookmark LastBookmark => _session.LastBookmark;
        public Bookmarks LastBookmarks => _session.LastBookmarks;

        public SessionConfig SessionConfig => _session.SessionConfig;

        #region Run Methods

        public IRxResult Run(string query)
        {
            return Run(query, null);
        }

        public IRxResult Run(string query, object parameters)
        {
            return Run(new Query(query, parameters.ToDictionary()), null);
        }

        public IRxResult Run(Query query)
        {
            return Run(query, null);
        }

        public IRxResult Run(string query, Action<TransactionConfigBuilder> action)
        {
            return Run(new Query(query), action);
        }

        public IRxResult Run(string query, object parameters, Action<TransactionConfigBuilder> action)
        {
            return Run(new Query(query, parameters.ToDictionary()), action);
        }

        public IRxResult Run(Query query, Action<TransactionConfigBuilder> action)
        {
            return new RxResult(Observable.FromAsync(() => _session.RunAsync(query, action, false))
                .Cast<IInternalResultCursor>());
        }

        #endregion

        #region BeginTransaction Methods

        public IObservable<IRxTransaction> BeginTransaction()
        {
            return BeginTransaction(null);
        }

        public IObservable<IRxTransaction> BeginTransaction(Action<TransactionConfigBuilder> action)
        {
            return Observable.FromAsync(() => _session.BeginTransactionAsync(action, false))
                .Select(tx =>
                    new InternalRxTransaction(tx.CastOrThrow<IInternalAsyncTransaction>()));
        }

        private IObservable<InternalRxTransaction> BeginTransaction(AccessMode mode, Action<TransactionConfigBuilder> action)
        {
            return Observable.FromAsync(() => _session.BeginTransactionAsync(mode, action, false))
                .Select(tx =>
                    new InternalRxTransaction(tx.CastOrThrow<IInternalAsyncTransaction>()));
        }

        #endregion

        #region Transaction Functions

        public IObservable<T> ReadTransaction<T>(Func<IRxTransaction, IObservable<T>> work)
        {
            return ReadTransaction(work, null);
        }

        public IObservable<T> ReadTransaction<T>(Func<IRxTransaction, IObservable<T>> work,
            Action<TransactionConfigBuilder> action)
        {
            return RunTransaction(AccessMode.Read, work, action);
        }

        public IObservable<T> WriteTransaction<T>(Func<IRxTransaction, IObservable<T>> work)
        {
            return WriteTransaction(work, null);
        }

        public IObservable<T> WriteTransaction<T>(Func<IRxTransaction, IObservable<T>> work,
            Action<TransactionConfigBuilder> action)
        {
            return RunTransaction(AccessMode.Write, work, action);
        }

        internal IObservable<T> RunTransaction<T>(AccessMode mode,
            Func<IRxTransaction, IObservable<T>> work,
            Action<TransactionConfigBuilder> action)
        {
            return _retryLogic.Retry(
                BeginTransaction(mode, action)
                    .SelectMany(txc =>
                        Observable.Defer(() =>
                            {
                                try
                                {
                                    return work(txc);
                                }
                                catch (Exception exc)
                                {
                                    return Observable.Throw<T>(exc);
                                }
                            })
                            .CatchAndThrow(exc => txc.IsOpen ? txc.Rollback<T>() : Observable.Empty<T>())
                            .Concat(txc.IsOpen ? txc.Commit<T>() : Observable.Empty<T>()))
            );
        }

        #endregion

        #region Cleanup

        public IObservable<T> Close<T>()
        {
            return Observable.FromAsync(
                () =>
                    _session.CloseAsync()).SelectMany(x => Observable.Empty<T>());
        }

        #endregion
    }
}