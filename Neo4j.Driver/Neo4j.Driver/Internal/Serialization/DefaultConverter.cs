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
using System.Reflection;
using System.Threading;

namespace Neo4j.Driver.Internal.Serialization
{
    internal class DefaultConverter : Neo4jConverter
    {
        public DefaultConverter(Type type)
        {
            Type = type;
            lazyDeserializers =
                new Lazy<List<Action<IReadOnlyDictionary<string, object>, object>>>(GenerateDeserializers,
                    LazyThreadSafetyMode.PublicationOnly);
        }

        private readonly Lazy<List<Action<IReadOnlyDictionary<string, object>, object>>> lazyDeserializers;

        private List<Action<IReadOnlyDictionary<string, object>, object>> GenerateDeserializers()
        {
            var factory = new PropsAndFieldConverterFactory();
            var fields = Type.GetFields(BindingFlags.Public);
            var props = Type.GetProperties(BindingFlags.SetProperty | BindingFlags.Public);

            var setters = new List<Action<IReadOnlyDictionary<string, object>, object>>();

            foreach (var fieldInfo in fields)
            {
                var action = factory.GenerateForField(fieldInfo);
                if (action == null)
                    continue;
                setters.Add(action);
            }

            foreach (var fieldInfo in props)
            {
                var action = factory.GenerateForProperties(fieldInfo);
                if (action == null)
                    continue;
                setters.Add(action);
            }

            return setters;
        }

        public override object Deserialize(IReadOnlyDictionary<string, object> properties, Type type)
        {
            var instance = Activator.CreateInstance(type);
            foreach (var action in lazyDeserializers.Value)
                action(properties, instance);

            return instance;
        }

        public override IReadOnlyDictionary<string, object> Serialize(object properties, Type type)
        {
            throw new NotImplementedException();
        }
    }

    internal class DefaultConverter<T> : Neo4jConverter<T>
    {
        private readonly DefaultConverter _internalConverter;

        public DefaultConverter()
        {
            Type = typeof(T);
            _internalConverter = new DefaultConverter(Type);
        }

        public DefaultConverter(DefaultConverter converter)
        {
            Type = typeof(T);
            _internalConverter = converter;
        }

        public override T Deserialize(IReadOnlyDictionary<string, object> properties)
        {
            return (T)_internalConverter.Deserialize(properties, Type);
        }

        public override IReadOnlyDictionary<string, object> Serialize(T properties)
        {
            return _internalConverter.Serialize(properties, Type);
        }
    }

}