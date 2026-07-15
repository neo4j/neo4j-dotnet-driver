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

#nullable enable

using System;
using System.Collections;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class PropertyTypeNamer : IPropertyTypeNamer
{
    public string GetValidTypeName(object value)
    {
        return GetValidTypeName(value, allowList: true);
    }

    private static string GetValidTypeName(object value, bool allowList)
    {
        return value switch
        {
            bool => "BOOLEAN",
            long => "INTEGER",
            double => "FLOAT",
            string => "STRING",
            byte[] => "BYTES",

            // if this isn't explicitly disallowed, an empty dictionary
            // would pass the next check and be treated as a valid property type
            IDictionary => throw Unsupported(value),

            IEnumerable e when allowList => ValidateList(e),

            _ => throw Unsupported(value)
        };
    }

    private static string ValidateList(IEnumerable list)
    {
        foreach (var item in list)
        {
            GetValidTypeName(item, allowList: false);
        }

        return "LIST";
    }

    private static ArgumentException Unsupported(object value)
    {
        var typeName = value?.GetType().FullName ?? "null";
        return new ArgumentException(
            $"Value of type '{typeName}' is not a supported Neo4j property type.",
            nameof(value));
    }
}
