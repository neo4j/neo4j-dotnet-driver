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

using System.Linq;
using System.Reflection;

namespace Neo4j.Driver.Mapping;

internal record EntityMappingInfo(
    string Path,
    EntityMappingSource EntityMappingSource,
    bool Optional = false,
    object DefaultValue = null);

internal static class ExtensionsForEntityMappingInfo
{
    public static EntityMappingInfo GetEntityMappingInfo(this PropertyInfo propertyInfo)
    {
        var path = propertyInfo.Name;
        var result = new EntityMappingInfo(path, EntityMappingSource.Property);
        result = GetEntityMappingInfoAffectedByAttributes(result, propertyInfo);
        return result;
    }

    public static EntityMappingInfo GetEntityMappingInfo(this ParameterInfo parameterInfo)
    {
        var path = parameterInfo.Name;
        var result = new EntityMappingInfo(path, EntityMappingSource.Property);
        result = GetEntityMappingInfoAffectedByAttributes(result, parameterInfo);
        return result;
    }

    private static EntityMappingInfo GetEntityMappingInfoAffectedByAttributes(
        EntityMappingInfo info,
        ICustomAttributeProvider provider)
    {
        // check for MappingSourceAttribute
        var sourceAttribute =
            provider.GetCustomAttributes(typeof(MappingSourceAttribute), false).FirstOrDefault() as MappingSourceAttribute;

        if (sourceAttribute is not null)
        {
            info = info with
            {
                Path = sourceAttribute.EntityMappingInfo.Path,
                EntityMappingSource = sourceAttribute.EntityMappingInfo.EntityMappingSource
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
