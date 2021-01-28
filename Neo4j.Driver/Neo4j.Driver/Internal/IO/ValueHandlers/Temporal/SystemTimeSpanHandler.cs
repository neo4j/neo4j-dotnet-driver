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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.IO.ValueHandlers
{
    internal class SystemTimeSpanHandler : WriteOnlyStructHandler
    {
        public override IEnumerable<Type> WritableTypes => new[] {typeof(TimeSpan)};

        public override void Write(IPackStreamWriter writer, object value)
        {
            var time = value.CastOrThrow<TimeSpan>();

            if (time.Ticks < 0 || time.Ticks >= TimeSpan.TicksPerDay)
            {
                throw new ProtocolException(
                    $"TimeSpan instance ({time}) passed to {nameof(SystemDateTimeHandler)} is not a valid time of day!");
            }

            writer.Write(new LocalTime(time));
        }
    }
}