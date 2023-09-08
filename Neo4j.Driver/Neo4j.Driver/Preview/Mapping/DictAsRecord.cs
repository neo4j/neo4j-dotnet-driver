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

namespace Neo4j.Driver.Preview.Mapping;

internal class DictAsRecord : IRecord
{
    private readonly IReadOnlyDictionary<string, object> _dict;
    private readonly IRecord _record;

    public DictAsRecord(IReadOnlyDictionary<string, object> dict, IRecord record)
    {
        _dict = dict;
        _record = record;
    }

    public IRecord Record => _record;
    public object this[int index] => _dict.TryGetValue(_dict.Keys.ElementAt(index), out var obj) ? obj : null;
    public object this[string key] => _dict.TryGetValue(key, out var obj) ? obj : null;
    public IReadOnlyDictionary<string, object> Values => _dict;
    public IReadOnlyList<string> Keys => _dict.Keys.ToList();
}
