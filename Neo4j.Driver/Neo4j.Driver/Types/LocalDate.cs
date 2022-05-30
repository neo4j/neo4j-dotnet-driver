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
    /// Represents a date value, without a time zone and time related components
    /// </summary>
    public sealed class LocalDate : TemporalValue, IEquatable<LocalDate>, IComparable, IComparable<LocalDate>, IHasDateComponents
    {
        /// <summary>
        /// Default comparer for <see cref="LocalDate"/> values.
        /// </summary>
        public static readonly IComparer<LocalDate> Comparer = new TemporalValueComparer<LocalDate>();

        /// <summary>
        /// Initializes a new instance of <see cref="LocalDate"/> from a date value
        /// </summary>
        /// <param name="date"></param>
        public LocalDate(DateTime date)
            : this(date.Year, date.Month, date.Day)
        {

        }
#if NET6_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance of <see cref="LocalDate"/> from a date value
        /// </summary>
        /// <param name="date"></param>
        public LocalDate(DateOnly date)
            : this(date.Year, date.Month, date.Day)
        {

        }
#endif
        /// <summary>
        /// Initializes a new instance of <see cref="LocalDate"/> from individual date component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        public LocalDate(int year, int month, int day)
        {
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(year, TemporalHelpers.MinYear, TemporalHelpers.MaxYear, nameof(year));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(month, TemporalHelpers.MinMonth, TemporalHelpers.MaxMonth, nameof(month));
            Throw.ArgumentOutOfRangeException.IfValueNotBetween(day, TemporalHelpers.MinDay, TemporalHelpers.MaxDayOfMonth(year, month), nameof(day));

            Year = year;
            Month = month;
            Day = day;
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
        /// Converts this date value to a <see cref="DateTime"/> instance.
        /// </summary>
        /// <value>Equivalent <see cref="DateTime"/> value</value>
        /// <exception cref="ValueOverflowException">If the value cannot be represented with DateTime</exception>
        /// <returns>A <see cref="DateTime"/> instance.</returns>
        public DateTime ToDateTime()
        {
            TemporalHelpers.AssertNoOverflow(this, nameof(DateTime));

            return new DateTime(Year, Month, Day);
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Converts this date value to a <see cref="DateOnly"/> instance.
        /// </summary>
        /// <exception cref="ValueOverflowException">If the value cannot be represented with DateOnly.</exception>
        /// <returns></returns>
        public DateOnly ToDateOnly()
        {
            TemporalHelpers.AssertValidDateOnly(this);
            return new DateOnly(Year, Month, Day);
        }
#endif
        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="LocalDate"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(LocalDate other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Year == other.Year && Month == other.Month && Day == other.Day;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="LocalDate"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is LocalDate date && Equals(date);
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
                return hashCode;
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="LocalDate"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return TemporalHelpers.ToIsoDateString(Year, Month, Day);
        }

        /// <summary>
        /// Compares the value of this instance to a specified <see cref="LocalDate"/> value and returns an integer 
        /// that indicates whether this instance is earlier than, the same as, or later than the specified 
        /// DateTime value.
        /// </summary>
        /// <param name="other">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(LocalDate other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var yearComparison = Year.CompareTo(other.Year);
            if (yearComparison != 0) return yearComparison;
            var monthComparison = Month.CompareTo(other.Month);
            if (monthComparison != 0) return monthComparison;
            return Day.CompareTo(other.Day);
        }

        /// <summary>
        /// Compares the value of this instance to a specified object which is expected to be a <see cref="LocalDate"/>
        /// value, and returns an integer that indicates whether this instance is earlier than, the same as, 
        /// or later than the specified <see cref="LocalDate"/> value.
        /// </summary>
        /// <param name="obj">The object to compare to the current instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and the value parameter.</returns>
        public int CompareTo(object obj)
        {
            if (obj is null) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            if (!(obj is LocalDate)) throw new ArgumentException($"Object must be of type {nameof(LocalDate)}");
            return CompareTo((LocalDate) obj);
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalDate"/> is earlier than another specified 
        /// <see cref="LocalDate"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is earlier than another, otherwise <code>false</code>.</returns>
        public static bool operator <(LocalDate left, LocalDate right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalDate"/> is later than another specified 
        /// <see cref="LocalDate"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is later than another, otherwise <code>false</code>.</returns>
        public static bool operator >(LocalDate left, LocalDate right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalDate"/> represents a duration that is the 
        /// same as or later than the other specified <see cref="LocalDate"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is the same as or later than another.</returns>
        public static bool operator <=(LocalDate left, LocalDate right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalDate"/> represents a duration that is the 
        /// same as or earlier than the other specified <see cref="LocalDate"/> 
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><code>true</code> if one is the same or earlier than another, otherwise <code>false</code>.</returns>
        public static bool operator >=(LocalDate left, LocalDate right)
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