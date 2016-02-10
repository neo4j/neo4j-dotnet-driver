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

using System.Collections.Generic;

namespace Neo4j.Driver.Internal
{
    public interface IPeekingEnumerator<T> where T : class
    {
        bool HasNext();
        T Next();
        T Peek();
        void Discard();
    }

    public class PeekingEnumerator<T> : IPeekingEnumerator<T> where T:class
    {
        private IEnumerator<T> _enumerator;

        public PeekingEnumerator(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        private T _cached;

        public bool HasNext()
        {
            return CacheNext();
        }

        public T Next()
        {
            if (CacheNext())
            {
                T result = _cached;
                _cached = null;
                return result;
            }
            else
            {
                return null;
            }
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
            return CacheNext() ? _cached : null;
        }

        public void Discard()
        {
            _cached = null;
            _enumerator = null;
        }
    }
}