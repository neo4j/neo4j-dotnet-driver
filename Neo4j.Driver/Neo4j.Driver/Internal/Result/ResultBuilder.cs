// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Linq;
using static Neo4j.Driver.V1.StatementType;
using System;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Result
{
    internal class ResultBuilder : BuilderBase
    {
        private Action _receiveOneAction;

        private readonly Queue<IRecord> _records = new Queue<IRecord>();
        private bool _hasMoreRecords = true;

        public ResultBuilder() : this(null, null, null, null, null)
        {
        }

        public ResultBuilder(Statement statement, Action receiveOneAction, IServerInfo server, IResultResourceHandler resourceHandler = null)
            : base(statement, server, resourceHandler)
        {
            SetReceiveOneAction(receiveOneAction);
        }

        public ResultBuilder(string statement, IDictionary<string, object> parameters, 
            Action receiveOneAction, IServerInfo server, IResultResourceHandler resourceHandler= null)
            : this(new Statement(statement, parameters), receiveOneAction, server, resourceHandler)
        {
        }

        public StatementResult PreBuild()
        {
            return new StatementResult(_keys, new RecordSet(NextRecord), Summary);
        }

        /// <summary>
        /// Buffer all records left unread into memory and return the summary
        /// </summary>
        /// <returns>The final summary</returns>
        private IResultSummary Summary()
        {
            // read all records into memory
            while (_hasMoreRecords)
            {
                _receiveOneAction.Invoke();
            }
            // return the summary
            return _summaryBuilder.Build();
        }

        /// <summary>
        /// Return next record in the record stream if any, otherwise return null
        /// </summary>
        /// <returns>Next record in the record stream if any, otherwise return null</returns>
        private IRecord NextRecord()
        {
            if (_records.Count > 0)
            {
                return _records.Dequeue();
            }
            while (_hasMoreRecords && _records.Count <= 0)
            {
                _receiveOneAction.Invoke();
            }
            return _records.Count > 0 ? _records.Dequeue() : null;
        }

        internal void SetReceiveOneAction(Action receiveOneAction)
        {
            _receiveOneAction = () =>
            {
                receiveOneAction.Invoke();
                if (!_hasMoreRecords)
                {
                    // The last message received is a reply to pull_all,
                    // we are good to do a reset and return the connection to pool
                    _resourceHandler?.OnResultConsumed();
                }
            };
        }

        public override void CollectRecord(object[] fields)
        {
            var record = new Record(_keys, fields);
            _records.Enqueue(record);
        }
        
        public override void CollectSummary(IDictionary<string, object> meta)
        {
            NoMoreRecords();

            base.CollectSummary(meta);
        }

        public override void DoneSuccess()
        {
            // do nothing
        }

        public override void DoneFailure()
        {
            NoMoreRecords();// an error received, so the result is broken
        }

        public override void DoneIgnored()
        {
            NoMoreRecords();// the result is ignored
        }

        private void NoMoreRecords()
        {
            _hasMoreRecords = false;
        }
        
    }
}