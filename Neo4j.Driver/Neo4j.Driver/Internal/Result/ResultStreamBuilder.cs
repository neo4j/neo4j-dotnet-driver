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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.MessageHandling;
using static Neo4j.Driver.Internal.Messaging.V4.ResultHandleMessage;

namespace Neo4j.Driver.Internal.Result
{
    internal class ResultStreamBuilder : IResultStreamBuilder
    {
        private readonly long _batchSize;
        private readonly Func<Task> _advanceFunction;
        private readonly Func<ResultStreamBuilder, long, long, Task> _moreFunction;
        private readonly Func<ResultStreamBuilder, long, Task> _cancelFunction;
        private readonly CancellationToken _cancellation;
        private readonly IResultResourceHandler _resourceHandler;
        private readonly SummaryBuilder _summaryBuilder;

        private readonly LinkedList<IRecord> _records;

        private State _state;
        private long _statementId;
        private string[] _fields;

        public ResultStreamBuilder(SummaryBuilder summaryBuilder, Func<Task> advanceFunction,
            Func<ResultStreamBuilder, long, long, Task> moreFunction,
            Func<ResultStreamBuilder, long, Task> cancelFunction, CancellationToken cancellation,
            IResultResourceHandler resourceHandler, long batchSize = All)
        {
            _summaryBuilder = summaryBuilder ?? throw new ArgumentNullException(nameof(summaryBuilder));
            _advanceFunction =
                WrapAdvanceFunc(advanceFunction ?? throw new ArgumentNullException(nameof(advanceFunction)));
            _moreFunction = moreFunction ?? ((s, id, n) => TaskHelper.GetCompletedTask());
            _cancelFunction = cancelFunction ?? ((s, id) => TaskHelper.GetCompletedTask());
            _cancellation = cancellation;
            _resourceHandler = resourceHandler;

            _records = new LinkedList<IRecord>();

            _state = State.Running;
            _statementId = NoStatementId;
            _fields = null;
            _batchSize = batchSize;
        }

        public IStatementResultCursor CreateCursor()
        {
            return new StatementResultCursor(GetKeysAsync, NextRecordAsync, SummaryAsync);
        }

        public async Task<string[]> GetKeysAsync()
        {
            while (_state == State.Running)
            {
                await _advanceFunction().ConfigureAwait(false);
            }

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

            while ((_state == State.Running || _state == State.Streaming || _state == State.StreamingPaused) &&
                   _records.First == null)
            {
                await _advanceFunction().ConfigureAwait(false);
            }

            first = _records.First;
            if (first != null)
            {
                _records.RemoveFirst();
            }

            return first?.Value;
        }

        public async Task<IResultSummary> SummaryAsync()
        {
            while (_state != State.Finished)
            {
                await _advanceFunction().ConfigureAwait(false);
            }

            return _summaryBuilder.Build();
        }

        public void RunCompleted(long statementId, string[] fields, IResponsePipelineError error)
        {
            _statementId = statementId;
            _fields = fields;
            _state = State.StreamingPaused;
        }

        public void PullCompleted(bool hasMore, IResponsePipelineError error)
        {
            _state = hasMore ? State.StreamingPaused : State.Finished;
        }

        public void PushRecord(object[] fieldValues)
        {
            _records.AddLast(new Record(_fields, fieldValues));
        }

        private Func<Task> WrapAdvanceFunc(Func<Task> advanceFunc)
        {
            return async () =>
            {
                if (_state == State.StreamingPaused)
                {
                    if (_cancellation.IsCancellationRequested)
                    {
                        await _cancelFunction(this, _statementId);
                    }
                    else
                    {
                        await _moreFunction(this, _statementId, _batchSize);
                        if (_state == State.StreamingPaused)
                        {
                            _state = State.Streaming;
                        }
                    }
                }

                if (_state != State.Finished)
                {
                    await advanceFunc().ConfigureAwait(false);
                }

                if (_state == State.Finished && _resourceHandler != null)
                {
                    await _resourceHandler.OnResultConsumedAsync();
                }
            };
        }


        private enum State
        {
            Running,
            StreamingPaused,
            Streaming,
            Finished,
        }
    }
}