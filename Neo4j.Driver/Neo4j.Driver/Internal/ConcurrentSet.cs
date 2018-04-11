// Copyright (c) 2002-2018 "Neo Technology,"
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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal
{
    internal class ConcurrentSet <T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, bool> _dictionary;

        public ConcurrentSet()
        {
            _dictionary = new ConcurrentDictionary<T, bool>();
        }

        /// <summary>
        /// true if the item was added to the set successfully; false if the item already exists.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryAdd(T item)
        {
            return _dictionary.TryAdd(item, true);
        }

        /// <summary>
        /// true if the item was removed from the set successfully; false if the item does not exists.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryRemove(T item)
        {
            bool value;
            return _dictionary.TryRemove(item, out value);
        }

        public int Count => _dictionary.Count;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _dictionary.Keys.GetEnumerator();
        }

        public override string ToString()
        {
            return _dictionary.Keys.ToContentString();
        }
    }
}
