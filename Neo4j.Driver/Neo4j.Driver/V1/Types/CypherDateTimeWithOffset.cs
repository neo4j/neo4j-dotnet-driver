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
    /// Represents a date time value with a time zone, specified as a UTC offset
    /// </summary>
    public struct CypherDateTimeWithOffset : IEquatable<CypherDateTimeWithOffset>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithOffset"/> from individual date time component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="offsetSeconds"></param>
        public CypherDateTimeWithOffset(int year, int month, int day, int hour, int minute, int second, int offsetSeconds)
            : this(year, month, day, hour, minute, second, 0, offsetSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithOffset"/> from individual date time component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="nanosOfSecond"></param>
        /// <param name="offsetSeconds"></param>
        public CypherDateTimeWithOffset(int year, int month, int day, int hour, int minute, int second, int nanosOfSecond, int offsetSeconds)
            : this(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified), offsetSeconds)
        {
            NanosOfSecond += nanosOfSecond;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithOffset"/> from given <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="offsetSeconds"></param>
        public CypherDateTimeWithOffset(DateTime dateTime, int offsetSeconds)
            : this(dateTime.Kind == DateTimeKind.Unspecified ? dateTime : new DateTime(dateTime.Ticks, DateTimeKind.Unspecified), TimeSpan.FromSeconds(offsetSeconds))
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithOffset"/> from given <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="offset"></param>
        public CypherDateTimeWithOffset(DateTime dateTime, TimeSpan offset)
            : this((dateTime.Kind == DateTimeKind.Unspecified ? dateTime : new DateTime(dateTime.Ticks, DateTimeKind.Unspecified)).Ticks - offset.Ticks, (int)offset.TotalSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithOffset"/> from ticks.
        /// </summary>
        /// <param name="ticks"></param>
        /// <param name="offsetSeconds"></param>
        public CypherDateTimeWithOffset(long ticks, int offsetSeconds)
            : this(TemporalHelpers.ComputeSecondsSinceEpoch(ticks),
                TemporalHelpers.ComputeNanosOfSecond(ticks), offsetSeconds)
        {

        }

        internal CypherDateTimeWithOffset(long epochSeconds, int nanosOfSecond, int offsetSeconds)
        {
            EpochSeconds = epochSeconds;
            NanosOfSecond = nanosOfSecond;
            OffsetSeconds = offsetSeconds;
        }

        /// <summary>
        /// Seconds since Unix Epoch
        /// </summary>
        public long EpochSeconds { get; }

        /// <summary>
        /// Fraction of seconds in nanosecond precision
        /// </summary>
        public int NanosOfSecond { get; }

        /// <summary>
        /// Offset in seconds precision
        /// </summary>
        public int OffsetSeconds { get; }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="CypherDateTimeWithOffset"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(CypherDateTimeWithOffset other)
        {
            return EpochSeconds == other.EpochSeconds && NanosOfSecond == other.NanosOfSecond && OffsetSeconds == other.OffsetSeconds;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="CypherDateTimeWithOffset"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CypherDateTimeWithOffset && Equals((CypherDateTimeWithOffset) obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EpochSeconds.GetHashCode();
                hashCode = (hashCode * 397) ^ NanosOfSecond;
                hashCode = (hashCode * 397) ^ OffsetSeconds;
                return hashCode;
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="CypherDateTimeWithOffset"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return $"DateTimeWithOffset{{epochSeconds: {EpochSeconds}, nanosOfSecond: {NanosOfSecond}, offsetSeconds: {OffsetSeconds}}}";
        }
    }
}