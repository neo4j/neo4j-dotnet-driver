// Copyright (c) "Neo4j"
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

using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Result
{
    internal class ConsumableResultCursor : IInternalResultCursor
    {
        private readonly IInternalResultCursor _cursor;
        private bool _isConsumed;

        public ConsumableResultCursor(IInternalResultCursor cursor)
        {
            _cursor = cursor;
        }

        public Task<string[]> KeysAsync()
        {
            return _cursor.KeysAsync();
        }

        public Task<IResultSummary> ConsumeAsync()
        {
            _isConsumed = true;
            return _cursor.ConsumeAsync();
        }

        public Task<IRecord> PeekAsync()
        {
            AssertNotConsumed();
            return _cursor.PeekAsync();
        }

        public Task<bool> FetchAsync()
        {
            AssertNotConsumed();
            return _cursor.FetchAsync();
        }

        public IRecord Current
        {
            get
            {
                AssertNotConsumed();
                return _cursor.Current;
            }
        }

        public void Cancel()
        {
            _cursor.Cancel();
        }

        protected void AssertNotConsumed()
        {
            if (_isConsumed)
            {
                throw ErrorExtensions.NewResultConsumedException();
            }
        }
    }

    internal class ConsumableResultCursor<T> : ConsumableResultCursor, IInternalResultCursor<T>
    {
        private readonly IInternalResultCursor<T> _cursor;

        public ConsumableResultCursor(IInternalResultCursor<T> cursor) : base(cursor)
        {
            _cursor = cursor;
        }

        public new T Current => _cursor.Current;
        
        public new Task<T> PeekAsync()
        {
            AssertNotConsumed();
            return _cursor.PeekAsync();
        }
    }
}