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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver
{
    /// <summary>
    /// Represents a local time value
    /// </summary>
    public sealed class LocalTime : TemporalValue, IEquatable<LocalTime>, IComparable, IComparable<LocalTime>, IHasTimeComponents
    {
        /// <summary>
        /// Default comparer for <see cref="LocalTime"/> values.
        /// </summary>
        public static readonly IComparer<LocalTime> Comparer = new TemporalValueComparer<LocalTime>();

        /// <summary>
        /// Initializes a new instance of <see cref="LocalTime"/> from time components of given <see cref="DateTime"/>
        /// </summary>
        /// <param name="time"></param>
        public LocalTime(DateTime time)
            : this(time.TimeOfDay)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocalTime"/> from given <see cref="TimeSpan"/> value
        /// </summary>
        /// <param name="time"></param>
        public LocalTime(TimeSpan time)
            : this(time.Hours, time.Minutes, time.Seconds, TemporalHelpers.ExtractNanosecondFromTicks(time.Ticks))
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocalTime"/> from individual time components
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        public LocalTime(int hour, int minute, int second)
            : this(hour, minute, second, 0)
        {

        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance of <see cref="LocalTime"/> from given <see cref="TimeOnly"/> value
        /// </summary>
        /// <param name="time"></param>
        public LocalTime(TimeOnly time)
            : this(TimeSpan.FromTicks(time.Ticks))
        {
        }
#endif

        /// <summary>
        /// Initializes a new instance of <see cref="LocalTime"/> from individual time components
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="nanosecond"></param>
        public LocalTime(int hour, int minute, int second, int nanosecond)
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
        /// Converts this time value to a <see cref="TimeSpan"/> instance.
        /// </summary>
        /// <value>Equivalent <see cref="TimeSpan"/> value</value>
        /// <exception cref="ValueTruncationException">If a truncation occurs during conversion</exception>
        /// <returns>A <see cref="TimeSpan"/> instance.</returns>
        public TimeSpan ToTimeSpan()
        {
            TemporalHelpers.AssertNoTruncation(this, nameof(TimeSpan));

            return new TimeSpan(0, Hour, Minute, Second).Add(
                TimeSpan.FromTicks(TemporalHelpers.ExtractTicksFromNanosecond(Nanosecond)));
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Converts this time value to a <see cref="TimeOnly"/> instance.
        /// </summary>
        /// <returns>A <see cref="TimeOnly"/> instance.</returns>
        public TimeOnly ToTimeOnly()
        {
            TemporalHelpers.AssertNoTruncation(this, nameof(TimeOnly));
            return new TimeOnly(Hour, Minute, Second, TemporalHelpers.NanosecondToMillisecond(Nanosecond));
        }
#endif

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="LocalTime" /> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(LocalTime other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Hour == other.Hour && Minute == other.Minute && Second == other.Second && Nanosecond == other.Nanosecond;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="LocalTime"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is LocalTime time && Equals(time);
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
        /// Converts the value of the current <see cref="LocalTime"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return TemporalHelpers.ToIsoTimeString(Hour, Minute, Second, Nanosecond);
        }

        /// <summary>
        /// Compares the value of this instance to a specified <see cref="LocalTime"/> value and returns an integer 
        /// that indicates whether this instance is earlier than, the same as, or later than the specified 
        /// DateTime value.
        /// </summary>
        /// <param name="other">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(LocalTime other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var hourComparison = Hour.CompareTo(other.Hour);
            if (hourComparison != 0) return hourComparison;
            var minuteComparison = Minute.CompareTo(other.Minute);
            if (minuteComparison != 0) return minuteComparison;
            var secondComparison = Second.CompareTo(other.Second);
            if (secondComparison != 0) return secondComparison;
            return Nanosecond.CompareTo(other.Nanosecond);
        }

        /// <summary>
        /// Compares the value of this instance to a specified object which is expected to be a <see cref="LocalTime"/>
        /// value, and returns an integer that indicates whether this instance is earlier than, the same as, 
        /// or later than the specified <see cref="LocalTime"/> value.
        /// </summary>
        /// <param name="obj">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(object obj)
        {
            if (obj is null) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            if (!(obj is LocalTime)) throw new ArgumentException($"Object must be of type {nameof(LocalTime)}");
            return CompareTo((LocalTime) obj);
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalTime"/> is earlier than another specified 
        /// <see cref="LocalTime"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code>if one is earlier than another, otherwise <code>false</code>.</returns>
        public static bool operator <(LocalTime left, LocalTime right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalTime"/> is later than another specified 
        /// <see cref="LocalTime"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is later than another, otherwise <code>false</code>.</returns>
        public static bool operator >(LocalTime left, LocalTime right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalTime"/> represents a duration that is the 
        /// same as or later than the other specified <see cref="LocalTime"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is the same as or later than another, otherwise <code>false</code>.</returns>
        public static bool operator <=(LocalTime left, LocalTime right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalTime"/> represents a duration that is the 
        /// same as or earlier than the other specified <see cref="LocalTime"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is the same as or later than another, otherwise <code>false</code>.</returns>
        public static bool operator >=(LocalTime left, LocalTime right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <inheritdoc cref="TemporalValue.ConvertToDateTime"/>
        protected override DateTime ConvertToDateTime()
        {
            return DateTime.Today.Add(ToTimeSpan());
        }

        /// <inheritdoc cref="TemporalValue.ConvertToTimeSpan"/>
        protected override TimeSpan ConvertToTimeSpan()
        {
            return ToTimeSpan();
        }
    }
}