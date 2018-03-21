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
    /// Represents a local date time value, without a time zone
    /// </summary>
    public struct CypherDateTime : ICypherValue, IEquatable<CypherDateTime>
    {

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTime"/> from individual date time
        /// component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        public CypherDateTime(int year, int month, int day, int hour, int minute, int second)
            : this(year, month, day, hour, minute, second, 0)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTime"/> from individual date time
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="nanosOfSecond"></param>
        public CypherDateTime(int year, int month, int day, int hour, int minute, int second, int nanosOfSecond)
            : this(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local))
        {
            NanosOfSecond += nanosOfSecond;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTime"/> from given <see cref="DateTime"/> value
        /// </summary>
        /// <param name="dateTime"></param>
        public CypherDateTime(DateTime dateTime)
            : this(dateTime.ToLocalTime().Ticks)
        {

        }

        internal CypherDateTime(long ticks)
            : this(TemporalHelpers.ComputeSecondsSinceEpoch(ticks),
                TemporalHelpers.ComputeNanosOfSecond(ticks))
        {

        }

        internal CypherDateTime(long epochSeconds, int nanosOfSecond)
        {
            EpochSeconds = epochSeconds;
            NanosOfSecond = nanosOfSecond;
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
        /// Gets a <see cref="DateTime"/> copy of this date value.
        /// </summary>
        /// <returns>Equivalent <see cref="DateTime"/> value</returns>
        public DateTime ToDateTime()
        {
            return TemporalHelpers.ComputeDateTime(EpochSeconds, NanosOfSecond);
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="CypherDateTime"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(CypherDateTime other)
        {
            return EpochSeconds == other.EpochSeconds && NanosOfSecond == other.NanosOfSecond;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="CypherDateTime"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CypherDateTime && Equals((CypherDateTime) obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (EpochSeconds.GetHashCode() * 397) ^ NanosOfSecond;
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="CypherDateTime"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return $"DateTime{{epochSeconds: {EpochSeconds}, nanosOfSecond: {NanosOfSecond}}}";
        }
    }
}