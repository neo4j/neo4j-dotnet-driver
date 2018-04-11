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
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Represents a local time value
    /// </summary>
    public struct CypherTime : ICypherValue, IEquatable<CypherTime>, IHasTimeComponents
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CypherTime"/> from time components of given <see cref="DateTime"/>
        /// </summary>
        /// <param name="time"></param>
        public CypherTime(DateTime time)
            : this(time.TimeOfDay)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherTime"/> from given <see cref="TimeSpan"/> value
        /// </summary>
        /// <param name="time"></param>
        public CypherTime(TimeSpan time)
            : this(time.Hours, time.Minutes, time.Seconds, TemporalHelpers.ExtractNanosecondFromTicks(time.Ticks))
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherTime"/> from individual time components
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        public CypherTime(int hour, int minute, int second)
            : this(hour, minute, second, 0)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherTime"/> from individual time components
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="nanosecond"></param>
        public CypherTime(int hour, int minute, int second, int nanosecond)
        {
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(hour, TemporalHelpers.MinHour, TemporalHelpers.MaxHour, nameof(hour));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(minute, TemporalHelpers.MinMinute, TemporalHelpers.MaxMinute, nameof(minute));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(second, TemporalHelpers.MinSecond, TemporalHelpers.MaxSecond, nameof(second));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(nanosecond, TemporalHelpers.MinNanosecond, TemporalHelpers.MaxNanosecond, nameof(nanosecond));

            Hour = hour;
            Minute = minute;
            Second = second;
            Nanosecond = nanosecond;
        }

        /// <summary>
        /// Gets the hour component of this instance.
        /// </summary>
        public int Hour { get; }

        /// <summary>
        /// Gets the minute component of this instance.
        /// </summary>
        public int Minute { get; }

        /// <summary>
        /// Gets the second component of this instance.
        /// </summary>
        public int Second { get; }

        /// <summary>
        /// Gets the nanosecond component of this instance.
        /// </summary>
        public int Nanosecond { get; }

        /// <summary>
        /// Gets a <see cref="TimeSpan"/> copy of this time value.
        /// </summary>
        /// <value>Equivalent <see cref="TimeSpan"/> value</value>
        /// <exception cref="ValueTruncationException">If a truncation occurs during conversion</exception>
        public TimeSpan Time
        {
            get
            {
                TemporalHelpers.AssertNoTruncation(this, nameof(TimeSpan));

                return new TimeSpan(0, Hour, Minute, Second).Add(
                    TimeSpan.FromTicks(TemporalHelpers.ExtractTicksFromNanosecond(Nanosecond)));
            }
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="CypherTime" /> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(CypherTime other)
        {
            return Hour == other.Hour && Minute == other.Minute && Second == other.Second && Nanosecond == other.Nanosecond;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="CypherTime"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CypherTime && Equals((CypherTime) obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Hour;
                hashCode = (hashCode * 397) ^ Minute;
                hashCode = (hashCode * 397) ^ Second;
                hashCode = (hashCode * 397) ^ Nanosecond;
                return hashCode;
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="CypherTime"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return TemporalHelpers.ToIsoTimeString(Hour, Minute, Second, Nanosecond);
        }

    }
}