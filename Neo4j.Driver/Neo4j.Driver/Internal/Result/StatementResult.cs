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
using System.Collections;
using System.Collections.Generic;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Result
{
    /// <summary>
    /// The result returned from the Neo4j instance
    /// </summary>
    internal class StatementResult : IStatementResult
    {
        private readonly Func<List<string>> _keys;
        private readonly Func<IResultSummary> _getSummary;
        private readonly IRecordSet _recordSet;

        private IResultSummary _summary;

        public StatementResult(Func<List<string>> keys, IRecordSet recordSet, Func<IResultSummary> getSummary = null)
        {
            Throw.ArgumentNullException.IfNull(keys, nameof(keys));
            Throw.ArgumentNullException.IfNull(recordSet, nameof(recordSet));

            _keys = keys;
            _recordSet = recordSet;
            _getSummary = getSummary;
        }

        public IReadOnlyList<string> Keys => _keys();

        public IResultSummary Summary
        {
            get
            {
                if (_summary == null && _getSummary != null)
                {
                    _summary = _getSummary();
                }
                return _summary;
            }
        }

        internal bool AtEnd => _recordSet.AtEnd;

        public IRecord Peek()
        {
            return _recordSet.Peek();
        }

        public IResultSummary Consume()
        {
            foreach (var record in _recordSet.Records())
            {
                // Do nothing, just consume the records
            }
            return Summary;
        }

        public IEnumerator<IRecord> GetEnumerator()
        {
            return _recordSet.Records().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
    }
}
