//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Extensions;

namespace Neo4j.Driver.Internal.Result
{
    internal class Record : IRecord
    {
        public object this[int index] => Values[Values.Keys.ToList()[index]];
        public object this[string key] => Values[key];

        public IReadOnlyDictionary<string, object> Values { get; }       
        public IReadOnlyList<string> Keys { get; }

        public Record(string[] keys, object[] values)
        {
            Throw.ArgumentException.IfNotEqual(keys.Length, values.Length, nameof(keys), nameof(values));

            var valueKeys = new Dictionary<string, object>();

            for (var i =0; i < keys.Length; i ++)
            {
                valueKeys.Add( keys[i], values[i]);
            }
            Values = valueKeys;
            Keys = new List<string>(keys);
        }
    }
}