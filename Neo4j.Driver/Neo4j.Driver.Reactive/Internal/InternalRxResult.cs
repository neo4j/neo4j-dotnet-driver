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
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
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
        private enum StreamingState
        {
            Ready = 0,
            Streaming = 1,
            Completed = 2
        }

        private readonly IObservable<IDiscardableStatementResultCursor> _resultCursor;
        private readonly IObservable<string[]> _keys;
        private readonly Subject<IRecord> _records;
        private readonly ReplaySubject<IResultSummary> _summary;

        private volatile int _streaming = (int) StreamingState.Ready;

        public InternalRxResult(IObservable<IDiscardableStatementResultCursor> resultCursor)
        {
            _resultCursor = resultCursor.Replay().AutoConnect();
            _keys = _resultCursor.SelectMany(x => x.KeysAsync().ToObservable()).Replay().AutoConnect();
            _records = new Subject<IRecord>();
            _summary = new ReplaySubject<IResultSummary>();
        }

        private StreamingState State => (StreamingState) _streaming;

        public IObservable<string[]> Keys()
        {
            return _keys;
        }

        public IObservable<IRecord> Records()
        {
            return _resultCursor.SelectMany(cursor =>
                Observable.Create<IRecord>(recordObserver => StartStreaming(cursor, recordObserver)));
        }

        public IObservable<IResultSummary> Summary()
        {
            return _resultCursor.SelectMany(cursor =>
                Observable.Create<IResultSummary>(summaryObserver =>
                    StartStreaming(cursor, summaryObserver: summaryObserver)));
        }

        private IDisposable StartStreaming(IDiscardableStatementResultCursor cursor,
            IObserver<IRecord> recordObserver = null, IObserver<IResultSummary> summaryObserver = null)
        {
            var cancellation = new CompositeDisposable(2);

            if (summaryObserver != null)
            {
                cancellation.Add(_summary.Subscribe(summaryObserver));
            }

            if (recordObserver != null && !_records.HasObservers)
            {
                cancellation.Add(_records.Subscribe(recordObserver));
            }

            if (StartStreaming())
            {
                var streamingCancellation = new CancellationDisposable();
                cancellation.Add(streamingCancellation);

                Task.Run(async () =>
                {
                    try
                    {
                        // Ensure that we propagate any errors from the KeysAsync call
                        await cursor.KeysAsync().ConfigureAwait(false);

                        if (!_records.HasObservers)
                        {
                            cursor.Discard();
                        }
                        else
                        {
                            streamingCancellation.Token.Register(cursor.Discard);
                        }

                        while (await cursor.FetchAsync().ConfigureAwait(false))
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

                    CompleteStreaming();
                }, streamingCancellation.Token);
            }
            else if (State == StreamingState.Streaming)
            {
                recordObserver?.OnError(
                    new ClientException(
                        "Streaming has already started with a previous Records or Summary subscription."));
            }

            return cancellation;
        }

        private bool StartStreaming()
        {
            return Interlocked.CompareExchange(ref _streaming, (int) StreamingState.Streaming,
                       (int) StreamingState.Ready) == (int) StreamingState.Ready;
        }

        private void CompleteStreaming()
        {
            Interlocked.CompareExchange(ref _streaming, (int) StreamingState.Completed, (int) StreamingState.Streaming);
        }
    }
}