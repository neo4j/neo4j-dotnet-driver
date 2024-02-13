// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.Internal.Result;

internal struct ArrayToReadOnlyListWrapper<T> : IReadOnlyList<T>
{
    private readonly T[] array;

    public ArrayToReadOnlyListWrapper(T[] array)
    {
        this.array = array ?? throw new ArgumentNullException(nameof(array));
    }

    public T this[int index] => array[index];

    public int Count => array.Length;

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)array).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return array.GetEnumerator();
    }
}
