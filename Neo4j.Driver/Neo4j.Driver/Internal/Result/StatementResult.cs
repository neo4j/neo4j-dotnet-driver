//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using Neo4j.Driver.Extensions;

namespace Neo4j.Driver.Internal.Result
{
    /// <summary>
    /// The result returned from the Neo4j instance
    /// </summary>
    public class StatementResult : IStatementResult
    {
        private IResultSummary _summary;
        private readonly IPeekingEnumerator<Record> _enumerator;
        private List<string> _keys;
        private Func<IResultSummary> _getSummary;
        private bool _disposed = false;
        internal long Position => _enumerator.Position;

        internal StatementResult(string[] keys, IPeekingEnumerator<Record> recordEnumerator)
        {
            _keys = new List<string>(keys);
            _enumerator = recordEnumerator;
            _summary = null;
        }

        public StatementResult(string[] keys, IEnumerable<Record> records, Func<IResultSummary> getSummary = null)
        {
            Throw.ArgumentNullException.IfNull(keys, nameof(keys));
            Throw.ArgumentNullException.IfNull(records, nameof(records));

            _keys = new List<string>(keys);
            _enumerator = new PeekingEnumerator<Record>(records.GetEnumerator());
            _getSummary = getSummary;
        }

        public IReadOnlyList<string> Keys => _keys;

        public IResultSummary Summary
        {
            get
            {
                if (!AtEnd)
                {
                    throw new InvalidOperationException("Cannot get summary before consuming all records.");
                }
                if (_summary == null && _getSummary != null)
                {
                    _summary = _getSummary();
                }
                return _summary;
            }
        }

        internal bool AtEnd => _enumerator.Peek() == null;

        public IRecord Single()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(StatementResult));

            if (_enumerator.Position >= 0)
            {
                throw new InvalidOperationException("The first record is already consumed.");
            }
            if (!_enumerator.MoveNext())
            {
                throw new InvalidOperationException("No record found.");
            }
            var record = _enumerator.Current;
            if (_enumerator.Peek() != null)
            {
                throw new InvalidOperationException("More than one record found.");
            }
            return record;
        }

        public IRecord Peek()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(StatementResult));

            return _enumerator.Peek();
        }

        public IResultSummary Consume()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(StatementResult));

            _enumerator.Consume();
            return Summary;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing || _disposed)
            {
                return;
            }

            _enumerator.Dispose();
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IEnumerator<IRecord> GetEnumerator()
        {
            return _enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}