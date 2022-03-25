// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.Internal.Serialization
{
    internal static class Neo4jSerialization
    {
        private static readonly Dictionary<Type, Neo4jConverter> _converters = new Dictionary<Type, Neo4jConverter>();

        static Neo4jSerialization()
        {
            //TODO: do we need to handle caching too many converter instances?
        }

        public static T Convert<T>(IReadOnlyDictionary<string, object> data)
        {
            var converter = GetOrGenerateConverter<T>();
            return converter.Deserialize(data);
        }

        public static Neo4jConverter<T> GetOrGenerateConverter<T>()
        {
            var typeInfo = typeof(T);
            if (_converters.TryGetValue(typeInfo, out var foundConverter))
            {
                if (foundConverter is Neo4jConverter<T> typedConverter)
                    return typedConverter;
                
                if (foundConverter is DefaultConverter untypedConverter)
                {
                    var upgraded = new DefaultConverter<T>(untypedConverter);
                    _converters[typeInfo] = upgraded;
                    return upgraded;
                }
                
                throw new Exception("this shouldn't happen");
            }

            var converterAttribute = typeInfo.GetCustomAttributes(typeof(Neo4jConverterAttribute), true);
            if (converterAttribute.Length == 1)
            {
                if (converterAttribute[0] is not Neo4jConverterAttribute attribute)
                    throw new InvalidOperationException("wrong type");

                if (Activator.CreateInstance(attribute.Converter) is not Neo4jConverter<T> converter)
                    throw new InvalidOperationException("wrong type");

                _converters.Add(typeInfo, converter);
                return converter;
            }

            if (converterAttribute.Length != 0)
                throw new Exception("can not happen");
            
            var newConverter = new DefaultConverter<T>();
            _converters.Add(typeInfo, newConverter);
            return newConverter;
        }

        public static Neo4jConverter GetOrGenerateConverter(Type typeInfo)
        {
            if (_converters.TryGetValue(typeInfo, out var foundConverter))
                return foundConverter;

            var converterAttribute = typeInfo.GetCustomAttributes(typeof(Neo4jConverterAttribute), true);
            if (converterAttribute.Length == 1)
            {
                if (converterAttribute[0] is not Neo4jConverterAttribute attribute)
                    throw new InvalidOperationException("wrong type");

                if (Activator.CreateInstance(attribute.Converter) is not Neo4jConverter converter)
                    throw new InvalidOperationException("wrong type");

                _converters.Add(typeInfo, converter);
                return converter;
            }

            if (converterAttribute.Length != 0)
                throw new Exception("can not happen");

            var typeConverter = new DefaultConverter(typeInfo);
            _converters.Add(typeInfo, typeConverter);
            return typeConverter;
        }
    }
}
