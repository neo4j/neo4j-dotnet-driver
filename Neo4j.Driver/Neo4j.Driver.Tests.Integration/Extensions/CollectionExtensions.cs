// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

namespace Neo4j.Driver.IntegrationTests.Extensions;

public static class CollectionExtensions
{
    public static T RandomElement<T>(this IEnumerable<T> list)
    {
        var random = new Random(Guid.NewGuid().GetHashCode());
        var eagerList = list.ToList();
        var index = random.Next(0, eagerList.Count());
        return eagerList.ElementAt(index);
    }

    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
    {
        var nextBatch = new List<T>(batchSize);
        foreach (var item in collection)
        {
            nextBatch.Add(item);
            if (nextBatch.Count == batchSize)
            {
                yield return nextBatch;

                nextBatch = new List<T>(batchSize);
            }
        }

        if (nextBatch.Count > 0)
        {
            yield return nextBatch;
        }
    }
}
