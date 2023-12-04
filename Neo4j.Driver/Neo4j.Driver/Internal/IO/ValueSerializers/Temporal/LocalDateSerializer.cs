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

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;

internal sealed class LocalDateSerializer : IPackStreamSerializer
{
    internal static readonly LocalDateSerializer Instance = new();

    public const byte StructType = (byte)'D';
    public const int StructSize = 1;

    public byte[] ReadableStructs => new[] { StructType };

#if NET6_0_OR_GREATER
    public IEnumerable<Type> WritableTypes => new[] { typeof(LocalDate), typeof(DateOnly) };
#else
    public IEnumerable<Type> WritableTypes => new[] { typeof(LocalDate) };
#endif

    public object Deserialize(BoltProtocolVersion _, PackStreamReader reader, byte signature, long size)
    {
        PackStream.EnsureStructSize("Date", StructSize, size);

        var epochDays = reader.ReadLong();

        return TemporalHelpers.EpochDaysToDate(epochDays);
    }

    public void Serialize(BoltProtocolVersion _, PackStreamWriter writer, object value)
    {
#if NET6_0_OR_GREATER
        if (value is DateOnly date)
        {
            WriteDateOnly(writer, date);
            return;
        }
#endif
        WriteLocalDate(writer, value);
    }

    public (object, int) DeserializeSpan(BoltProtocolVersion version, SpanPackStreamReader reader, byte signature, int size)
    {
        PackStream.EnsureStructSize("Date", StructSize, size);

        var epochDays = reader.ReadLong();

        return (TemporalHelpers.EpochDaysToDate(epochDays), reader.Index);
    }

    private static void WriteLocalDate(PackStreamWriter writer, object value)
    {
        var date = value.CastOrThrow<LocalDate>();
        writer.WriteStructHeader(StructSize, StructType);
        writer.WriteLong(date.ToEpochDays());
    }

#if NET6_0_OR_GREATER
    private static void WriteDateOnly(PackStreamWriter writer, DateOnly date)
    {
        writer.WriteStructHeader(StructSize, StructType);
        writer.WriteLong(date.ToEpochDays());
    }
#endif
}
