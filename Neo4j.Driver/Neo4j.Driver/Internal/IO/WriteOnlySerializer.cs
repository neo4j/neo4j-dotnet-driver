// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

namespace Neo4j.Driver.Internal.IO;

internal abstract class WriteOnlySerializer : IPackStreamSerializer
{
    public byte[] ReadableStructs => Array.Empty<byte>();

    public object Deserialize(BoltProtocolVersion _, PackStreamReader reader, byte signature, long size)
    {
        throw new ProtocolException(
            $"{GetType().Name}: It is not expected to receive a struct of signature {signature:X2} from the server.");
    }

    public abstract IEnumerable<Type> WritableTypes { get; }

    public virtual void Serialize(BoltProtocolVersion _, PackStreamWriter writer, object value)
    {
        Serialize(writer, value);
    }

    public (object, int) DeserializeSpan(BoltProtocolVersion version, SpanPackStreamReader reader, byte signature, int size)
    {
        throw new NotImplementedException();
    }

    public abstract void Serialize(PackStreamWriter writer, object value);
}
