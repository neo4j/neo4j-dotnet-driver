// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

namespace Neo4j.Driver;

/// <summary>
/// This abstract class represents a time zone that's usable by <see cref="ZonedDateTime"/> type. A time zone can
/// be identified either by its offset (in seconds) from UTC or its IANA/Windows time zone identifiers. You can get
/// corresponding instances of <see cref="Zone"/> by using its <see cref="Of(int)"/> or <see cref="Of(string)"/> members.
/// </summary>
public abstract class Zone
{
    internal abstract int OffsetSecondsAt(DateTime dateTime);

    /// <summary>Creates a <see cref="Zone"/> instance by using its offset from UTC</summary>
    /// <param name="offsetSeconds">The offset (in seconds) from UTC.</param>
    /// <returns>A <see cref="ZoneOffset"/> instance</returns>
    public static Zone Of(int offsetSeconds)
    {
        return new ZoneOffset(offsetSeconds);
    }

    /// <summary>Creates a <see cref="Zone"/> instance by using its time zone identifier.</summary>
    /// <param name="zoneId">The time zone identifier.</param>
    /// <returns>A <see cref="ZoneId"/> instance</returns>
    public static Zone Of(string zoneId)
    {
        return new ZoneId(zoneId);
    }
}
