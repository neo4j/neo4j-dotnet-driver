// Copyright (c) 2002-2020 "Neo4j,"
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
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Util
{
    internal class ConcurrentOrderedSet<T> : ICollection<T>
    {
        private readonly object _itemsLock = new object();
        private volatile List<T> _items = new List<T>();

        /// <summary>
        ///     Add one item into this set.
        /// </summary>
        /// <param name="item">The item to add</param>
        public void Add(T item)
        {
            lock (_itemsLock)
            {
                if (!_items.Contains(item))
                {
                    var newItems = new List<T>(_items);
                    newItems.Add(item);
                    _items = newItems;
                }
            }
        }

        /// <summary>
        ///     Adds several _items into this set.
        /// </summary>
        /// <param name="items">The _items to add</param>
        public void Add(IEnumerable<T> items)
        {
            lock (_itemsLock)
            {
                var newItems = new List<T>(_items);
                foreach (var item in items)
                {
                    if (!newItems.Contains(item))
                    {
                        newItems.Add(item);
                    }
                }

                _items = newItems;
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///     Remove one item from this set
        /// </summary>
        /// <param name="item"></param>
        public bool Remove(T item)
        {
            lock (_itemsLock)
            {
                if (_items.Contains(item))
                {
                    var newItems = new List<T>(_items);
                    newItems.Remove(item);
                    _items = newItems;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Clean all the item inside this set
        /// </summary>
        public void Clear()
        {
            lock (_itemsLock)
            {
                _items = new List<T>();
            }
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        /// <summary>
        ///     Number of items in this set
        /// </summary>
        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public IList<T> Snapshot => _items.AsReadOnly();

        /// <summary>
        ///     Check if this set is empty
        /// </summary>
        public bool IsEmpty => _items.Count == 0;

        public override string ToString()
        {
            return string.Join(", ", _items);
        }

        /// <returns>The enumerator of the current snapshot of the set</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <returns>The enumerator of the current snapshot of the set</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}