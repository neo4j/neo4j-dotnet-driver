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
using System.Linq;
using System.Reflection;
using Neo4j.Driver.Mapping;

namespace Neo4j.Driver.Internal.Mapping;

internal interface IParameterMapper
{
    Func<IRecord, T> GetParameterMappedCall<T>(MethodBase methodBase, object target = null);
}

internal class ParameterMapper : IParameterMapper
{
    private readonly IMappableValueProvider _mappableValueProvider = new MappableValueProvider();

    public Func<IRecord, T> GetParameterMappedCall<T>(MethodBase method, object target = null)
    {
        // this part only happens once, at the time of building the mapper
        var parameters = method.GetParameters();
        var paramMappings = parameters.Select(
            parameter => new
            {
                parameter,
                mapping = parameter.GetEntityMappingInfo()
            });

        return MapFromRecord;

        // this part happens every time a record is mapped
        T MapFromRecord(IRecord record)
        {
            var args = new List<object>();
            foreach (var p in paramMappings)
            {
                var success = _mappableValueProvider.TryGetMappableValue(
                    record,
                    r => _mappableValueProvider.GetConvertedValue(r, p.mapping, p.parameter.ParameterType, null),
                    p.parameter.ParameterType,
                    out var mappable);

                if (!success)
                {
                    throw new MappingFailedException(
                        $"Cannot map record to type {typeof(T).Name} because the record does not " +
                        $"contain a value for the parameter '{p.parameter.Name}'.");
                }

                args.Add(mappable);
            }

            try
            {
                return method switch
                {
                    ConstructorInfo constructorInfo => (T)constructorInfo.Invoke(args.ToArray()),
                    MethodInfo methodInfo => (T)methodInfo.Invoke(target, args.ToArray()),
                    _ => throw new NotSupportedException($"Unsupported method type: {method.GetType()}")
                };
            }
            catch (TargetInvocationException tie)
            {
                throw new MappingFailedException(
                    $"Cannot map record to type {typeof(T).Name} because the method '{method.Name}' threw an exception.",
                    tie.InnerException);
            }
        }
    }
}
