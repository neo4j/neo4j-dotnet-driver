// Copyright (c) 2002-2019 "Neo4j,"
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver
{
    /// <summary>
    /// Represents temporal amount containing months, days, seconds and nanoseconds. 
    /// <remarks>A duration can hold a negative value.</remarks>
    /// </summary>
    public sealed class Duration : TemporalValue, IEquatable<Duration>, IComparable, IComparable<Duration>
    {
        /// <summary>
        /// Default comparer for <see cref="Duration"/> values.
        /// </summary>
        public static readonly IComparer<Duration> Comparer = new TemporalValueComparer<Duration>();

        /// <summary>
        /// Initializes a new instance of <see cref="Duration" /> in terms of <see cref="Seconds"/>
        /// </summary>
        /// <param name="seconds"><see cref="Seconds"/></param>
        public Duration(long seconds)
            : this(seconds, 0)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="Duration" /> in terms of <see cref="Seconds"/> 
        /// and <see cref="Nanos"/>
        /// </summary>
        /// <param name="seconds"><see cref="Seconds"/></param>
        /// <param name="nanos"><see cref="Nanos"/></param>
        public Duration(long seconds, int nanos)
            : this(0, seconds, nanos)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="Duration" /> in terms of <see cref="Days"/>, 
        /// <see cref="Seconds"/> and <see cref="Nanos"/>
        /// </summary>
        /// <param name="days"><see cref="Days"/></param>
        /// <param name="seconds"><see cref="Seconds"/></param>
        /// <param name="nanos"><see cref="Nanos"/></param>
        public Duration(long days, long seconds, int nanos)
            : this(0, days, seconds, nanos)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="Duration" /> with all supported temporal fields
        /// </summary>
        /// <param name="months"><see cref="Months"/></param>
        /// <param name="days"><see cref="Days"/></param>
        /// <param name="seconds"><see cref="Seconds"/></param>
        /// <param name="nanos"><see cref="Nanos"/></param>
        public Duration(long months, long days, long seconds, int nanos)
        {
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(nanos, TemporalHelpers.MinNanosecond,
                TemporalHelpers.MaxNanosecond, nameof(nanos));

            Months = months;
            Days = days;
            Seconds = seconds;
            Nanos = nanos;
        }

        /// <summary>
        /// The number of months in this duration.
        /// </summary>
        public long Months { get; }

        /// <summary>
        /// The number of days in this duration.
        /// </summary>
        public long Days { get; }

        /// <summary>
        /// The number of seconds in this duration.
        /// </summary>
        public long Seconds { get; }

        /// <summary>
        /// The number of nanoseconds in this duration.
        /// </summary>
        public int Nanos { get; }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="Duration"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(Duration other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Months == other.Months && Days == other.Days && Seconds == other.Seconds && Nanos == other.Nanos;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="Duration"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Duration duration && Equals(duration);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Months.GetHashCode();
                hashCode = (hashCode * 397) ^ Days.GetHashCode();
                hashCode = (hashCode * 397) ^ Seconds.GetHashCode();
                hashCode = (hashCode * 397) ^ Nanos;
                return hashCode;
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="Duration"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return TemporalHelpers.ToIsoDurationString(Months, Days, Seconds, Nanos);
        }

        /// <summary>
        /// Compares the value of this instance to a specified <see cref="Duration"/> value and returns an integer 
        /// that indicates whether this instance is earlier than, the same as, or later than the specified 
        /// DateTime value.
        /// </summary>
        /// <param name="other">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(Duration other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var thisDays = this.GetDays();
            var otherDays = other.GetDays();
            var comparison = thisDays.CompareTo(otherDays);
            if (comparison == 0)
            {
                var thisNanos = this.GetNanos();
                var otherNanos = other.GetNanos();

                comparison = thisNanos.CompareTo(otherNanos);
            }
            return comparison;
        }

        /// <summary>
        /// Compares the value of this instance to a specified object which is expected to be a <see cref="Duration"/>
        /// value, and returns an integer that indicates whether this instance is earlier than, the same as, 
        /// or later than the specified <see cref="Duration"/> value.
        /// </summary>
        /// <param name="obj">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(object obj)
        {
            if (obj is null) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            if (!(obj is Duration)) throw new ArgumentException($"Object must be of type {nameof(Duration)}");
            return CompareTo((Duration) obj);
        }

        /// <summary>
        /// Determines whether one specified <see cref="Duration"/> is less than another specified 
        /// <see cref="Duration"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator <(Duration left, Duration right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="Duration"/> is more than another specified 
        /// <see cref="Duration"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator >(Duration left, Duration right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="Duration"/> represents a duration that is the 
        /// same as or more than the other specified <see cref="Duration"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator <=(Duration left, Duration right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="Duration"/> represents a duration that is the 
        /// same as or less than the other specified <see cref="Duration"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator >=(Duration left, Duration right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}