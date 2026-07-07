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

namespace Neo4j.Driver.Internal.Encryption;

internal sealed class PropertyTypeValidator : IPropertyTypeValidator
{
    public void EnsureSupported(object value)
    {
        if (!IsSupported(value, allowList: true))
        {
            var typeName = value?.GetType().FullName ?? "null";
            throw new ArgumentException(
                $"Value of type '{typeName}' is not a supported Neo4j property type.",
                nameof(value));
        }
    }

    private static bool IsSupported(object value, bool allowList)
    {
        switch (value)
        {
            case bool:
            case long:
            case double:
            case string:
            case byte[]:
                return true;

            case IDictionary:
                return false;

            case IEnumerable enumerable when allowList:
                foreach (var item in enumerable)
                {
                    if (!IsSupported(item, allowList: false))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return false;
        }
    }
}
