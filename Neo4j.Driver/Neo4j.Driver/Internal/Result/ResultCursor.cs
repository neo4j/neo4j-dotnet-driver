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
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Extensions;

namespace Neo4j.Driver.Internal.Result
{
    /// <summary>
    /// The result returned from the Neo4j instance
    /// </summary>
    public class ResultCursor : IExtendedResultCursor
    {
        private IResultSummary _summary;
        private readonly IPeekingEnumerator<Record> _enumerator;
        private List<string> _keys;
        public IReadOnlyList<string>  Keys => _keys;
        private long _position = -1;
        private long _limit = -1;
        private Record _current;
        private bool _open = true;
        private Func<IResultSummary> _getSummary; 

        internal ResultCursor(string[] keys, IPeekingEnumerator<Record> recordEnumerator)
        {
            _keys = new List<string>(keys);
            _enumerator = recordEnumerator;
            _summary = null;
        }

        public ResultCursor(string[] keys, IEnumerable<Record> records, Func<IResultSummary> getSummary = null)
        {
            Throw.ArgumentNullException.IfNull(keys, nameof(keys));
            Throw.ArgumentNullException.IfNull(records, nameof(records));

            _keys = new List<string>(keys);
            _enumerator = new PeekingEnumerator<Record>(records.GetEnumerator());
            _getSummary = getSummary;
        }

        public object Get(int index)
        {
            return _current[index];
        }

        public object Get(string key)
        {
            return _current[key];
        }

        public bool ContainsKey(string key)
        {
            return Keys.Contains(key);
        }

        public int Index(string key)
        {
            return _keys.FindIndex(c => c == key);
        }

        public int Size()
        {
            return Keys.Count;
        }

        public IReadOnlyDictionary<string, object> Values()
        {
            EnsureCurrentIsCalled();
            return _current.Values;
        }

        private void EnsureCurrentIsCalled()
        {
            if (!HasRecord())
            {
                throw new InvalidOperationException(
                    "In order to access the fields of a record in a result, " +
                    "you must first call next() to point the result to the next record in the result stream.");
            }
        }

        public IReadOnlyList<KeyValuePair<string, object>> OrderedValues()
        {
            EnsureCurrentIsCalled();
            return Keys.Select(key => new KeyValuePair<string, object>(key, _current.Values[key])).ToList();
        }

        public IEnumerable<Record> Stream()
        {
            while (Next())
            {
                yield return _current;
            }
        }

        public IResultSummary Summary
        {
            get
            {
                if (!AtEnd)
                {
                    throw new ClientException("Cannot get summary before reading all records.");
                }
                _enumerator.Discard();
                if (_summary == null && _getSummary != null)
                {
                    _summary = _getSummary();
                }
                return _summary;
            }
        }

        public bool Next()
        {
            AssertOpen();
            if (_enumerator.HasNext())
            {
                _current = _enumerator.Next();
                _position += 1;
                if (_position == _limit)
                {
                    _enumerator.Discard();
                }
                return true;
            }
            return false;
        }

        public bool HasRecord()
        {
            AssertOpen();
            return _current != null;
        }

        /// <summary>
        /// Return an immutable copy of the currently viewed record
        /// </summary>
        /// <returns></returns>
        public IRecord Record()
        {
            EnsureCurrentIsCalled();
            return _current;
           
        }

        public long Position
        {
            get
            {
                AssertOpen();
                return _position;
            }
        }

        private void AssertOpen()
        {
            if (!_open)
            {
                throw new InvalidOperationException("Cursor already closed");
            }
        }

        public bool AtEnd
        {
            get
            {
                AssertOpen();
                return !_enumerator.HasNext();
            }
        }

        public long Skip(long size)
        {
            Throw.ArgumentOutOfRangeException.IfValueLessThan(size, 0, nameof(size));

            int skipped = 0;
            while (skipped < size && Next())
            {
                skipped += 1;
            }
            return skipped;

        }

        public long Limit(long records)
        {
            Throw.ArgumentOutOfRangeException.IfValueLessThan(records, 0, nameof(records));
            if (records == 0)
            {
                _limit = _position;
                _enumerator.Discard();
            }
            else
            {
                _limit = records + _position;
            }
            return _limit;
        }

        public bool First()
        {
            long pos = Position;
            return pos < 0 ? Next() : pos == 0;
        }

        public bool Single()
        {
            return First() && AtEnd;
        }

        public Record Peek()
        {
            return _enumerator.Peek();
        }

        private bool IsEmpty()
        {
            return _position == -1 && !_enumerator.HasNext();
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }

            Close();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsOpen()
        {
            return _open;
        }

        public void Close()
        {
            if (_open)
            {
                _enumerator.Discard();
                _open = false;
            }
            else
            {
                throw new InvalidOperationException("Already closed");
            }
        }
    }

}