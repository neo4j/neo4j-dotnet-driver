// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;

internal sealed class SystemDateTimeSerializer : WriteOnlySerializer
{
    internal static readonly SystemDateTimeSerializer Instance = new();

    public override IEnumerable<Type> WritableTypes => new[] { typeof(DateTime) };

    public override void Serialize(PackStreamWriter writer, object value)
    {
        var dateTime = value.CastOrThrow<DateTime>();

        switch (dateTime.Kind)
        {
            case DateTimeKind.Local:
            case DateTimeKind.Unspecified:
                writer.Write(new LocalDateTime(dateTime));
                break;

            case DateTimeKind.Utc:
                writer.Write(new ZonedDateTime(dateTime, 0));
                break;

            default:
                throw new ProtocolException(
                    $"Unsupported DateTimeKind {dateTime.Kind} passed to {nameof(SystemDateTimeSerializer)}!");
        }
    }
}
