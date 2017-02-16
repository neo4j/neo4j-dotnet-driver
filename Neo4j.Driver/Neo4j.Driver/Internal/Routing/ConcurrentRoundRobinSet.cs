// Copyright (c) 2002-2017 "Neo Technology,"
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
using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Routing
{
    // The current impl uses lock to protect from concurrent access to the elements in this set.
    // Make this class lock free if it is possible.
    internal class ConcurrentRoundRobinSet<T> : IEnumerable<T>
    {
        private readonly IList<T> _items = new List<T>();
        private int _index = 0;

        /// <summary>
        /// Add one item into this set.
        /// </summary>
        /// <param name="item">The item to add</param>
        public void Add(T item)
        {
            lock (_items)
            {
                if (!_items.Contains(item))
                {
                    _items.Add(item);
                }
            }
        }

        /// <summary>
        /// Adds several _items into this set.
        /// </summary>
        /// <param name="items">The _items to add</param>
        public void Add(IEnumerable<T> items)
        {
            lock (_items)
            {
                foreach (var item in items)
                {
                    Add(item);
                }
            }
        }

        /// <summary>
        /// Remove one item from this set
        /// </summary>
        /// <param name="item"></param>
        public void Remove(T item)
        {
            lock (_items)
            {
                var pos = _items.IndexOf(item);
                _items.Remove(item);
                if (_index > pos)
                {
                    _index--;
                }
            }
        }

        /// <summary>
        /// Round robin to get the next item in the set
        /// </summary>
        /// <param name="value">The next item in the set</param>
        /// <returns>true if succesfully find an item, otherwise false if the set is empty</returns>
        public bool TryNext(out T value)
        {
            lock (_items)
            {
                if (_items.Count == 0)
                {
                    value = default(T);
                    return false;
                }
                // ensure the index is in range
                _index = _index % _items.Count;
                value = _items[_index++];
            }
            return true;
        }

        /// <summary>
        /// Clean all the item inside this set
        /// </summary>
        public void Clear()
        {
            lock (_items)
            {
                _items.Clear();
            }
        }

        /// <summary>
        /// Not thread safe
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Not thread safe
        /// </summary>
        public bool IsEmpty => _items.Count == 0;

        /// <summary>
        /// Not thread safe.
        /// </summary>
        /// <returns>The enumerator of the current snapshot of the set</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Not thread safe
        /// </summary>
        /// <returns>The eumerator of the current snapshot of the set</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Not thread safe
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Join(", ", _items);
        }
    }
}