// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
    /// Represents a time value with a UTC offset
    /// </summary>
    public struct CypherTimeWithOffset : ICypherValue, IEquatable<CypherTimeWithOffset>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CypherTimeWithOffset"/> from individual time components
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="offsetSeconds"></param>
        public CypherTimeWithOffset(int hour, int minute, int second, int offsetSeconds)
            : this(hour, minute, second, 0, offsetSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherTimeWithOffset"/> from individual time components
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="nanoOfSecond"></param>
        /// <param name="offsetSeconds"></param>
        public CypherTimeWithOffset(int hour, int minute, int second, int nanoOfSecond, int offsetSeconds)
            : this(TemporalHelpers.NanosOf(hour, minute, second, nanoOfSecond), offsetSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherTimeWithOffset"/> from given <see cref="TimeSpan"/> value
        /// </summary>
        /// <param name="time"></param>
        /// <param name="offsetSeconds"></param>
        private CypherTimeWithOffset(TimeSpan time, int offsetSeconds)
            : this(time.NanosOf(), offsetSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherTimeWithOffset"/> from given <see cref="TimeSpan"/> value
        /// </summary>
        /// <param name="time"></param>
        /// <param name="offset"></param>
        public CypherTimeWithOffset(TimeSpan time, TimeSpan offset)
            : this(time.NanosOf(), (int)offset.TotalSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherTimeWithOffset"/> from time components of given <see cref="DateTime"/> value
        /// </summary>
        /// <param name="time"></param>
        /// <param name="offset"></param>
        public CypherTimeWithOffset(DateTime time, TimeSpan offset)
            : this(time.TimeOfDay, (int)offset.TotalSeconds)
        {

        }

        internal CypherTimeWithOffset(long nanosecondsOfDay, int offsetSeconds)
        {
            NanosecondsOfDay = nanosecondsOfDay;
            OffsetSeconds = offsetSeconds;
        }

        /// <summary>
        /// Nanoseconds since midnight
        /// </summary>
        public long NanosecondsOfDay { get; }

        /// <summary>
        /// Offset in seconds precision
        /// </summary>
        public int OffsetSeconds { get; }

        /// <summary>
        /// Gets a <see cref="TimeSpan"/> value that represents the time of this instance.
        /// </summary>
        public TimeSpan Time => TemporalHelpers.TimeOf(NanosecondsOfDay, true);

        /// <summary>
        /// Gets a <see cref="TimeSpan"/> value that represents the offset of this instance.
        /// </summary>
        public TimeSpan Offset => TimeSpan.FromSeconds(OffsetSeconds);

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="CypherTimeWithOffset" /> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(CypherTimeWithOffset other)
        {
            return NanosecondsOfDay == other.NanosecondsOfDay && OffsetSeconds == other.OffsetSeconds;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="CypherTimeWithOffset"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CypherTimeWithOffset && Equals((CypherTimeWithOffset) obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (NanosecondsOfDay.GetHashCode() * 397) ^ OffsetSeconds;
            }
        }
        
        /// <summary>
        /// Converts the value of the current <see cref="CypherTimeWithOffset"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return $"TimeWithOffset{{nanosOfDay: {NanosecondsOfDay}, offsetSeconds: {OffsetSeconds}}}";
        }
    }
}