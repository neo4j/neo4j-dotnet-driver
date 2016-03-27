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

namespace Neo4j.Driver.Internal.Result
{
    public interface IPeekingEnumerator<T> : IEnumerator<T> where T : class
    {
        T Peek();
        void Consume();
        long Position { get; }
    }

    public class PeekingEnumerator<T> : IPeekingEnumerator<T> where T:class
    {
        private IEnumerator<T> _enumerator;
        private T _cached;
        private T _current;
        private bool _hasConsumed = false;
        private int _position = -1;
        private bool _disposed = false;
        public long Position => _position;

        public PeekingEnumerator(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public T Current => _current;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_disposed) return false;

            if (CacheNext())
            {
                _current = _cached;
                _position ++;
                _cached = null;
                return true;
            }
            if(_current != null)
            {
                _current = null;
                _position ++;
                return false;
            }
            return false;
        }

        public void Reset()
        {
            throw new InvalidOperationException("Cannot revisit the records in result");
        }

        /// <summary>
        /// Get the next item if it is not available yet and save it to cached.
        /// </summary>
        /// <returns></returns>
        private bool CacheNext()
        {
            if (_cached == null)
            {
                if (_enumerator == null || !_enumerator.MoveNext())
                {
                    return false;
                }

                _cached = _enumerator.Current;
                return true;
            }
            return true;
        }

        public T Peek()
        {
            if (_disposed) return default(T);
            return CacheNext() ? _cached : null;
        }

        public void Consume()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PeekingEnumerator<T>));
            if (_hasConsumed)
            {
                return;
            }
            // drain all the records
            while (_enumerator.MoveNext())
            {
                _position ++;
            }
            _position ++;
            _cached = null;
            _current = null;
            _enumerator = null;
            _hasConsumed = true;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}