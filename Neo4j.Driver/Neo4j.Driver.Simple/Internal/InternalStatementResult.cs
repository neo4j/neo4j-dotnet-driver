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

namespace Neo4j.Driver.Internal
{
    internal class InternalStatementResult : IStatementResult
    {
        private readonly IStatementResultCursor _cursor;
        private readonly IRecordSet _recordSet;
        private readonly BlockingExecutor _executor;

        public InternalStatementResult(IStatementResultCursor cursor, BlockingExecutor executor)
        {
            _cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));

            _cursor = cursor;
            _recordSet = new RecordSet(cursor, executor);
            _executor = executor;
        }

        public IReadOnlyList<string> Keys => _executor.RunSync(() => _cursor.KeysAsync());

        public IRecord Peek()
        {
            return _recordSet.Peek();
        }
        public IResultSummary Consume()
        {
            return _executor.RunSync(() => _cursor.ConsumeAsync());
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