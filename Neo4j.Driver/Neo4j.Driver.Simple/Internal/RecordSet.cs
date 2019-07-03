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

using System.Collections.Generic;

namespace Neo4j.Driver.Internal
{
    internal interface IRecordSet
    {
        IEnumerable<IRecord> Records();
        IRecord Peek();
    }

    internal class RecordSet : IRecordSet
    {
        private readonly IStatementResultCursor _cursor;
        private readonly BlockingExecutor _executor;

        public RecordSet(IStatementResultCursor cursor, BlockingExecutor executor)
        {
            _cursor = cursor;
            _executor = executor;
        }

        public IEnumerable<IRecord> Records()
        {
            while (_executor.RunSync(() => _cursor.FetchAsync()))
            {
                yield return _cursor.Current;
            }
        }

        public IRecord Peek()
        {
            return _executor.RunSync(() => _cursor.PeekAsync());
        }
    }
}