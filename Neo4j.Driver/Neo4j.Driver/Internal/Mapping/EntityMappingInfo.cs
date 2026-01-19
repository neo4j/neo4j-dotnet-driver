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
using System.Linq;
using System.Reflection;
using Neo4j.Driver.Mapping;

namespace Neo4j.Driver.Internal.Mapping;

internal record EntityMappingInfo(
    string Path,
    EntityMappingSource EntityMappingSource,
    bool Optional = false,
    object DefaultValue = null,
    bool Explicit = false);

internal static class ExtensionsForEntityMappingInfo
{
    public static EntityMappingInfo GetEntityMappingInfo<T>(this T target) where T : ICustomAttributeProvider
    {
        var path = target switch
        {
            PropertyInfo { Name: var n } => n,
            ParameterInfo { Name: var n } => n,
            _ => throw new NotSupportedException("Only properties and parameters are supported")
        };

        var result = new EntityMappingInfo(path, EntityMappingSource.Property);
        return GetEntityMappingInfoAffectedByAttributes(result, target);
    }

    private static EntityMappingInfo GetEntityMappingInfoAffectedByAttributes(
        EntityMappingInfo info,
        ICustomAttributeProvider provider)
    {
        // check for MappingSourceAttribute
        if (provider.GetCustomAttributes(typeof(MappingSourceAttribute), false).FirstOrDefault() is
            MappingSourceAttribute sourceAttribute)
        {
            info = info with
            {
                Path = sourceAttribute.EntityMappingInfo.Path,
                EntityMappingSource = sourceAttribute.EntityMappingInfo.EntityMappingSource,
                Explicit = true
            };
        }

        var optional = provider.IsDefined(typeof(MappingOptionalAttribute), false);
        var defaultValueAttribute =
            provider.GetCustomAttributes(typeof(MappingDefaultValueAttribute), false).FirstOrDefault() as
                MappingDefaultValueAttribute;

        var defaultValue = defaultValueAttribute?.DefaultValue;
        return info with { Optional = optional, DefaultValue = defaultValue };
    }
}
