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
    /// Represents a date value, without a time zone and time related components
    /// </summary>
    public struct CypherDate : ICypherValue, IEquatable<CypherDate>
    {

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDate"/> from individual date component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        public CypherDate(int year, int month, int day)
            : this(new DateTime(year, month, day))
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDate"/> from a date value
        /// </summary>
        /// <param name="date"></param>
        public CypherDate(DateTime date)
            : this(date.ComputeDaysSinceEpoch())
        {

        }

        internal CypherDate(long epochDays)
        {
            EpochDays = epochDays;
        }

        /// <summary>
        /// Days since Unix Epoch
        /// </summary>
        public long EpochDays { get; }

        /// <summary>
        /// Gets a <see cref="DateTime"/> copy of this date value.
        /// </summary>
        /// <returns>Equivalent <see cref="DateTime"/> value</returns>
        public DateTime ToDateTime()
        {
            return TemporalHelpers.DateOf(EpochDays);
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
            return EpochDays == other.EpochDays;
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
            return EpochDays.GetHashCode();
        }

        /// <summary>
        /// Converts the value of the current <see cref="CypherDate"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return $"Date{{epochDays: {EpochDays}}}";
        }
    }
}