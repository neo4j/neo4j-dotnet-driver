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
    /// Represents a date value, without a time zone and time related components
    /// </summary>
    public struct CypherDate : ICypherValue, IEquatable<CypherDate>, IHasDateComponents
    {

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDate"/> from a date value
        /// </summary>
        /// <param name="date"></param>
        public CypherDate(DateTime date)
            : this(date.Year, date.Month, date.Day)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDate"/> from individual date component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        public CypherDate(int year, int month, int day)
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
        /// Gets a <see cref="DateTime"/> copy of this date value.
        /// </summary>
        /// <value>Equivalent <see cref="DateTime"/> value</value>
        /// <exception cref="ValueOverflowException">If the value cannot be represented with DateTime</exception>
        public DateTime DateTime
        {
            get
            {
                TemporalHelpers.AssertNoOverflow(this, nameof(System.DateTime));

                return new DateTime(Year, Month, Day);
            }
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="CypherDate"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(CypherDate other)
        {
            return Year == other.Year && Month == other.Month && Day == other.Day;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="CypherDate"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CypherDate && Equals((CypherDate) obj);
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
        /// Converts the value of the current <see cref="CypherDate"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return TemporalHelpers.ToIsoDateString(Year, Month, Day);
        }
    }
}