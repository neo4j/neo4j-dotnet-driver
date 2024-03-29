﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Helpers;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;

internal sealed class LocalTimeSerializer : IPackStreamSerializer
{
    internal static readonly LocalTimeSerializer Instance = new();

    public const byte StructType = (byte)'t';
    public const int StructSize = 1;

    public byte[] ReadableStructs => new[] { StructType };
#if NET6_0_OR_GREATER
    public IEnumerable<Type> WritableTypes => new[] { typeof(LocalTime), typeof(TimeOnly) };
#else
    public IEnumerable<Type> WritableTypes => new[] { typeof(LocalTime) };
#endif

    public object Deserialize(BoltProtocolVersion _, PackStreamReader reader, byte signature, long size)
    {
        PackStream.EnsureStructSize("LocalTime", StructSize, size);

        var nanosOfDay = reader.ReadLong();

        return TemporalHelpers.NanoOfDayToTime(nanosOfDay);
    }


    public void Serialize(BoltProtocolVersion _, PackStreamWriter writer, object value)
    {
#if NET6_0_OR_GREATER
        if (value is TimeOnly time)
        {
            WriteTimeOnly(writer, time);
            return;
        }
#endif
        WriteLocalTime(writer, value);
    }

    public (object, int) DeserializeSpan(BoltProtocolVersion version, SpanPackStreamReader reader, byte signature, int size)
    {
        PackStream.EnsureStructSize("LocalTime", StructSize, size);
        var nanosOfDay = reader.ReadLong();
        return (TemporalHelpers.NanoOfDayToTime(nanosOfDay), reader.Index);
    }

#if NET6_0_OR_GREATER
    private void WriteTimeOnly(PackStreamWriter writer, TimeOnly time)
    {
        writer.WriteStructHeader(StructSize, StructType);
        writer.WriteLong(time.ToNanoOfDay());
    }
#endif

    private static void WriteLocalTime(PackStreamWriter writer, object value)
    {
        var time = value.CastOrThrow<LocalTime>();
        writer.WriteStructHeader(StructSize, StructType);
        writer.WriteLong(time.ToNanoOfDay());
    }
}
