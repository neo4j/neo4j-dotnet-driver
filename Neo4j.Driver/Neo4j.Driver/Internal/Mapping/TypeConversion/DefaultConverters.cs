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

namespace Neo4j.Driver.Internal.Mapping.TypeConversion;

internal interface IDefaultConverters
{
    void Register();
}

internal class DefaultConverters : IDefaultConverters
{
    private readonly IMappingTypeConversionManager _manager;

    public DefaultConverters(IMappingTypeConversionManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public void Register()
    {
        // string to Guid
        _manager.RegisterConverter<string, Guid>(Guid.Parse);

        // various vector conversions
        RegisterVectorConverters();
    }

    private void RegisterVectorConverters()
    {
        foreach (var t in Vector.SupportedTypes)
        {
            RegisterSingleVectorTypeConverters(t);
        }
    }

    private void RegisterSingleVectorTypeConverters(Type elementType)
    {
        var vectorType = typeof(Vector<>).MakeGenericType(elementType);
        var arrayType = elementType.MakeArrayType();

        var toArrayMethod =
            typeof(DefaultConverters).GetMethod(nameof(VectorToArray), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(elementType);

        // Create delegate from MethodInfo
        var arrayConverterType = typeof(Func<,>).MakeGenericType(vectorType, arrayType);
        var arrayConverter = Delegate.CreateDelegate(arrayConverterType, toArrayMethod);

        GetRegisterMethod(vectorType, arrayType).Invoke(_manager, [arrayConverter]);

        var toListMethod =
            typeof(DefaultConverters).GetMethod(nameof(VectorToList), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(elementType);

        // Create delegate from MethodInfo
        var listType = typeof(List<>).MakeGenericType(elementType);
        var listConverterType = typeof(Func<,>).MakeGenericType(vectorType, listType);
        var listConverter = Delegate.CreateDelegate(listConverterType, toListMethod);

        foreach (var targetType in GetAllVectorConversionTargets(elementType))
        {
            GetRegisterMethod(vectorType, targetType).Invoke(_manager, [listConverter]);
        }
    }
    private static IEnumerable<Type> GetAllVectorConversionTargets(Type vectorType)
    {
        yield return typeof(List<>).MakeGenericType(vectorType);
        yield return typeof(IList<>).MakeGenericType(vectorType);
        yield return typeof(IEnumerable<>).MakeGenericType(vectorType);
        yield return typeof(IReadOnlyList<>).MakeGenericType(vectorType);
        yield return typeof(IReadOnlyCollection<>).MakeGenericType(vectorType);
        yield return typeof(ICollection<>).MakeGenericType(vectorType);
    }

    private MethodInfo GetRegisterMethod(Type fromType, Type toType)
    {
        var method = typeof(IMappingTypeConversionManager).GetMethod(nameof(
            IMappingTypeConversionManager.RegisterConverter));
        return method!.MakeGenericMethod(fromType, toType);
    }

    private static T[] VectorToArray<T>(Vector<T> vector) where T : struct => vector.Values.ToArray();
    private static List<T> VectorToList<T>(Vector<T> vector) where T : struct => vector.Values.ToList();
}
