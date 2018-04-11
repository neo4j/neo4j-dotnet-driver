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
    /// Represents a date time value with a time zone, specified as a UTC offset
    /// </summary>
    public struct CypherDateTimeWithOffset : ICypherValue, IEquatable<CypherDateTimeWithOffset>, IHasDateTimeComponents
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithOffset"/> from given <see cref="DateTimeOffset"/> value.
        /// </summary>
        /// <param name="dateTimeOffset"></param>
        public CypherDateTimeWithOffset(DateTimeOffset dateTimeOffset)
            : this(dateTimeOffset.DateTime, dateTimeOffset.Offset)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithOffset"/> from given <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="offset"></param>
        public CypherDateTimeWithOffset(DateTime dateTime, TimeSpan offset)
            : this(dateTime, (int)offset.TotalSeconds)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithOffset"/> from given <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="offsetSeconds"></param>
        public CypherDateTimeWithOffset(DateTime dateTime, int offsetSeconds)
            : this(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, TemporalHelpers.ExtractNanosecondFromTicks(dateTime.Ticks), offsetSeconds)
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
        /// <param name="nanosecond"></param>
        /// <param name="offsetSeconds"></param>
        public CypherDateTimeWithOffset(int year, int month, int day, int hour, int minute, int second, int nanosecond, int offsetSeconds)
        {
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(year, TemporalHelpers.MinYear, TemporalHelpers.MaxYear, nameof(year));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(month, TemporalHelpers.MinMonth, TemporalHelpers.MaxMonth, nameof(month));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(day, TemporalHelpers.MinDay, TemporalHelpers.MaxDayOfMonth(year, month), nameof(day));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(hour, TemporalHelpers.MinHour, TemporalHelpers.MaxHour, nameof(hour));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(minute, TemporalHelpers.MinMinute, TemporalHelpers.MaxMinute, nameof(minute));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(second, TemporalHelpers.MinSecond, TemporalHelpers.MaxSecond, nameof(second));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(nanosecond, TemporalHelpers.MinNanosecond, TemporalHelpers.MaxNanosecond, nameof(nanosecond));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(offsetSeconds, TemporalHelpers.MinOffset, TemporalHelpers.MaxOffset, nameof(offsetSeconds));

            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            Minute = minute;
            Second = second;
            Nanosecond = nanosecond;
            OffsetSeconds = offsetSeconds;
        }

        internal CypherDateTimeWithOffset(IHasDateTimeComponents dateTime, int offsetSeconds)
            : this(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second,
                dateTime.Nanosecond, offsetSeconds)
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
        /// Offset in seconds precision
        /// </summary>
        public int OffsetSeconds { get; }

        /// <summary>
        /// Gets a <see cref="DateTime"/> value that represents the date and time of this instance.
        /// </summary>
        public DateTime DateTime
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
        /// Gets a <see cref="TimeSpan"/> value that represents the offset of this instance.
        /// </summary>
        public TimeSpan Offset => TimeSpan.FromSeconds(OffsetSeconds);
        
        /// <summary>
        /// Converts this instance to an equivalent <see cref="DateTimeOffset"/> value
        /// </summary>
        /// <returns>Equivalent <see cref="DateTimeOffset"/> value</returns>
        /// <exception cref="ValueTruncationException">If a truncation occurs during conversion</exception>
        public DateTimeOffset ToDateTimeOffset()
        {
            return new DateTimeOffset(DateTime, Offset);
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="CypherDateTimeWithOffset"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(CypherDateTimeWithOffset other)
        {
            return Year == other.Year && Month == other.Month && Day == other.Day && Hour == other.Hour &&
                   Minute == other.Minute && Second == other.Second && Nanosecond == other.Nanosecond &&
                   OffsetSeconds == other.OffsetSeconds;
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
                var hashCode = Year;
                hashCode = (hashCode * 397) ^ Month;
                hashCode = (hashCode * 397) ^ Day;
                hashCode = (hashCode * 397) ^ Hour;
                hashCode = (hashCode * 397) ^ Minute;
                hashCode = (hashCode * 397) ^ Second;
                hashCode = (hashCode * 397) ^ Nanosecond;
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
            return
                $"{TemporalHelpers.ToIsoDateString(Year, Month, Day)}T{TemporalHelpers.ToIsoTimeString(Hour, Minute, Second, Nanosecond)}{TemporalHelpers.ToIsoTimeZoneOffset(OffsetSeconds)}";
        }
    }
}