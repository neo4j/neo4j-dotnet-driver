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
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Tests.TestUtil;

internal static class TestRecord
{
    public static Record Create(string[] keys, object[] values)
    {
        var lookup = keys
            .Select((key, index) => (key, index))
            .ToDictionary(pair => pair.key, pair => pair.index);

        var invariantLookup = new Dictionary<string, int>(lookup, StringComparer.InvariantCultureIgnoreCase);
        return new Record(lookup, invariantLookup, values);
    }

    public static Record Create(params (string key, object value)[] keyValuePairs)
    {
        var keys = keyValuePairs.Select(pair => pair.key).ToArray();
        var values = keyValuePairs.Select(pair => pair.value).ToArray();
        return Create(keys, values);
    }
}
