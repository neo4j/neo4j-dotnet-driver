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

namespace Neo4j.Driver.Internal.Util;

internal class ObjectToParameterDictionaryConverter(IParameterValueTransformer parameterValueTransformer = null)
    : IObjectToDictionaryConverter
{
    private readonly IParameterValueTransformer _parameterValueTransformer = 
        parameterValueTransformer ?? new ParameterValueTransformer();

    public IDictionary<string, object> Convert(object o)
    {
        switch (o)
        {
            case null: return null;
            case Dictionary<string, object> dict: return dict;
            case IDictionary<string, object> dictInt: return new Dictionary<string, object>(dictInt);
            case IReadOnlyDictionary<string, object> dictIntRo: return dictIntRo.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            case var _ when TryGetDictionaryOfStringKeys(o, out var dictStr): return dictStr;
            case IEnumerable<KeyValuePair<string, object>> kvpSeq: return kvpSeq.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            default: return FillDictionary(o, new Dictionary<string, object>());
        }
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
            var name = propInfo.Name;
            var value = propInfo.GetValue(o);
            var valueTransformed = _parameterValueTransformer.Transform(value);

            dict.Add(name, valueTransformed);
        }

        return dict;
    }
}
