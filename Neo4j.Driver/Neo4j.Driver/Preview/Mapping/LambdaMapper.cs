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
using System.Linq.Expressions;
using System.Reflection;
using Neo4j.Driver.Mapping;

namespace Neo4j.Driver.Preview.Mapping;

internal class LambdaMapper
{
    internal static T Map<T>(IRecord record, LambdaExpression mapFunction)
    {
        var paramValues = new List<object>();
        foreach (var param in mapFunction.Parameters)
        {
            if (record.TryGet(param.Name, out object value))
            {
                object valueToUse;
                try
                {
                    valueToUse = value.AsType(param.Type);
                }
                catch (InvalidCastException) when (value is IEntity or IReadOnlyDictionary<string, object>)
                {
                    var objToMap = new DictAsRecord(value, null);
                    valueToUse = RecordObjectMapping.Map(objToMap, param.Type);
                }

                paramValues.Add(valueToUse);
            }
            else
            {
                throw new MappingFailedException($"Failed to map parameter {param.Name}: No such key in record.");
            }
        }

        try
        {
            return (T)
                mapFunction
                    .Compile()
                    .DynamicInvoke(paramValues.ToArray());
        }
        catch (Exception ex)
        {
            var inner = ex is TargetInvocationException tie ? tie.InnerException : ex;
            throw new MappingFailedException("Failed to map record to blueprint.", inner);
        }
    }
}
