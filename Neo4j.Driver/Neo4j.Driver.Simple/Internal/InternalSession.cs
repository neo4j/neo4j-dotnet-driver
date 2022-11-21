// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.Internal;

internal class InternalSession : ISession
{
    private readonly BlockingExecutor _executor;
    private readonly IRetryLogic _retryLogic;
    private readonly IInternalAsyncSession _session;

    private bool _disposed;

    public InternalSession(IInternalAsyncSession session, IRetryLogic retryLogic, BlockingExecutor executor)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _retryLogic = retryLogic ?? throw new ArgumentNullException(nameof(retryLogic));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

#pragma warning disable CS0618
    public Bookmark LastBookmark => _session.LastBookmark;
#pragma warning restore CS0618
    public Bookmarks LastBookmarks => _session.LastBookmarks;
    public SessionConfig SessionConfig => _session.SessionConfig;

    public IResult Run(string query)
    {
        return Run(new Query(query));
    }

    public IResult Run(string query, object parameters)
    {
        return Run(new Query(query, parameters.ToDictionary()));
    }

    public IResult Run(string query, IDictionary<string, object> parameters)
    {
        return Run(new Query(query, parameters));
    }

    public IResult Run(Query query)
    {
        return Run(query, null);
    }

    public IResult Run(string query, Action<TransactionConfigBuilder> action)
    {
        return Run(new Query(query), action);
    }

    public IResult Run(
        string query,
        IDictionary<string, object> parameters,
        Action<TransactionConfigBuilder> action)
    {
        return Run(new Query(query, parameters), action);
    }

    public IResult Run(Query query, Action<TransactionConfigBuilder> action)
    {
        return new InternalResult(
            _executor.RunSync(() => _session.RunAsync(query, action, true)),
            _executor);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~InternalSession()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _executor.RunSync(() => _session.CloseAsync());
            }
        }

        _disposed = true;
    }

#region BeginTransaction Methods

    public ITransaction BeginTransaction()
    {
        return BeginTransaction(null);
    }

    public ITransaction BeginTransaction(Action<TransactionConfigBuilder> action)
    {
        return new InternalTransaction(
            _executor.RunSync(() => _session.BeginTransactionAsync(action, true))
                .CastOrThrow<IInternalAsyncTransaction>(),
            _executor);
    }

    private InternalTransaction BeginTransaction(AccessMode mode, Action<TransactionConfigBuilder> action)
    {
        return new InternalTransaction(
            _executor.RunSync(() => _session.BeginTransactionAsync(mode, action, true))
                .CastOrThrow<IInternalAsyncTransaction>(),
            _executor);
    }

#endregion

#region Transaction Methods

    public T ReadTransaction<T>(Func<ITransaction, T> work)
    {
        return ReadTransaction(work, null);
    }

    public T ReadTransaction<T>(Func<ITransaction, T> work, Action<TransactionConfigBuilder> action)
    {
        return RunTransaction(AccessMode.Read, work, action);
    }

    public T WriteTransaction<T>(Func<ITransaction, T> work)
    {
        return WriteTransaction(work, null);
    }

    public T WriteTransaction<T>(Func<ITransaction, T> work, Action<TransactionConfigBuilder> action)
    {
        return RunTransaction(AccessMode.Write, work, action);
    }

    public T ExecuteRead<T>(Func<IQueryRunner, T> work)
    {
        return ReadTransaction(work, null);
    }

    public T ExecuteRead<T>(Func<IQueryRunner, T> work, Action<TransactionConfigBuilder> action)
    {
        return RunTransaction(AccessMode.Read, work, action);
    }

    public T ExecuteWrite<T>(Func<IQueryRunner, T> work)
    {
        return WriteTransaction(work, null);
    }

    public T ExecuteWrite<T>(Func<IQueryRunner, T> work, Action<TransactionConfigBuilder> action)
    {
        return RunTransaction(AccessMode.Write, work, action);
    }

    internal T RunTransaction<T>(AccessMode mode, Func<ITransaction, T> work, Action<TransactionConfigBuilder> action)
    {
        return _retryLogic.Retry(
            () =>
            {
                using (var txc = BeginTransaction(mode, action))
                {
                    try
                    {
                        var result = work(txc);
                        if (txc.IsOpen)
                        {
                            txc.Commit();
                        }

                        return result;
                    }
                    catch
                    {
                        if (txc.IsOpen)
                        {
                            txc.Rollback();
                        }

                        throw;
                    }
                }
            });
    }

#endregion
}
