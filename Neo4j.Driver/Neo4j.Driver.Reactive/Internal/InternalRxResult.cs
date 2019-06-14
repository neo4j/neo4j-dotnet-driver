// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal
{
    internal class InternalRxResult : IRxResult
    {
        private readonly IObservable<IStatementResultCursor> _resultCursor;
        private readonly CancellationTokenSource _cts;
        private readonly IObservable<string[]> _keys;
        private readonly ReplaySubject<IResultSummary> _summary;
        private readonly Subject<IRecord> _records;

        private int _streaming;

        public InternalRxResult(IObservable<IStatementResultCursor> resultCursor)
        {
            _resultCursor = resultCursor.Replay().AutoConnect();
            _cts = new CancellationTokenSource();
            _keys = _resultCursor.SelectMany(x => x.KeysAsync().ToObservable()).Replay().AutoConnect();
            _records = new Subject<IRecord>();
            _summary = new ReplaySubject<IResultSummary>();
        }

        public IObservable<string[]> Keys()
        {
            return _keys;
        }

        public IObservable<IRecord> Records()
        {
            return _resultCursor.SelectMany(cursor =>
                Observable.Create<IRecord>(observer => StartStreaming(cursor, observer, null)));
        }

        public IObservable<IResultSummary> Summary()
        {
            return _resultCursor.SelectMany(cursor =>
                Observable.Create<IResultSummary>(observer => StartStreaming(cursor, null, observer)));
        }

        private IDisposable StartStreaming(IStatementResultCursor cursor, IObserver<IRecord> recordObserver,
            IObserver<IResultSummary> summaryObserver)
        {
            var result = Disposable.Empty;

            if (recordObserver != null && EnsureNoRecordsObservers(recordObserver))
            {
                result = _records.Subscribe(recordObserver);
            }

            if (summaryObserver != null)
            {
                result = _summary.Subscribe(summaryObserver);
            }

            if (Interlocked.CompareExchange(ref _streaming, 1, 0) == 0)
            {
                if (recordObserver == null)
                {
                    _cts.Cancel();
                }

                Task.Run(async () =>
                {
                    try
                    {
                        // Ensure that we propagate any errors from the KeysAsync call
                        await cursor.KeysAsync().ConfigureAwait(false);

                        while (await cursor.FetchAsync().ConfigureAwait(false) && !_cts.IsCancellationRequested)
                        {
                            _records.OnNext(cursor.Current);
                        }

                        _records.OnCompleted();
                    }
                    catch (Exception exc)
                    {
                        _records.OnError(exc);

                        if (summaryObserver != null)
                        {
                            _summary.OnError(exc);
                        }
                    }

                    try
                    {
                        _summary.OnNext(await cursor.ConsumeAsync().ConfigureAwait(false));
                        _summary.OnCompleted();
                    }
                    catch (Exception exc)
                    {
                        _summary.OnError(exc);
                    }
                });
            }

            return result;
        }

        private bool EnsureNoRecordsObservers(IObserver<IRecord> recordObserver)
        {
            if (_records.HasObservers)
            {
                recordObserver.OnError(
                    new ClientException("At most one observer could be subscribed to Records stream."));

                return false;
            }

            return true;
        }
    }
}