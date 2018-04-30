// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Represents a local date time value, without a time zone
    /// </summary>
    public sealed class LocalDateTime : TemporalValue, IEquatable<LocalDateTime>, IComparable, IComparable<LocalDateTime>, IHasDateTimeComponents
    {
        /// <summary>
        /// Default comparer for <see cref="LocalDateTime"/> values.
        /// </summary>
        public static readonly IComparer<LocalDateTime> Comparer = new TemporalValueComparer<LocalDateTime>();

        /// <summary>
        /// Initializes a new instance of <see cref="LocalDateTime"/> from given <see cref="System.DateTime"/> value.
        /// The given <see cref="System.DateTime"/> value will be normalized to local time <see cref="DateTimeKind.Local"/>
        /// before being used.
        /// </summary>
        ///
        /// <remarks>If the <see cref="System.DateTime"/> value was created with no <see cref="DateTimeKind"/> specified,
        /// then <see cref="DateTimeKind.Unspecified"/> would be assigned by default.
        /// Possible conversion from UTC to local time might happen when normalizing it to local time.
        /// <seealso cref="System.DateTime.ToLocalTime"/>
        /// </remarks>
        /// <param name="dateTime"></param>
        public LocalDateTime(DateTime dateTime)
            : this(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second,
                TemporalHelpers.ExtractNanosecondFromTicks(dateTime.Ticks))
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocalDateTime"/> from individual date time
        /// component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        public LocalDateTime(int year, int month, int day, int hour, int minute, int second)
            : this(year, month, day, hour, minute, second, 0)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocalDateTime"/> from individual date time
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="nanosecond"></param>
        public LocalDateTime(int year, int month, int day, int hour, int minute, int second, int nanosecond)
        {
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(year, TemporalHelpers.MinYear, TemporalHelpers.MaxYear, nameof(year));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(month, TemporalHelpers.MinMonth, TemporalHelpers.MaxMonth, nameof(month));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(day, TemporalHelpers.MinDay, TemporalHelpers.MaxDayOfMonth(year, month), nameof(day));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(hour, TemporalHelpers.MinHour, TemporalHelpers.MaxHour, nameof(hour));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(minute, TemporalHelpers.MinMinute, TemporalHelpers.MaxMinute, nameof(minute));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(second, TemporalHelpers.MinSecond, TemporalHelpers.MaxSecond, nameof(second));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(nanosecond, TemporalHelpers.MinNanosecond, TemporalHelpers.MaxNanosecond, nameof(nanosecond));

            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            Minute = minute;
            Second = second;
            Nanosecond = nanosecond;
        }

        /// <summary>
        /// Gets the year component of this instance.
        /// </summary>
        public int Year { get; }

        /// <summary>
        /// Gets the month component of this instance.
        /// </summary>
        public int Month { get; }

        /// <summary>
        /// Gets the day of month component of this instance.
        /// </summary>
        public int Day { get; }

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
        /// Converts this date value to a <see cref="DateTime"/> instance.
        /// </summary>
        /// <value>Equivalent <see cref="DateTime"/> value</value>
        /// <exception cref="ValueOverflowException">If the value cannot be represented with DateTime</exception>
        /// <exception cref="ValueTruncationException">If a truncation occurs during conversion</exception>
        public DateTime ToDateTime()
        {
            TemporalHelpers.AssertNoTruncation(this, nameof(System.DateTime));
            TemporalHelpers.AssertNoOverflow(this, nameof(System.DateTime));

            return new DateTime(Year, Month, Day, Hour, Minute, Second).AddTicks(
                TemporalHelpers.ExtractTicksFromNanosecond(Nanosecond));
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="LocalDateTime"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(LocalDateTime other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Year == other.Year && Month == other.Month && Day == other.Day && Hour == other.Hour && Minute == other.Minute && Second == other.Second && Nanosecond == other.Nanosecond;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="LocalDateTime"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is LocalDateTime && Equals((LocalDateTime) obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Year;
                hashCode = (hashCode * 397) ^ Month;
                hashCode = (hashCode * 397) ^ Day;
                hashCode = (hashCode * 397) ^ Hour;
                hashCode = (hashCode * 397) ^ Minute;
                hashCode = (hashCode * 397) ^ Second;
                hashCode = (hashCode * 397) ^ Nanosecond;
                return hashCode;
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="LocalDateTime"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return
                $"{TemporalHelpers.ToIsoDateString(Year, Month, Day)}T{TemporalHelpers.ToIsoTimeString(Hour, Minute, Second, Nanosecond)}";
        }

        /// <summary>
        /// Compares the value of this instance to a specified <see cref="LocalDateTime"/> value and returns an integer 
        /// that indicates whether this instance is earlier than, the same as, or later than the specified 
        /// DateTime value.
        /// </summary>
        /// <param name="other">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(LocalDateTime other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var yearComparison = Year.CompareTo(other.Year);
            if (yearComparison != 0) return yearComparison;
            var monthComparison = Month.CompareTo(other.Month);
            if (monthComparison != 0) return monthComparison;
            var dayComparison = Day.CompareTo(other.Day);
            if (dayComparison != 0) return dayComparison;
            var hourComparison = Hour.CompareTo(other.Hour);
            if (hourComparison != 0) return hourComparison;
            var minuteComparison = Minute.CompareTo(other.Minute);
            if (minuteComparison != 0) return minuteComparison;
            var secondComparison = Second.CompareTo(other.Second);
            if (secondComparison != 0) return secondComparison;
            return Nanosecond.CompareTo(other.Nanosecond);
        }

        /// <summary>
        /// Compares the value of this instance to a specified object which is expected to be a <see cref="LocalDateTime"/>
        /// value, and returns an integer that indicates whether this instance is earlier than, the same as, 
        /// or later than the specified <see cref="LocalDateTime"/> value.
        /// </summary>
        /// <param name="obj">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            if (!(obj is LocalDateTime)) throw new ArgumentException($"Object must be of type {nameof(LocalDateTime)}");
            return CompareTo((LocalDateTime) obj);
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalDateTime"/> is earlier than another specified 
        /// <see cref="LocalDateTime"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator <(LocalDateTime left, LocalDateTime right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalDateTime"/> is later than another specified 
        /// <see cref="LocalDateTime"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator >(LocalDateTime left, LocalDateTime right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalDateTime"/> represents a duration that is the 
        /// same as or later than the other specified <see cref="LocalDateTime"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator <=(LocalDateTime left, LocalDateTime right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalDateTime"/> represents a duration that is the 
        /// same as or earlier than the other specified <see cref="LocalDateTime"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
        public static bool operator >=(LocalDateTime left, LocalDateTime right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <inheritdoc cref="TemporalValue.ConvertToDateTime"/>
        protected override DateTime ConvertToDateTime()
        {
            return ToDateTime();
        }
    }
}
