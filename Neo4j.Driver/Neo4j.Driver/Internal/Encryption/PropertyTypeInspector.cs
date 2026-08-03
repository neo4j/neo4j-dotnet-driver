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
internal class PropertyTypeInspector : IPropertyTypeInspector
{
    private static readonly BoltValueSerializationSchemeVersion Baseline1_0 = new(1, 0);

    public PropertyTypeInfo GetPropertyTypeInfo(object value)
    {
        return GetPropertyTypeInfo(value, allowList: true);
    }

    private static PropertyTypeInfo GetPropertyTypeInfo(object value, bool allowList)
    {
        return value switch
        {
            bool => new PropertyTypeInfo("BOOLEAN", Baseline1_0),
            long => new PropertyTypeInfo("INTEGER", Baseline1_0),
            double => new PropertyTypeInfo("FLOAT", Baseline1_0),
            string => new PropertyTypeInfo("STRING", Baseline1_0),
            byte[] => new PropertyTypeInfo("BYTES", Baseline1_0),

            // if this isn't explicitly disallowed, an empty dictionary
            // would pass the next check and be treated as a valid property type
            IDictionary => throw Unsupported(value),

            IEnumerable e when allowList => GetListTypeInfo(e),

            _ => throw Unsupported(value)
        };
    }

    private static PropertyTypeInfo GetListTypeInfo(IEnumerable list)
    {
        var baseline = Baseline1_0;

        foreach (var item in list)
        {
            var itemInfo = GetPropertyTypeInfo(item, allowList: false);
            if (itemInfo.Baseline > baseline)
            {
                baseline = itemInfo.Baseline;
            }
        }

        return new PropertyTypeInfo("LIST", baseline);
    }

    private static ArgumentException Unsupported(object value)
    {
        var typeName = value?.GetType().FullName ?? "null";
        return new ArgumentException(
            $"Value of type '{typeName}' is not a supported Neo4j property type.",
            nameof(value));
    }
}
