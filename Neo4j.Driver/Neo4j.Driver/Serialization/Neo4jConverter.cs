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

namespace Neo4j.Driver
{
    public abstract class Neo4jConverter
    {
        public Type Type { get; protected set; }
        public abstract object Deserialize(IReadOnlyDictionary<string, object> properties, Type type);
        public abstract IReadOnlyDictionary<string, object> Serialize(object properties, Type type);
    }

    public abstract class Neo4jConverter<T> : Neo4jConverter
    {
        
        public sealed override object Deserialize(IReadOnlyDictionary<string, object> properties, Type type)
        {
            return Deserialize(properties);
        }

        public sealed override IReadOnlyDictionary<string, object> Serialize(object properties, Type type)
        {
            return Serialize((T)properties);
        }

        public abstract T Deserialize(IReadOnlyDictionary<string, object> properties);
        public abstract IReadOnlyDictionary<string, object> Serialize(T properties);
    }
}