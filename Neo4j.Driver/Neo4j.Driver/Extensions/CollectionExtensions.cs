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
using System.Collections;
using System.Linq;

namespace Neo4j.Driver.Extensions
{
    internal static class CollectionExtensions
    {
        public static string ValueToString(this object o)
        {
            if (o == null)
            {
                return "NULL";
            }
            if (o is string)
            {
                return o.ToString();
            }
            if (o is IDictionary)
            {
                var dict = (IDictionary) o;
                var dictStrings = (from object key in dict.Keys select $"{{{key.ValueToString()} : {dict[key].ValueToString()}}}").ToList();
                return $"[{string.Join(", ", dictStrings)}]";
            }
            if (o is IEnumerable)
            {
                var listStrings = (from object item in ((IEnumerable) o) select item.ValueToString());
                return $"[{string.Join(", ", listStrings)}]";
            }

            return o.ToString();
        }
    }
}