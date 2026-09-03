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
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo4j.Driver.Internal;

internal static class EnumerableExtensions
{
    extension(Enumerable)
    {
        public static IEnumerable<T> TypedRange<T>(T start, T length)
            where T :
            struct,
            IComparisonOperators<T, T, bool>,
            IAdditionOperators<T, T, T>,
            ISubtractionOperators<T, T, T>,
            IAdditiveIdentity<T, T>,
            IIncrementOperators<T>,
            IMinMaxValue<T>
        {
            if (length < T.AdditiveIdentity)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
            }

            if (T.MaxValue - start < length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    "The range exceeds the maximum value of the type.");
            }

            var current = start;
            var end = start + length;
            while (current < end)
            {
                yield return current++;
            }
        }
    }
}
