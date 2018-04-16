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
    /// Represents a local time value
    /// </summary>
    public sealed class LocalTime : IValue, IEquatable<LocalTime>, IComparable, IComparable<LocalTime>, IConvertible, IHasTimeComponents
    {
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
        /// Gets a <see cref="TimeSpan"/> copy of this time value.
        /// </summary>
        /// <value>Equivalent <see cref="TimeSpan"/> value</value>
        /// <exception cref="ValueTruncationException">If a truncation occurs during conversion</exception>
        public TimeSpan Time
        {
            get
            {
                TemporalHelpers.AssertNoTruncation(this, nameof(TimeSpan));

                return new TimeSpan(0, Hour, Minute, Second).Add(
                    TimeSpan.FromTicks(TemporalHelpers.ExtractTicksFromNanosecond(Nanosecond)));
            }
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="LocalTime" /> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(LocalTime other)
        {
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
            if (ReferenceEquals(null, obj)) return false;
            return obj is LocalTime && Equals((LocalTime) obj);
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
            if (ReferenceEquals(null, obj)) return 1;
            if (!(obj is LocalTime)) throw new ArgumentException($"Object must be of type {nameof(LocalTime)}");
            return CompareTo((LocalTime) obj);
        }

        /// <summary>
        /// Determines whether one specified <see cref="LocalTime"/> is earlier than another specified 
        /// <see cref="LocalTime"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
        public static bool operator >=(LocalTime left, LocalTime right)
        {
            return left.CompareTo(right) >= 0;
        }

        #region IConvertible Implementation

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to boolean is not supported.");
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to char is not supported.");
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to sbyte is not supported.");
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to byte is not supported.");
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to short is not supported.");
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to unsigned short is not supported.");
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to int is not supported.");
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to unsigned int is not supported.");
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to long is not supported.");
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to unsigned long is not supported.");
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to single is not supported.");
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to double is not supported.");
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to decimal is not supported.");
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return DateTime.Today.Add(Time);
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(TimeSpan))
            {
                return Time;
            }

            if (conversionType == typeof(DateTime))
            {
                return DateTime.Today.Add(Time);
            }

            if (conversionType == typeof(string))
            {
                return ToString();
            }

            throw new InvalidCastException($"Conversion of {GetType().Name} to {conversionType.Name} is not supported.");
        }

        #endregion
    }
}