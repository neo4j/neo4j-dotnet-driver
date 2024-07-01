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
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.IO;

internal abstract class ReadOnlySerializer : IPackStreamSerializer
{
    public IEnumerable<Type> WritableTypes => Enumerable.Empty<Type>();
    public abstract byte[] ReadableStructs { get; }

    public void Serialize(BoltProtocolVersion _, PackStreamWriter writer, object value)
    {
        throw new ProtocolException(
            $"{GetType().Name}: It is not allowed to send a value of type {value?.GetType().Name} to the server.");
    }

    public virtual object Deserialize(BoltProtocolVersion _, PackStreamReader reader, byte __, long ___)
    {
        return Deserialize(reader);
    }

    public virtual object Deserialize(PackStreamReader reader)
    {
        throw new NotImplementedException();
    }

    public virtual (object, int) DeserializeSpan(
        BoltProtocolVersion version,
        SpanPackStreamReader reader,
        byte signature,
        int size)
    {
        return DeserializeSpan(reader);
    }

    public virtual (object, int) DeserializeSpan(SpanPackStreamReader reader)
    {
        throw new NotImplementedException();
    }
}
