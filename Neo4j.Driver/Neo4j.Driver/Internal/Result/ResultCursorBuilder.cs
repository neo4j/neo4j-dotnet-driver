// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Collections.Generic;
using System;
using Neo4j.Driver.V1;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Result
{
    internal class ResultCursorBuilder : ResultBuilderBase
    {
        private Func<Task> _receiveOneFunc;

        private readonly IResultResourceHandler _resourceHandler;
        private readonly Queue<IRecord> _records = new Queue<IRecord>();
        private bool _hasMoreRecords = true;

        public ResultCursorBuilder(SummaryCollector summaryCollector, Func<Task> receiveOneFunc, IResultResourceHandler resourceHandler = null)
            : base(summaryCollector)
        {
            SetReceiveOneFunc(receiveOneFunc);
            _resourceHandler = resourceHandler;
        }

        public async Task<IStatementResultCursor> PreBuildAsync()
        {
            await GetKeys().ConfigureAwait(false);
            return new StatementResultCursor(Keys, NextRecordAsync, SummaryAsync);
        }

        private async Task<List<string>> GetKeys()
        {
            while (!StatementProcessed)
            {
                await _receiveOneFunc().ConfigureAwait(false);
            }
            return Keys;
        }

        /// <summary>
        /// Buffer all records left unread into memory and return the summary
        /// </summary>
        /// <returns>The final summary</returns>
        private async Task<IResultSummary> SummaryAsync()
        {
            // read all records into memory
            while (_hasMoreRecords)
            {
                await _receiveOneFunc().ConfigureAwait(false);
            }
            // return the summary
            return SummaryCollector.Build();
        }

        /// <summary>
        /// Return next record in the record stream if any, otherwise return null
        /// </summary>
        /// <returns>Next record in the record stream if any, otherwise return null</returns>
        private async Task<IRecord> NextRecordAsync()
        {
            if (_records.Count > 0)
            {
                return _records.Dequeue();
            }
            while (_hasMoreRecords && _records.Count <= 0)
            {
                await _receiveOneFunc().ConfigureAwait(false);
            }
            return _records.Count > 0 ? _records.Dequeue() : null;
        }

        internal void SetReceiveOneFunc(Func<Task> receiveOneAction)
        {
            _receiveOneFunc = async () =>
            {
                await receiveOneAction().ConfigureAwait(false);
                if (!_hasMoreRecords && _resourceHandler != null)
                {
                    // The last message received is a reply to pull_all,
                    // we are good to do a reset and return the connection to pool
                    await _resourceHandler.OnResultConsumedAsync().ConfigureAwait(false);
                }
            };
        }

        protected override void EnqueueRecord(Record record)
        {
            _records.Enqueue(record);
        }

        protected override void NoMoreRecords()
        {
            _hasMoreRecords = false;
        }

    }
}
