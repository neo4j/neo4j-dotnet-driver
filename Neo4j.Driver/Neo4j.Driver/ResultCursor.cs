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

namespace Neo4j.Driver
{
    /// <summary>
    /// The result returned from the Neo4j instance
    /// </summary>
    public class ResultCursor : IExtendedResultCursor, IResultRecordAccessor, IResources
    {
        //private readonly ResultSummary summary;
        private readonly IPeekingEnumerator<Record> _enumerator;
        private List<string> _keys;
        public IReadOnlyList<string>  Keys => _keys;
        private long _position = -1;
        private long _limit = -1;
        private Record _current;
        private bool _open = true;

        internal ResultCursor( IPeekingEnumerator<Record> recordEnumerator, string[] keys)
        {
            _enumerator = recordEnumerator;
            _keys = new List<string>(keys);
        }

        public ResultCursor(IEnumerable<Record> records, string[] keys)
        {
            Throw.ArgumentNullException.IfNull(records, nameof(records));
            Throw.ArgumentNullException.IfNull(keys, nameof(keys));

            _enumerator = new PeekingEnumerator<Record>(records.GetEnumerator());
            _keys = new List<string>(keys);
        }

        public dynamic Value(int index)
        {
            return _current[index];
        }

        public dynamic Value(string key)
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

        public IReadOnlyDictionary<string, dynamic> Values()
        {
            return _current?.Values;
        } 

        public IReadOnlyList<KeyValuePair<string, dynamic>> OrderedValues()
        {
            if (_current == null)
            {
                return null;
            }
            return Keys.Select(key => new KeyValuePair<string, dynamic>(key, _current.Values[key])).ToList();
        }

        public IEnumerable<Record> Stream()
        {
            while (Next())
            {
                yield return _current;
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
            else
            {
                return false;
            }
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
        public Record Record()
        {
            if (HasRecord())
            {
                return _current;
            }
            else
            {
                throw new InvalidOperationException("In order to access the fields of a record in a result, " +
                                                    "you must first call next() to point the result to the next record in the result stream.");
            }
        }

        public long Position()
        {
            AssertOpen();
            return _position;
        }

        private void AssertOpen()
        {
            if (!_open)
            {
                throw new InvalidOperationException("Cursor already closed");
            }
        }

        public bool AtEnd()
        {
            AssertOpen();
            return !_enumerator.HasNext();
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
            long pos = Position();
            return pos < 0 ? Next() : pos == 0;
        }

        public bool Single()
        {
            return First() && AtEnd();
        }

        //TODO: Does DIPS expect exception here?
        public Record Peek()
        {
            return _enumerator.Peek();
        }

        private bool IsEmpty()
        {
            return _position == -1 && !_enumerator.HasNext();
        }

        public IList<Record> List()
        {
            return List(r => r);
        }

        public IList<T> List<T>(Func<Record, T> mapFunction)
        {
            if (IsEmpty())
            {
                AssertOpen();
                return new List<T>();
            }
            if (First())
            {
                var result = new List<T>();
                do
                {
                    result.Add(mapFunction(_current));
                } while (Next());
                _enumerator.Discard();
                return result;
            }

            throw new InvalidOperationException(
                $"Can't retain records when cursor is not pointing at the first record (currently at position {_position})");
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

    public interface IResources :IDisposable
    {
        bool IsOpen();
        void Close();
    }
}