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

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;

internal sealed class SystemTimeSpanSerializer : WriteOnlySerializer
{
    internal static readonly SystemTimeSpanSerializer Instance = new();

    public override IEnumerable<Type> WritableTypes => new[] { typeof(TimeSpan) };

    public override void Serialize(PackStreamWriter writer, object value)
    {
        var time = value.CastOrThrow<TimeSpan>();

        if (time.Ticks < 0 || time.Ticks >= TimeSpan.TicksPerDay)
        {
            throw new ProtocolException(
                $"TimeSpan instance ({time}) passed to {nameof(SystemDateTimeSerializer)} is not a valid time of day!");
        }

        writer.Write(new LocalTime(time));
    }
}
