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

using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Routing
{
    internal interface IAddressSet<T>
    {
        /// <summary>
        /// Add one item into this set.
        /// </summary>
        /// <param name="item">The item to add</param>
        void Add(T item);

        /// <summary>
        /// Adds several _items into this set.
        /// </summary>
        /// <param name="items">The _items to add</param>
        void Add(IEnumerable<T> items);

        /// <summary>
        /// Remove one item from this set
        /// </summary>
        /// <param name="item"></param>
        void Remove(T item);

        /// <summary>
        /// Clean all the item inside this set
        /// </summary>
        void Clear();

        IList<T> Snaphost { get; }

        /// <summary>
        /// Not thread safe
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Not thread safe
        /// </summary>
        bool IsEmpty { get; }
    }
}
