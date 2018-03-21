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
using TimeZoneConverter;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Represents a date time value with a time zone, specified as a zone id
    /// </summary>
    public struct CypherDateTimeWithZoneId : ICypherValue, IEquatable<CypherDateTimeWithZoneId>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithZoneId"/> from individual date time component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="zoneId"></param>
        public CypherDateTimeWithZoneId(int year, int month, int day, int hour, int minute, int second, string zoneId)
            : this(year, month, day, hour, minute, second, 0, zoneId)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithZoneId"/> from individual date time component values
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="nanosOfSecond"></param>
        /// <param name="zoneId"></param>
        public CypherDateTimeWithZoneId(int year, int month, int day, int hour, int minute, int second, int nanosOfSecond, string zoneId)
            : this(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified), zoneId)
        {
            NanosOfSecond += nanosOfSecond;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithZoneId"/> from given <see cref="DateTime"/> value
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="zoneId"></param>
        public CypherDateTimeWithZoneId(DateTime dateTime, string zoneId)
            : this(dateTime.Kind == DateTimeKind.Unspecified ? dateTime : new DateTime(dateTime.Ticks, DateTimeKind.Unspecified), zoneId, TemporalHelpers.GetTimeZoneInfo(zoneId))
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDateTimeWithZoneId"/> from given <see cref="DateTimeOffset"/> value
        /// <remarks>Please note that <see cref="DateTimeOffset.Offset"/> is ignored with this constructor</remarks>
        /// </summary>
        /// <param name="dateTimeOffset"></param>
        /// <param name="zoneId"></param>
        public CypherDateTimeWithZoneId(DateTimeOffset dateTimeOffset, string zoneId)
            : this(dateTimeOffset.DateTime, zoneId, TemporalHelpers.GetTimeZoneInfo(zoneId))
        {

        }

        private CypherDateTimeWithZoneId(DateTime dateTime, string zoneId, TimeZoneInfo zoneInfo)
            : this(dateTime.Ticks - zoneInfo.GetUtcOffset(dateTime).Ticks, zoneId)
        {

        }

        private CypherDateTimeWithZoneId(long ticks, string zoneId)
            : this(TemporalHelpers.ComputeSecondsSinceEpoch(ticks),
                TemporalHelpers.ComputeNanosOfSecond(ticks), zoneId)
        {

        }

        internal CypherDateTimeWithZoneId(long epochSeconds, int nanosOfSecond, string zoneId)
        {
            EpochSeconds = epochSeconds;
            NanosOfSecond = nanosOfSecond;
            ZoneId = zoneId;
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
        /// Zone identifier
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// Gets a <see cref="DateTimeOffset"/> copy of this date value.
        /// </summary>
        /// <returns>Equivalent <see cref="DateTimeOffset"/> value</returns>
        /// <exception cref="TruncationException">If a truncation occurs during conversion</exception>
        public DateTimeOffset ToDateTimeOffset()
        {
            var zoneInfo = TemporalHelpers.GetTimeZoneInfo(ZoneId);
            var dateTime = TemporalHelpers.DateTimeOf(EpochSeconds, NanosOfSecond, DateTimeKind.Unspecified, true);

            return new DateTimeOffset(dateTime, zoneInfo.GetUtcOffset(dateTime));
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="CypherDateTimeWithZoneId"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(CypherDateTimeWithZoneId other)
        {
            return EpochSeconds == other.EpochSeconds && NanosOfSecond == other.NanosOfSecond && string.Equals(ZoneId, other.ZoneId);
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="CypherDateTimeWithZoneId"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CypherDateTimeWithZoneId && Equals((CypherDateTimeWithZoneId) obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EpochSeconds.GetHashCode();
                hashCode = (hashCode * 397) ^ NanosOfSecond;
                hashCode = (hashCode * 397) ^ (ZoneId != null ? ZoneId.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="CypherDateTimeWithZoneId"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return $"DateTimeWithOffset{{epochSeconds: {EpochSeconds}, nanosOfSecond: {NanosOfSecond}, zoneId: '{ZoneId}'}}";
        }

    }
}