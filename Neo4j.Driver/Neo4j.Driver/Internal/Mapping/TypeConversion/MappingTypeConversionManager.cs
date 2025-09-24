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
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Neo4j.Driver.Internal.Mapping.TypeConversion;

internal class MappingTypeConversionManager : IMappingTypeConversionManager
{
    private readonly ConcurrentDictionary<(Type From, Type To), Func<object, object>> _converters = new();

    public void Clear() => _converters.Clear();

    /// <inheritdoc />
    public void RegisterDefaultConverters()
    {
    }

    public bool TryConvert(Type fromType, Type toType, object from, out object to)
    {
        if (_converters.TryGetValue((fromType, toType), out var converter))
        {
            to = converter(from);
            return true;
        }

        to = default!;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert<TFrom, TTo>(TFrom from, out TTo to)
    {
        var success = TryConvert(typeof(TFrom), typeof(TTo), from, out var result);
        if (success)
        {
            to = (TTo)result;
            return true;
        }

        to = default!;
        return false;
    }

    /// <inheritdoc />
    public void RegisterConverter<TFrom, TTo>(Func<TFrom, TTo> converter)
    {
        Trace.WriteLine("Registering converter in " + GetHashCode());
        Func<object, object> func = o => converter((TFrom)o);
        _converters.AddOrUpdate((typeof(TFrom), typeof(TTo)), func, (_, _) => func);
    }
}
