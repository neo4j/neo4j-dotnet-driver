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
        private readonly ReplaySubject<IResultSummary> _summary;
        private readonly Subject<IRecord> _records;

        private int _streaming;

        public InternalRxResult(IObservable<IStatementResultCursor> resultCursor)
        {
            _resultCursor = resultCursor.Replay().AutoConnect();
            _cts = new CancellationTokenSource();
            _summary = new ReplaySubject<IResultSummary>(1);
            _records = new Subject<IRecord>();
        }

        private bool IsStreaming => Volatile.Read(ref _streaming) > 0;

        public IObservable<string[]> Keys()
        {
            return _resultCursor.SelectMany(r => r.KeysAsync());
        }

        public IObservable<IRecord> Records()
        {
            return Observable.Create(async (IObserver<IRecord> o) =>
            {
                _records.Subscribe(o);
                await Stream(_cts.Token);
                return new CancellationDisposable(_cts);
            });
        }

        public IObservable<IResultSummary> Summary()
        {
            return Observable.Create(async (IObserver<IResultSummary> o) =>
            {
                var subscription = _summary.Subscribe(o);
                if (!IsStreaming)
                {
                    _cts.Cancel();
                }

                await Stream(_cts.Token).ConfigureAwait(false);
                return new CancellationDisposable(_cts);
            });
        }

        private async Task Stream(CancellationToken cts)
        {
            if (Interlocked.Increment(ref _streaming) == 1)
            {
                try
                {
                    var cursor = await _resultCursor.GetAwaiter();

                    // Ensure that we propagate any errors from the KeysAsync call
                    await cursor.KeysAsync();

                    while (await cursor.FetchAsync() && !cts.IsCancellationRequested)
                    {
                        _records.OnNext(cursor.Current);
                    }

                    _records.OnCompleted();

                    try
                    {
                        _summary.OnNext(await cursor.ConsumeAsync().ConfigureAwait(false));
                        _summary.OnCompleted();
                    }
                    catch (Exception exc)
                    {
                        _summary.OnError(exc);
                    }
                }
                catch (Exception exc)
                {
                    _records.OnError(exc);
                    _summary.OnError(exc);
                }
            }
        }
    }
}