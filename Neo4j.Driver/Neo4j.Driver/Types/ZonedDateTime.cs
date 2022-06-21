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
    /// Represents a date time value with a time zone, specified as a UTC offset
    /// </summary>
    public sealed class ZonedDateTime : TemporalValue, IEquatable<ZonedDateTime>, IComparable, IComparable<ZonedDateTime>, IHasDateTimeComponents
    {
        /// <summary>
        /// Default comparer for <see cref="ZonedDateTime"/> values.
        /// </summary>
        public static readonly IComparer<ZonedDateTime> Comparer = new TemporalValueComparer<ZonedDateTime>();

        /// <summary>
        /// Initializes a new instance of <see cref="ZonedDateTime"/> from given <see cref="DateTimeOffset"/> value.
        /// </summary>
        /// <param name="dateTimeOffset"></param>
        public ZonedDateTime(DateTimeOffset dateTimeOffset)
            : this(dateTimeOffset.DateTime, dateTimeOffset.Offset)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="ZonedDateTime"/> from given <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="offset"></param>
        public ZonedDateTime(DateTime dateTime, TimeSpan offset)
            : this(dateTime, (int) offset.TotalSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="ZonedDateTime"/> from given <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="offsetSeconds"></param>
        public ZonedDateTime(DateTime dateTime, int offsetSeconds)
            : this(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second,
                TemporalHelpers.ExtractNanosecondFromTicks(dateTime.Ticks), Zone.Of(offsetSeconds))
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="ZonedDateTime"/> from given <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="zoneId"></param>
        public ZonedDateTime(DateTime dateTime, string zoneId)
            : this(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second,
                TemporalHelpers.ExtractNanosecondFromTicks(dateTime.Ticks), Zone.Of(zoneId))
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="ZonedDateTime"/> from individual date time component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="zone"></param>
        public ZonedDateTime(int year, int month, int day, int hour, int minute, int second, Zone zone)
            : this(year, month, day, hour, minute, second, 0, zone)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="ZonedDateTime"/> from individual date time component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="nanosecond"></param>
        /// <param name="zone"></param>
        public ZonedDateTime(int year, int month, int day, int hour, int minute, int second, int nanosecond,
            Zone zone)
        {
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(year, TemporalHelpers.MinYear, TemporalHelpers.MaxYear,
                nameof(year));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(month, TemporalHelpers.MinMonth,
                TemporalHelpers.MaxMonth, nameof(month));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(day, TemporalHelpers.MinDay,
                TemporalHelpers.MaxDayOfMonth(year, month), nameof(day));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(hour, TemporalHelpers.MinHour, TemporalHelpers.MaxHour,
                nameof(hour));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(minute, TemporalHelpers.MinMinute,
                TemporalHelpers.MaxMinute, nameof(minute));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(second, TemporalHelpers.MinSecond,
                TemporalHelpers.MaxSecond, nameof(second));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(nanosecond, TemporalHelpers.MinNanosecond,
                TemporalHelpers.MaxNanosecond, nameof(nanosecond));
            Throw.ArgumentNullException.IfNull(zone, nameof(zone));

            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            Minute = minute;
            Second = second;
            Nanosecond = nanosecond;
            Zone = zone;
        }

        internal ZonedDateTime(IHasDateTimeComponents dateTime, Zone zone)
            : this(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second,
                dateTime.Nanosecond, zone)
        {

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
        /// The time zone that this instance represents.
        /// </summary>
        public Zone Zone { get; }

        /// <summary>
        /// Gets a <see cref="DateTime"/> value that represents the date and time of this instance.
        /// </summary>
        /// <exception cref="ValueOverflowException">If the value cannot be represented with DateTime</exception>
        /// <exception cref="ValueTruncationException">If a truncation occurs during conversion</exception>
        private DateTime DateTime
        {
            get
            {
                TemporalHelpers.AssertNoTruncation(this, nameof(DateTime));
                TemporalHelpers.AssertNoOverflow(this, nameof(DateTime));

                return new DateTime(Year, Month, Day, Hour, Minute, Second).AddTicks(
                    TemporalHelpers.ExtractTicksFromNanosecond(Nanosecond));
            }
        }

        /// <summary>
        /// Returns the offset from UTC of this instance at the time it represents.
        /// </summary>
        public int OffsetSeconds => Zone.OffsetSecondsAt(new DateTime(Year, Month, Day, Hour, Minute, Second));

        /// <summary>
        /// Gets a <see cref="TimeSpan"/> value that represents the offset of this instance.
        /// </summary>
        private TimeSpan Offset => TimeSpan.FromSeconds(OffsetSeconds);

        /// <summary>
        /// Converts this instance to an equivalent <see cref="DateTimeOffset"/> value
        /// </summary>
        /// <returns>Equivalent <see cref="DateTimeOffset"/> value</returns>
        /// <exception cref="ValueOverflowException">If the value cannot be represented with DateTimeOffset</exception>
        /// <exception cref="ValueTruncationException">If a truncation occurs during conversion</exception>
        public DateTimeOffset ToDateTimeOffset()
        {
            // we first get DateTime instance to force Truncation / Overflow checks
            var dateTime = DateTime;
            var offset = Offset;

            TemporalHelpers.AssertNoTruncation(offset, nameof(DateTimeOffset));
            TemporalHelpers.AssertNoOverflow(offset, nameof(DateTimeOffset));

            return new DateTimeOffset(dateTime, offset);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public long ToUtcEpochSeconds()
        {
            return new DateTimeOffset(Year, Month, Day, Hour, Minute, Second, Offset).ToUniversalTime().ToUnixTimeSeconds();
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="ZonedDateTime"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(ZonedDateTime other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Year == other.Year && Month == other.Month && Day == other.Day && Hour == other.Hour && Second == other.Second && Nanosecond == other.Nanosecond && Equals(Zone, other.Zone);
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="ZonedDateTime"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ZonedDateTime dateTime && Equals(dateTime);
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
                hashCode = (hashCode * 397) ^ Second;
                hashCode = (hashCode * 397) ^ Nanosecond;
                hashCode = (hashCode * 397) ^ (Zone != null ? Zone.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="ZonedDateTime"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return
                $"{TemporalHelpers.ToIsoDateString(Year, Month, Day)}T{TemporalHelpers.ToIsoTimeString(Hour, Minute, Second, Nanosecond)}{Zone}";
        }

        /// <summary>
        /// Compares the value of this instance to a specified <see cref="ZonedDateTime"/> value and returns an integer 
        /// that indicates whether this instance is earlier than, the same as, or later than the specified 
        /// DateTime value.
        /// </summary>
        /// <param name="other">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(ZonedDateTime other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var thisEpochSeconds = this.ToEpochSeconds() - OffsetSeconds;
            var otherEpochSeconds = other.ToEpochSeconds() - other.OffsetSeconds;
            var epochComparison = thisEpochSeconds.CompareTo(otherEpochSeconds);
            if (epochComparison != 0) return epochComparison;
            return Nanosecond.CompareTo(other.Nanosecond);
        }

        /// <summary>
        /// Compares the value of this instance to a specified object which is expected to be a <see cref="ZonedDateTime"/>
        /// value, and returns an integer that indicates whether this instance is earlier than, the same as, 
        /// or later than the specified <see cref="ZonedDateTime"/> value.
        /// </summary>
        /// <param name="obj">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(object obj)
        {
            if (obj is null) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            if (!(obj is ZonedDateTime))
                throw new ArgumentException($"Object must be of type {nameof(ZonedDateTime)}");
            return CompareTo((ZonedDateTime) obj);
        }

        /// <summary>
        /// Determines whether one specified <see cref="ZonedDateTime"/> is earlier than another specified 
        /// <see cref="ZonedDateTime"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is earlier than another, otherwise <code>false</code>.</returns>
        public static bool operator <(ZonedDateTime left, ZonedDateTime right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="ZonedDateTime"/> is later than another specified 
        /// <see cref="ZonedDateTime"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is later than another, otherwise <code>false</code>.</returns>
        public static bool operator >(ZonedDateTime left, ZonedDateTime right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="ZonedDateTime"/> represents a duration that is the 
        /// same as or later than the other specified <see cref="ZonedDateTime"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is the same as or later than another, otherwise <code>false</code>.</returns>
        public static bool operator <=(ZonedDateTime left, ZonedDateTime right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="ZonedDateTime"/> represents a duration that is the 
        /// same as or earlier than the other specified <see cref="ZonedDateTime"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is the same as or earlier than another, otherwise <code>false</code>.</returns>
        public static bool operator >=(ZonedDateTime left, ZonedDateTime right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <inheritdoc cref="TemporalValue.ConvertToDateTimeOffset"/>
        protected override DateTimeOffset ConvertToDateTimeOffset()
        {
            return ToDateTimeOffset();
        }
    }
}