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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neo4j.Driver.Internal.Mapping;
using Neo4j.Driver.Mapping;

namespace Neo4j.Driver.Internal.Util;

internal class ObjectToCypherParameterDictionaryConverter(
    IParameterValueTransformer parameterValueTransformer = null,
    IMappingBindingProvider mappingBindingProvider = null)
    : IObjectToCypherParameterDictionaryConverter
{
    private readonly IParameterValueTransformer _parameterValueTransformer =
        parameterValueTransformer ?? new ParameterValueTransformer();

    private readonly IMappingBindingProvider _mappingBindingProvider =
        mappingBindingProvider ?? new MappingBindingProvider();

    public IDictionary<string, object> Convert(object o)
    {
        return o switch
        {
            null => null,
            Dictionary<string, object> dict => dict,
            IDictionary<string, object> dictInt => new Dictionary<string, object>(dictInt),
            IReadOnlyDictionary<string, object> dictIntRo => dictIntRo.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            var _ when TryGetDictionaryOfStringKeys(o, out var dictStr) => dictStr,
            IEnumerable<KeyValuePair<string, object>> kvpSeq => kvpSeq.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            _ => FillDictionary(o, new Dictionary<string, object>())
        };
    }

    private static bool TryGetDictionaryOfStringKeys(object o, out IDictionary<string, object> dictionary)
    {
        dictionary = null;

        var typeInfo = o.GetType().GetTypeInfo();

        // get all the interfaces implemented by the type and make sure that one of them is
        // IDictionary<string, T>
        var interfaces = typeInfo.ImplementedInterfaces;
        var canUse = interfaces.Any(i => i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IDictionary<,>) &&
            i.GenericTypeArguments[0] == typeof(string));

        if (canUse)
        {
            dictionary = new DictionaryAccessWrapper((IDictionary)o);
        }

        return canUse;
    }

    private IDictionary<string, object> FillDictionary(object o, IDictionary<string, object> dict)
    {
        foreach (var propInfo in o.GetType().GetRuntimeProperties())
        {
            var mappingBinding = _mappingBindingProvider.GetMappingBinding(propInfo);
            var name = mappingBinding.CypherParameterName ??
                RecordObjectMapping.Instance.GetTranslatedCypherParameterName(propInfo.Name);

            var value = propInfo.GetValue(o);
            var valueTransformed = _parameterValueTransformer.Transform(value);

            dict.Add(name, valueTransformed);
        }

        return dict;
    }
}
