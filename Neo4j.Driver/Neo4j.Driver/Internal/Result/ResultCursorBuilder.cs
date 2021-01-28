﻿// Copyright (c) "Neo4j"
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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.MessageHandling;
using static Neo4j.Driver.Internal.Messaging.V4.ResultHandleMessage;

namespace Neo4j.Driver.Internal.Result
{
    internal class ResultCursorBuilder : IResultStreamBuilder, IResultStream
    {
        private readonly long _fetchSize;
        private readonly Func<Task> _advanceFunction;
        private readonly Func<IResultStreamBuilder, long, long, Task> _moreFunction;
        private readonly Func<IResultStreamBuilder, long, Task> _cancelFunction;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly IResultResourceHandler _resourceHandler;
        private readonly SummaryBuilder _summaryBuilder;

        private readonly ConcurrentQueue<IRecord> _records;

        private volatile int _state;
        private long _queryId;
        private string[] _fields;

        private IResponsePipelineError _pendingError;
        private readonly IAutoPullHandler _autoPullHandler;

        public ResultCursorBuilder(SummaryBuilder summaryBuilder, Func<Task> advanceFunction,
            Func<IResultStreamBuilder, long, long, Task> moreFunction,
            Func<IResultStreamBuilder, long, Task> cancelFunction, IResultResourceHandler resourceHandler,
            long fetchSize = Config.Infinite, bool reactive = false)
        {
            _summaryBuilder = summaryBuilder ?? throw new ArgumentNullException(nameof(summaryBuilder));
            _advanceFunction =
                WrapAdvanceFunc(advanceFunction ?? throw new ArgumentNullException(nameof(advanceFunction)));
            _moreFunction = moreFunction ?? ((s, id, n) => Task.CompletedTask);
            _cancelFunction = cancelFunction ?? ((s, id) => Task.CompletedTask);
            _cancellationSource = new CancellationTokenSource();
            _resourceHandler = resourceHandler;

            _records = new ConcurrentQueue<IRecord>();

            _state = (int) (reactive ? State.RunRequested : State.RunAndRecordsRequested);
            _queryId = NoQueryId;
            _fields = null;
            _fetchSize = fetchSize;
            _autoPullHandler = new AutoPullHandler(_fetchSize);
        }

        /// <summary>
        /// Auto pull is disabled when there is too few records, and re-enabled when there is too many records.
        /// The auto pull state will be checked when the record is consumed.
        /// </summary>
        private interface IAutoPullHandler
        {
            bool TryDisableAutoPull(int recordCount);
            bool TryEnableAutoPull(int recordCount);

            bool AutoPull { get; }
        }

        internal class AutoPullHandler : IAutoPullHandler
        {
            private readonly long _lowWatermark;
            private readonly long _highWatermark;
            private bool _autoPull = true;

            public AutoPullHandler(long fetchSize)
            {
                if (fetchSize == Config.Infinite)
                {
                    // All records would come in one batch.
                    // We will not turn off auto pull for this case.
                    _lowWatermark = long.MaxValue; // we will always be lower than this to turn on auto pull.
                    _highWatermark = long.MaxValue; // we can never go higher than this to turn off auto pull
                }
                else
                {
                    _lowWatermark = (long) (fetchSize * 0.3);
                    _highWatermark = (long) (fetchSize * 0.7);
                }
            }

            public bool TryDisableAutoPull(int recordCount)
            {
                if (_autoPull && recordCount > _highWatermark)
                {
                    _autoPull = false;
                    return true;
                }

                return false;
            }

            public bool TryEnableAutoPull(int recordCount)
            {
                if (!_autoPull && recordCount <= _lowWatermark)
                {
                    _autoPull = true;
                    return true;
                }

                return false;
            }

            public bool AutoPull => _autoPull;
        }

        internal State CurrentState
        {
            get => (State) _state;
            set => _state = (int) value;
        }

        public IInternalResultCursor CreateCursor()
        {
            return new ConsumableResultCursor(new ResultCursor(this));
        }

        public async Task<string[]> GetKeysAsync()
        {
            while (CurrentState < State.RunCompleted)
            {
                await _advanceFunction().ConfigureAwait(false);
            }

            _pendingError?.EnsureThrown();

            return _fields ?? new string[0];
        }

        public async Task<IRecord> NextRecordAsync()
        {
            if (_cancellationSource.IsCancellationRequested)
            {
                // Stop populate records immediately once the cancellation is requested.
                ClearRecords();
            }

            if (_records.TryDequeue(out var record))
            {
                _autoPullHandler.TryEnableAutoPull(_records.Count);
                if (CurrentState < State.Completed && _autoPullHandler.AutoPull)
                {
                    await _advanceFunction().ConfigureAwait(false);
                }

                return record;
            }

            while (CurrentState < State.Completed && _records.IsEmpty)
            {
                await _advanceFunction().ConfigureAwait(false);
            }

            if (_records.TryDequeue(out record))
            {
                return record;
            }

            _pendingError?.EnsureThrown();

            return null;
        }

        private void ClearRecords()
        {
            while (_records.TryDequeue(out _))
            {
            }
        }

        public void Cancel()
        {
            _cancellationSource.Cancel();
        }

        public async Task<IResultSummary> ConsumeAsync()
        {
            while (CurrentState < State.Completed)
            {
                await _advanceFunction().ConfigureAwait(false);
            }
            _pendingError?.EnsureThrown();
            return _summaryBuilder.Build();
        }

        public void RunCompleted(long queryId, string[] fields, IResponsePipelineError error)
        {
            _queryId = queryId;
            _fields = fields;

            CheckAndUpdateState(State.RunCompleted, State.RunRequested);
        }

        public void PullCompleted(bool hasMore, IResponsePipelineError error)
        {
            UpdateState(hasMore ? State.RunCompleted : State.Completed);
        }

        public void PushRecord(object[] fieldValues)
        {
            _records.Enqueue(new Record(_fields, fieldValues));
            _autoPullHandler.TryDisableAutoPull(_records.Count);

            UpdateState(State.RecordsStreaming);
        }

        private Func<Task> WrapAdvanceFunc(Func<Task> advanceFunc)
        {
            return async () =>
            {
                if (CheckAndUpdateState(State.RecordsRequested, State.RunCompleted))
                {
                    if (_cancellationSource.IsCancellationRequested)
                    {
                        await _cancelFunction(this, _queryId).ConfigureAwait(false);
                    }
                    else
                    {
                        await _moreFunction(this, _queryId, _fetchSize).ConfigureAwait(false);
                    }
                }

                if (CurrentState < State.Completed)
                {
                    try
                    {
                        await advanceFunc().ConfigureAwait(false);
                    }
                    catch (ProtocolException)
                    {
                        throw;
                    }
                    catch (Exception exc)
                    {
                        _pendingError = new ResponsePipelineError(exc);

                        // Ensure that current state is updated and is recognized as Completed
                        UpdateState(State.Completed);
                    }
                }

                if (CurrentState == State.Completed && _resourceHandler != null)
                {
                    await _resourceHandler.OnResultConsumedAsync().ConfigureAwait(false);
                }
            };
        }

        private bool CheckAndUpdateState(State desired, State current)
        {
            var desiredState = (int) desired;
            var currentState = (int) current;
            if (Interlocked.CompareExchange(ref _state, desiredState, currentState) == currentState)
            {
                return true;
            }

            return false;
        }

        private void UpdateState(State desired)
        {
            var desiredState = (int) desired;
            Interlocked.Exchange(ref _state, desiredState);
        }

        internal enum State
        {
            RunRequested, // reactive initial state
            RunAndRecordsRequested, // async initial state
            RunCompleted,
            RecordsRequested,
            RecordsStreaming,
            Completed
        }
    }
}