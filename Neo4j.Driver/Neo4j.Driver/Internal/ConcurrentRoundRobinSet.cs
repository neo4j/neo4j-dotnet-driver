// Copyright (c) 2002-2016 "Neo Technology,"
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal
{
    // The current impl uses lock to protect from concurrent access to the elements in this set.
    // Make this class lock free if it is possible.
    internal class ConcurrentRoundRobinSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        // We need a lock when we cannot finish modification of the queue in one concurrent method call
        private readonly object _syncLock = new object();

        /// <summary>
        /// Add one item into this set.
        /// </summary>
        /// <param name="item">The item to add</param>
        public void Add(T item)
        {
            lock (_syncLock)
            {
                if (!_queue.Contains(item))
                {
                    _queue.Enqueue(item);
                }
            }
        }

        /// <summary>
        /// Adds several items into this set.
        /// </summary>
        /// <param name="items">The items to add</param>
        public void Add(IEnumerable<T> items)
        {
            lock (_syncLock)
            {
                foreach (var item in items)
                {
                    if (!_queue.Contains(item))
                    {
                        _queue.Enqueue(item);
                    }
                }
            }
        }

//        public void Clear()
//        {
//            lock (_syncLock)
//            {
//                var count = _queue.Count;
//                for (var i = 0; i < count; i++)
//                {
//                    T ignore;
//                    _queue.TryDequeue(out ignore);
//                }
//            }
//        }
//
//        /// <summary>
//        /// Remove all items from the set and add the given items into the set
//        /// </summary>
//        /// <param name="items">The new items to add in this set</param>
//        public void Update(IEnumerable<T> items)
//        {
//            lock (_syncLock)
//            {
//                // Clear 
//                Clear();
//                // Add
//                Add(items);
//            }
//        }

        /// <summary>
        /// Remove one item from this set
        /// </summary>
        /// <param name="item"></param>
        public void Remove(T item)
        {
            lock (_syncLock)
            {
                // Note: no one could add or remove to the set or the queue at the same time
                var count = _queue.Count;
                for (var i = 0; i < count; i++)
                {
                    T value;
                    _queue.TryDequeue(out value);

                    if (value.Equals(item)) // found the item
                    {
                        // removed the item
                        break;
                    }
                    // if not found, then put it back to the end of the queue
                    _queue.Enqueue(value);
                }
            }
        }

        public T Hop()
        {
            T value;
            if (!TryHop(out value))
            {
                throw new InvalidOperationException("No item in set");
            }
            return value;
        }

        public bool TryHop(out T value)
        {
            lock (_syncLock)
            {
                if (_queue.Count == 0)
                {
                    value = default(T);
                    return false;
                }
                // change to no memory copy, e.g. list
                _queue.TryDequeue(out value);
                _queue.Enqueue(value);
            }
            return true;
        }

        public int Count => _queue.Count;

        public bool IsEmpty => _queue.IsEmpty;

        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}