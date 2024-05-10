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
using System.Reflection;

namespace Neo4j.Driver.Mapping;

internal static class MappingExtensions
{
    private static readonly MethodInfo AsGenericMethod =
        typeof(ValueExtensions).GetMethod(nameof(ValueExtensions.As), [typeof(object)]);

    private static readonly Dictionary<Type, MethodInfo> AsMethods = new();

    public static object AsType(this object obj, Type type)
    {
        if (!AsMethods.TryGetValue(type, out var asMethod))
        {
            asMethod = AsGenericMethod.MakeGenericMethod(type);
            AsMethods[type] = asMethod;
        }

        try
        {
            return asMethod.Invoke(null, [obj]);
        }
        catch (TargetInvocationException tie)
        {
            throw tie.InnerException!;
        }
    }
}
