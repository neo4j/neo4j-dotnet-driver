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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

internal class RxResult : IRxResult
{
    private readonly IObservable<string[]> _keys;
    private readonly ILogger _logger;
    private readonly Subject<IRecord> _records;

    private readonly IObservable<IInternalResultCursor> _resultCursor;
    private readonly ReplaySubject<IResultSummary> _summary;

    private volatile int _streaming = (int)StreamingState.Ready;

    public RxResult(
        IObservable<IInternalResultCursor> resultCursor,
        ILogger logger = null)
    {
        _resultCursor = resultCursor.Replay().AutoConnect();
        _keys = _resultCursor.SelectMany(x => x.KeysAsync().ToObservable()).Replay().AutoConnect();
        _records = new Subject<IRecord>();
        _summary = new ReplaySubject<IResultSummary>();
        _logger = logger;
    }

    private StreamingState State => (StreamingState)_streaming;

    public IObservable<string[]> Keys()
    {
        return _keys;
    }

    public IObservable<IRecord> Records()
    {
        return _resultCursor.SelectMany(
            cursor =>
                Observable.Create<IRecord>(recordObserver => StartStreaming(cursor, recordObserver)));
    }

    public IObservable<IResultSummary> Consume()
    {
        return _resultCursor.SelectMany(
            cursor =>
                Observable.Create<IResultSummary>(
                    summaryObserver =>
                        StartStreaming(cursor, summaryObserver: summaryObserver)));
    }

    public IObservable<bool> IsOpen => _resultCursor.Select(x => x.IsOpen).FirstAsync();

    private IDisposable StartStreaming(
        IInternalResultCursor cursor,
        IObserver<IRecord> recordObserver = null,
        IObserver<IResultSummary> summaryObserver = null)
    {
        var cancellation = new CompositeDisposable(2);

        if (summaryObserver != null)
        {
            cancellation.Add(_summary.Subscribe(summaryObserver));
        }

        if (StartStreaming())
        {
            if (recordObserver != null)
            {
                cancellation.Add(_records.Subscribe(recordObserver));
            }

            var streamingCancellation = new CancellationDisposable();
            cancellation.Add(streamingCancellation);

            Task.Run(
                async () =>
                {
                    try
                    {
                        // Ensure that we propagate any errors from the KeysAsync call
                        await cursor.KeysAsync().ConfigureAwait(false);

                        if (!_records.HasObservers)
                        {
                            cursor.Cancel();
                        }
                        else
                        {
                            streamingCancellation.Token.Register(cursor.Cancel);
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
                },
                streamingCancellation.Token);
        }
        else
        {
            recordObserver?.OnError(
                new ResultConsumedException(
                    "Streaming has already started and/or finished with a previous Records or Summary subscription."));
        }

        return cancellation;
    }

    private bool StartStreaming()
    {
        return Interlocked.CompareExchange(
                ref _streaming,
                (int)StreamingState.Streaming,
                (int)StreamingState.Ready) ==
            (int)StreamingState.Ready;
    }

    private void CompleteStreaming()
    {
        Interlocked.CompareExchange(ref _streaming, (int)StreamingState.Completed, (int)StreamingState.Streaming);
    }

    private enum StreamingState
    {
        Ready = 0,
        Streaming = 1,
        Completed = 2
    }
}
