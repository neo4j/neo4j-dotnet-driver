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
    /// Represents a time value with a UTC offset
    /// </summary>
    public sealed class OffsetTime : TemporalValue, IEquatable<OffsetTime>, IComparable, IComparable<OffsetTime>, IHasTimeComponents
    {
        /// <summary>
        /// Default comparer for <see cref="OffsetTime"/> values.
        /// </summary>
        public static readonly IComparer<OffsetTime> Comparer = new TemporalValueComparer<OffsetTime>();

        /// <summary>
        /// Initializes a new instance of <see cref="OffsetTime"/> from time components of given <see cref="DateTime"/> value
        /// </summary>
        /// <param name="time"></param>
        /// <param name="offset"></param>
        public OffsetTime(DateTime time, TimeSpan offset)
            : this(time.TimeOfDay, (int) offset.TotalSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="OffsetTime"/> from given <see cref="TimeSpan"/> value
        /// </summary>
        /// <param name="time"></param>
        /// <param name="offset"></param>
        public OffsetTime(TimeSpan time, TimeSpan offset)
            : this(time, (int) offset.TotalSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="OffsetTime"/> from given <see cref="TimeSpan"/> value
        /// </summary>
        /// <param name="time"></param>
        /// <param name="offsetSeconds"></param>
        private OffsetTime(TimeSpan time, int offsetSeconds)
            : this(time.Hours, time.Minutes, time.Seconds, TemporalHelpers.ExtractNanosecondFromTicks(time.Ticks),
                offsetSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="OffsetTime"/> from individual time components
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="offsetSeconds"></param>
        public OffsetTime(int hour, int minute, int second, int offsetSeconds)
            : this(hour, minute, second, 0, offsetSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="OffsetTime"/> from individual time components
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="nanosecond"></param>
        /// <param name="offsetSeconds"></param>
        public OffsetTime(int hour, int minute, int second, int nanosecond, int offsetSeconds)
        {
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(hour, TemporalHelpers.MinHour, TemporalHelpers.MaxHour,
                nameof(hour));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(minute, TemporalHelpers.MinMinute,
                TemporalHelpers.MaxMinute, nameof(minute));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(second, TemporalHelpers.MinSecond,
                TemporalHelpers.MaxSecond, nameof(second));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(nanosecond, TemporalHelpers.MinNanosecond,
                TemporalHelpers.MaxNanosecond, nameof(nanosecond));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(offsetSeconds, TemporalHelpers.MinOffset,
                TemporalHelpers.MaxOffset, nameof(offsetSeconds));

            Hour = hour;
            Minute = minute;
            Second = second;
            Nanosecond = nanosecond;
            OffsetSeconds = offsetSeconds;
        }

        internal OffsetTime(IHasTimeComponents time, int offsetSeconds)
            : this(time.Hour, time.Minute, time.Second, time.Nanosecond, offsetSeconds)
        {

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
        /// Offset in seconds precision
        /// </summary>
        public int OffsetSeconds { get; }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="OffsetTime" /> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(OffsetTime other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Hour == other.Hour && Minute == other.Minute && Second == other.Second && Nanosecond == other.Nanosecond && OffsetSeconds == other.OffsetSeconds;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="OffsetTime"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is OffsetTime time && Equals(time);
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
                hashCode = (hashCode * 397) ^ OffsetSeconds;
                return hashCode;
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="OffsetTime"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return TemporalHelpers.ToIsoTimeString(Hour, Minute, Second, Nanosecond) +
                   TemporalHelpers.ToIsoTimeZoneOffset(OffsetSeconds);
        }

        /// <summary>
        /// Compares the value of this instance to a specified <see cref="OffsetTime"/> value and returns an integer 
        /// that indicates whether this instance is earlier than, the same as, or later than the specified 
        /// DateTime value.
        /// </summary>
        /// <param name="other">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(OffsetTime other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;

            var thisNanoOfDay = this.ToNanoOfDay() - (OffsetSeconds * TemporalHelpers.NanosPerSecond);
            var otherNanoOfDay = other.ToNanoOfDay() - (other.OffsetSeconds * TemporalHelpers.NanosPerSecond);

            if (thisNanoOfDay < 0)
            {
                thisNanoOfDay = TemporalHelpers.NanosPerDay + thisNanoOfDay;
            }

            if (otherNanoOfDay < 0)
            {
                otherNanoOfDay = TemporalHelpers.NanosPerDay + otherNanoOfDay;
            }

            return thisNanoOfDay.CompareTo(otherNanoOfDay);
        }

        /// <summary>
        /// Compares the value of this instance to a specified object which is expected to be a <see cref="OffsetTime"/>
        /// value, and returns an integer that indicates whether this instance is earlier than, the same as, 
        /// or later than the specified <see cref="OffsetTime"/> value.
        /// </summary>
        /// <param name="obj">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(object obj)
        {
            if (obj is null) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            if (!(obj is OffsetTime))
                throw new ArgumentException($"Object must be of type {nameof(OffsetTime)}");
            return CompareTo((OffsetTime) obj);
        }

        /// <summary>
        /// Determines whether one specified <see cref="OffsetTime"/> is earlier than another specified 
        /// <see cref="OffsetTime"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator <(OffsetTime left, OffsetTime right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="OffsetTime"/> is later than another specified 
        /// <see cref="OffsetTime"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator >(OffsetTime left, OffsetTime right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="OffsetTime"/> represents a duration that is the 
        /// same as or later than the other specified <see cref="OffsetTime"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator <=(OffsetTime left, OffsetTime right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="OffsetTime"/> represents a duration that is the 
        /// same as or earlier than the other specified <see cref="OffsetTime"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator >=(OffsetTime left, OffsetTime right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}