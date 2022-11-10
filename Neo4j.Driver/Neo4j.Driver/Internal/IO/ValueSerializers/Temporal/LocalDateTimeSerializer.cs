// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;

internal class LocalDateTimeSerializer : IPackStreamSerializer
{
    public const byte StructType = (byte) 'd';
    public const int StructSize = 2;

    public IEnumerable<byte> ReadableStructs => new[] {StructType};

    public IEnumerable<Type> WritableTypes => new[] {typeof(LocalDateTime)};

    public object Deserialize(BoltProtocolVersion _, PackStreamReader reader, byte signature, long size)
    {
        PackStream.EnsureStructSize("LocalDateTime", StructSize, size);

        var epochSeconds = reader.ReadLong();
        var nanosOfSecond = reader.ReadInteger();

        return TemporalHelpers.EpochSecondsAndNanoToDateTime(epochSeconds, nanosOfSecond);
    }

    public void Serialize(BoltProtocolVersion _, PackStreamWriter writer, object value)
    {
        var dateTime = value.CastOrThrow<LocalDateTime>();

        writer.WriteStructHeader(StructSize, StructType);
        writer.WriteLong(dateTime.ToEpochSeconds());
        writer.WriteInt(dateTime.Nanosecond);
    }
}