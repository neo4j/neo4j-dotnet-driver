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
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.MessageHandling;
using static Neo4j.Driver.Internal.Messaging.V4.ResultHandleMessage;

namespace Neo4j.Driver.Internal.Result
{
    internal class StatementResultCursorBuilder : IResultStreamBuilder
    {
        private readonly long _batchSize;
        private readonly Func<Task> _advanceFunction;
        private readonly Func<StatementResultCursorBuilder, long, long, Task> _moreFunction;
        private readonly Func<StatementResultCursorBuilder, long, Task> _cancelFunction;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly IResultResourceHandler _resourceHandler;
        private readonly SummaryBuilder _summaryBuilder;

        private readonly LinkedList<IRecord> _records;

        private volatile int _state;
        private long _statementId;
        private string[] _fields;

        private IResponsePipelineError _pendingError;

        public StatementResultCursorBuilder(SummaryBuilder summaryBuilder, Func<Task> advanceFunction,
            Func<StatementResultCursorBuilder, long, long, Task> moreFunction,
            Func<StatementResultCursorBuilder, long, Task> cancelFunction, IResultResourceHandler resourceHandler,
            long batchSize = All)
        {
            _summaryBuilder = summaryBuilder ?? throw new ArgumentNullException(nameof(summaryBuilder));
            _advanceFunction =
                WrapAdvanceFunc(advanceFunction ?? throw new ArgumentNullException(nameof(advanceFunction)));
            _moreFunction = moreFunction ?? ((s, id, n) => TaskHelper.GetCompletedTask());
            _cancelFunction = cancelFunction ?? ((s, id) => TaskHelper.GetCompletedTask());
            _cancellationSource = new CancellationTokenSource();
            _resourceHandler = resourceHandler;

            _records = new LinkedList<IRecord>();

            _state = (int) State.RunRequested;
            _statementId = NoStatementId;
            _fields = null;
            _batchSize = batchSize;
        }

        internal State CurrentState => (State) _state;

        public IReactiveStatementResultCursor CreateCursor()
        {
            return new StatementResultCursor(GetKeysAsync, NextRecordAsync, SummaryAsync, _cancellationSource);
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
            var first = _records.First;
            if (first != null)
            {
                _records.RemoveFirst();
                return first.Value;
            }

            while (CurrentState < State.Completed && _records.First == null)
            {
                await _advanceFunction().ConfigureAwait(false);
            }

            first = _records.First;
            if (first != null)
            {
                _records.RemoveFirst();
                return first.Value;
            }

            _pendingError?.EnsureThrown();

            return null;
        }

        public async Task<IResultSummary> SummaryAsync()
        {
            while (CurrentState < State.Completed)
            {
                await _advanceFunction().ConfigureAwait(false);
            }

            _pendingError?.EnsureThrown();

            return _summaryBuilder.Build();
        }

        public void RunCompleted(long statementId, string[] fields, IResponsePipelineError error)
        {
            _statementId = statementId;
            _fields = fields;

            UpdateState(State.RunCompleted);
        }

        public void PullCompleted(bool hasMore, IResponsePipelineError error)
        {
            UpdateState(hasMore ? State.RunCompleted : State.Completed);
        }

        public void PushRecord(object[] fieldValues)
        {
            _records.AddLast(new Record(_fields, fieldValues));
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
                        await _cancelFunction(this, _statementId).ConfigureAwait(false);
                    }
                    else
                    {
                        await _moreFunction(this, _statementId, _batchSize).ConfigureAwait(false);
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
            RunRequested,
            RunCompleted,
            RecordsRequested,
            RecordsStreaming,
            Completed
        }
    }
}