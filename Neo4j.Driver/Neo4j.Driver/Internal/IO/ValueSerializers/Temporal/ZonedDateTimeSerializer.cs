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

internal sealed class ZonedDateTimeSerializer : IPackStreamSerializer
{
    public const byte StructTypeWithOffset = (byte)'F';
    public const byte StructTypeWithId = (byte)'f';
    public const int StructSize = 3;
    internal static readonly ZonedDateTimeSerializer Instance = new();

    public byte[] ReadableStructs => new[] { StructTypeWithId, StructTypeWithOffset };

    public IEnumerable<Type> WritableTypes => new[] { typeof(ZonedDateTime) };

    public object Deserialize(BoltProtocolVersion _, PackStreamReader reader, byte signature, long size)
    {
        PackStream.EnsureStructSize($"ZonedDateTime[{(char)signature}]", StructSize, size);

        var epochSecondsUtc = reader.ReadLong();
        var nanosOfSecond = reader.ReadInteger();

        switch (signature)
        {
            case StructTypeWithId:
                return new ZonedDateTime(
                    TemporalHelpers.EpochSecondsAndNanoToDateTime(epochSecondsUtc, nanosOfSecond),
                    Zone.Of(reader.ReadString()));

            case StructTypeWithOffset:
                return new ZonedDateTime(
                    TemporalHelpers.EpochSecondsAndNanoToDateTime(epochSecondsUtc, nanosOfSecond),
                    Zone.Of(reader.ReadInteger()));

            default:
                throw new ProtocolException(
                    $"Unsupported struct signature {signature} passed to {nameof(ZonedDateTimeSerializer)}!");
        }
    }

    public void Serialize(BoltProtocolVersion _, PackStreamWriter writer, object value)
    {
        var dateTime = value.CastOrThrow<ZonedDateTime>();

        switch (dateTime.Zone)
        {
            case ZoneId zone:
                writer.WriteStructHeader(StructSize, StructTypeWithId);
                writer.WriteLong(dateTime.ToEpochSeconds());
                writer.WriteInt(dateTime.Nanosecond);
                writer.WriteString(zone.Id);
                break;

            case ZoneOffset zone:
                writer.WriteStructHeader(StructSize, StructTypeWithOffset);
                writer.WriteLong(dateTime.ToEpochSeconds());
                writer.WriteInt(dateTime.Nanosecond);
                writer.WriteInt(zone.OffsetSeconds);
                break;

            default:
                throw new ProtocolException(
                    $"{GetType().Name}: Zone('{dateTime.Zone.GetType().Name}') is not supported.");
        }
    }

    public (object, int) DeserializeSpan(
        BoltProtocolVersion version,
        SpanPackStreamReader reader,
        byte signature,
        int size)
    {
        PackStream.EnsureStructSize($"ZonedDateTime[{(char)signature}]", StructSize, size);

        var epochSecondsUtc = reader.ReadLong();
        var nanosOfSecond = reader.ReadInteger();

        var zone = signature switch
        {
            StructTypeWithId => Zone.Of(reader.ReadString()),
            StructTypeWithOffset => Zone.Of(reader.ReadInteger()),
            _ =>
                throw new ProtocolException(
                    $"Unsupported struct signature {signature} passed to {nameof(ZonedDateTimeSerializer)}!")
        };

        return (new ZonedDateTime(TemporalHelpers.EpochSecondsAndNanoToDateTime(epochSecondsUtc, nanosOfSecond), zone),
            reader.Index);
    }
}
