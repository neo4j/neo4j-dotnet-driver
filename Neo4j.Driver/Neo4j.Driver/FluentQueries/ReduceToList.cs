﻿// Copyright (c) "Neo4j"
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

using System.Collections.Generic;

namespace Neo4j.Driver;

internal static class ReduceToList<T>
{
    public static List<T> Seed() => new();

    public static List<T> Accumulate(List<T> list, T item)
    {
        list.Add(item);
        return list;
    }

    public static IReadOnlyList<T> SelectResult(List<T> accumulation)
    {
        return accumulation;
    }
}
