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

namespace Neo4j.Driver.Internal.Result
{
    internal class ResultStreamBuilder
    {
        private readonly long _batchSize = 1000;
        private readonly Func<Task> _advanceFunction;
        private readonly Func<long, Task> _moreFunction;
        private readonly Func<Task> _cancelFunction;
        private readonly CancellationToken _cancellation;
        private readonly IResultResourceHandler _resourceHandler;

        private readonly LinkedList<IRecord> _records;

        private volatile bool _runCompleted;
        private volatile bool _hasMoreRecords;
        private volatile bool _pullCompleted;

        public ResultStreamBuilder(Statement statement, IServerInfo serverInfo, Func<Task> advanceFunction,
            Func<long, Task> moreFunction, Func<Task> cancelFunction, CancellationToken cancellation,
            IResultResourceHandler resourceHandler)
        {
            _advanceFunction =
                WrapAdvanceFunc(advanceFunction ?? throw new ArgumentNullException(nameof(advanceFunction)));
            _moreFunction = moreFunction ?? (n => TaskHelper.GetCompletedTask());
            _cancelFunction = cancelFunction ?? TaskHelper.GetCompletedTask;
            _cancellation = cancellation;
            _resourceHandler = resourceHandler;

            _records = new LinkedList<IRecord>();

            _hasMoreRecords = true;
            _runCompleted = false;
            _pullCompleted = false;

            Summary = new SummaryBuilder(statement, serverInfo);
            Fields = null;
        }

        internal SummaryBuilder Summary { get; }

        internal string[] Fields { get; set; }

        public IStatementResultCursor CreateCursor()
        {
            return new StatementResultCursor(GetKeysAsync, NextRecordAsync, SummaryAsync);
        }

        public async Task<string[]> GetKeysAsync()
        {
            while (!_runCompleted)
            {
                await _advanceFunction().ConfigureAwait(false);
            }

            return Fields ?? new string[0];
        }

        public async Task<IRecord> NextRecordAsync()
        {
            var first = _records.First;
            if (first != null)
            {
                _records.RemoveFirst();
                return first.Value;
            }

            while (_hasMoreRecords && _records.First == null)
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
            while (_hasMoreRecords)
            {
                await _advanceFunction().ConfigureAwait(false);
            }

            return Summary.Build();
        }

        public Task RunCompletedAsync(IResponsePipelineError error)
        {
            _runCompleted = true;

            if (_cancellation.IsCancellationRequested)
            {
                return _cancelFunction();
            }

            return _moreFunction(_batchSize);
        }

        public Task PullCompletedAsync(IResponsePipelineError error, bool hasMoreRecords)
        {
            _hasMoreRecords = hasMoreRecords;
            _runCompleted = !_hasMoreRecords;

            if (_cancellation.IsCancellationRequested && hasMoreRecords)
            {
                return _cancelFunction();
            }

            if (hasMoreRecords)
            {
                return _moreFunction(_batchSize);
            }

            return TaskHelper.GetCompletedTask();
        }

        public Task PushRecordAsync(object[] fieldValues)
        {
            _records.AddLast(new Record(Fields, fieldValues));
            return TaskHelper.GetCompletedTask();
        }

        private Func<Task> WrapAdvanceFunc(Func<Task> advanceFunc)
        {
            return async () =>
            {
                await advanceFunc().ConfigureAwait(false);

                if (!_hasMoreRecords && _resourceHandler != null)
                {
                    await _resourceHandler.OnResultConsumedAsync();
                }
            };
        }
    }
}