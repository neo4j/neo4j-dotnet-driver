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

internal sealed class SystemDateTimeOffsetSerializer : WriteOnlySerializer
{
    internal static readonly SystemDateTimeOffsetSerializer Instance = new();

    public override IEnumerable<Type> WritableTypes => new[] { typeof(DateTimeOffset) };

    public override void Serialize(PackStreamWriter writer, object value)
    {
        var dateTime = value.CastOrThrow<DateTimeOffset>();

        writer.Write(new ZonedDateTime(dateTime));
    }
}
