// Copyright (c) 2002-2018 "Neo4j,"
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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.IO.StructHandlers
{
    internal class ZonedDateTimeHandler : IPackStreamStructHandler
    {
        public const byte StructTypeWithOffset = (byte) 'F';
        public const byte StructTypeWithId = (byte)'f';
        public const int StructSize = 3;

        public IEnumerable<byte> ReadableStructs => new[] {StructTypeWithId, StructTypeWithOffset};

        public IEnumerable<Type> WritableTypes => new[] {typeof(ZonedDateTime)};

        public object Read(IPackStreamReader reader, byte signature, long size)
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
                        $"Unsupported struct signature {signature} passed to {nameof(ZonedDateTimeHandler)}!");
            }
        }

        public void Write(IPackStreamWriter writer, object value)
        {
            var dateTime = value.CastOrThrow<ZonedDateTime>();

            switch (dateTime.Zone)
            {
                case ZoneId zone:
                    writer.WriteStructHeader(StructSize, StructTypeWithId);
                    writer.Write(dateTime.ToEpochSeconds());
                    writer.Write(dateTime.Nanosecond);
                    writer.Write(zone.Id);
                    break;
                case ZoneOffset zone:
                    writer.WriteStructHeader(StructSize, StructTypeWithOffset);
                    writer.Write(dateTime.ToEpochSeconds());
                    writer.Write(dateTime.Nanosecond);
                    writer.Write(zone.OffsetSeconds);
                    break;
                default:
                    throw new ProtocolException(
                        $"{GetType().Name}: Zone('{dateTime.Zone.GetType().Name}') is not supported.");
            }
        }
    }
}