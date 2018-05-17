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
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Represents a time zone specified by its offset from UTC.
    /// </summary>
    public sealed class ZoneOffset: Zone, IEquatable<ZoneOffset>
    {

        internal ZoneOffset(int offsetSeconds)
        {
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(offsetSeconds, TemporalHelpers.MinOffset,
                TemporalHelpers.MaxOffset, nameof(offsetSeconds));

            OffsetSeconds = offsetSeconds;
        }

        /// <returns>The offset (in seconds) from UTC.</returns>
        public int OffsetSeconds { get; }

        /// <returns>The offset from UTC as a <see cref="TimeSpan"/> instance.</returns>
        public TimeSpan Offset => TimeSpan.FromSeconds(OffsetSeconds);

        internal override int OffsetSecondsAt(DateTime dateTime)
        {
            return OffsetSeconds;
        }

        /// <summary>
        /// Converts the value of the current <see cref="ZoneOffset"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return TemporalHelpers.ToIsoTimeZoneOffset(OffsetSeconds);
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="ZoneOffset"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(ZoneOffset other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return OffsetSeconds == other.OffsetSeconds;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="ZoneOffset"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ZoneOffset offset && Equals(offset);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return OffsetSeconds;
        }
    }
}