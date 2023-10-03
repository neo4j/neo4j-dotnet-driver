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
using System.ComponentModel;
using System.Linq;

namespace Neo4j.Driver.Internal.Util;

internal static class EnumUtils
{
    public static string GetDescription<T>(this T value) where T : Enum
    {
        // get the Description attribute on the enum member
        var descriptionAttribute =
            value.GetType()
                .GetField(value.ToString())
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() as DescriptionAttribute;

        // if the Description attribute exists, return its value, otherwise return the enum member name
        return descriptionAttribute?.Description ?? value.ToString();
    }
}
