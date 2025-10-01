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
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.IO.ValueSerializers;

internal class UnsupportedTypeSerializer: IPackStreamSerializer
{
    private const byte UnsupportedTypeStructType = (byte)'?';
    private const int UnsupportedTypeStructSize = 4;
    
    /// <inheritdoc />
    public byte[] ReadableStructs => [UnsupportedTypeStructType];
    
    // we don't write unknown data
    public IEnumerable<Type> WritableTypes { get; } = [];
    
    public static UnsupportedTypeSerializer Instance { get; } = new();

    /// <inheritdoc />
    public (object, int) DeserializeSpan(BoltProtocolVersion version, SpanPackStreamReader reader, byte signature, int size)
    {
        if (signature != UnsupportedTypeStructType)
        {
            throw new ProtocolException(
                $"Unknown struct signature {signature} passed to {nameof(UnsupportedTypeSerializer)}!");
        }

        PackStream.EnsureStructSize("UnsupportedType", UnsupportedTypeStructSize, size);

        var name = reader.ReadString();
        var minProtocolMajor = reader.ReadInteger();
        var minProtocolMinor = reader.ReadInteger();
        var extra = reader.ReadMap();
        var message = "";
        
        if (extra.TryGetValue("message", out var messageObj) && messageObj is string foundMessage)
        {
            message = foundMessage;
        }

        var result = new UnsupportedType(name, minProtocolMajor, minProtocolMinor, message);
        return (result, reader.Index);
    }

    public object Deserialize(BoltProtocolVersion version, PackStreamReader reader, byte signature, long size)
    {
        if (signature != UnsupportedTypeStructType)
        {
            throw new ProtocolException(
                $"Unknown struct signature {signature} passed to {nameof(UnsupportedTypeSerializer)}!");
        }

        PackStream.EnsureStructSize("UnsupportedType", UnsupportedTypeStructSize, size);

        var name = reader.ReadString();
        var minProtocolMajor = reader.ReadInteger();
        var minProtocolMinor = reader.ReadInteger();
        var extra = reader.ReadMap();
        var message = "";

        if (extra.TryGetValue("message", out var messageObj) && messageObj is string foundMessage)
        {
            message = foundMessage;
        }
        
        var result = new UnsupportedType(name, minProtocolMajor, minProtocolMinor, message);
        return result;
    }
    
    /// <inheritdoc />
    public void Serialize(BoltProtocolVersion version, PackStreamWriter writer, object value)
    {
        throw new NotImplementedException("UnsupportedType cannot be serialized.");
    }
}
