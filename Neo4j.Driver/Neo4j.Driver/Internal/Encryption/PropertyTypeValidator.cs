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
using System.Linq;

namespace Neo4j.Driver.Internal.Encryption;

internal class PropertyTypeValidator : IPropertyTypeValidator
{
    public void EnsureSupported(object value)
    {
        var isSupported = IsSupported(value, allowList: true);
        if (isSupported)
        {
            return;
        }

        var typeName = value?.GetType().FullName ?? "null";
        throw new ArgumentException(
            $"Value of type '{typeName}' is not a supported Neo4j property type.",
            nameof(value));
    }

    private static bool IsSupported(object value, bool allowList)
    {
        return value switch
        {
            bool or long or double or string or byte[] => true,
            
            // if this isn't explicitly disallowed, an empty dictionary
            // would pass the next check and be treated as a valid property type
            IDictionary => false,

            IEnumerable e when allowList => e.Cast<object>().All(o => IsSupported(o, allowList: false)), 
            _ => false
        };
    }
}
