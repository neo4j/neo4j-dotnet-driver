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
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Base class for temporal values
    /// </summary>
    public abstract class TemporalValue: IValue, IConvertible
    {
        /// <summary>
        /// Converts this <see cref="TemporalValue"/> instance to a <see cref="DateTime"/> instance.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> value</returns>
        /// <exception cref="InvalidCastException">If conversion is not possible</exception>
        /// <exception cref="ValueTruncationException">If conversion results in a truncation under ms precision</exception>
        /// <exception cref="OverflowException">If the value falls beyond valid range of target type</exception>
        protected virtual DateTime ConvertToDateTime()
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to {nameof(DateTime)} is not supported.");
        }

        /// <summary>
        /// Converts this <see cref="TemporalValue"/> instance to a <see cref="DateTimeOffset"/> instance.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> value</returns>
        /// <exception cref="InvalidCastException">If conversion is not possible</exception>
        /// <exception cref="ValueTruncationException">If conversion results in a truncation under ms precision</exception>
        /// <exception cref="OverflowException">If the value falls beyond valid range of target type</exception>
        protected virtual DateTimeOffset ConvertToDateTimeOffset()
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to {nameof(DateTimeOffset)} is not supported.");
        }

        /// <summary>
        /// Converts this <see cref="TemporalValue"/> instance to a <see cref="TimeSpan"/> instance.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> value</returns>
        /// <exception cref="InvalidCastException">If conversion is not possible</exception>
        /// <exception cref="ValueTruncationException">If conversion results in a truncation under ms precision</exception>
        /// <exception cref="OverflowException">If the value falls beyond valid range of target type</exception>
        protected virtual TimeSpan ConvertToTimeSpan()
        {
            throw new InvalidCastException($"Conversion of {GetType().Name} to {nameof(TimeSpan)} is not supported.");
        }

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
            return ConvertToDateTime();
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(DateTime))
            {
                return ConvertToDateTime();
            }

            if (conversionType == typeof(DateTimeOffset))
            {
                return ConvertToDateTimeOffset();
            }

            if (conversionType == typeof(TimeSpan))
            {
                return ConvertToTimeSpan();
            }

            if (conversionType == typeof(string))
            {
                return ToString();
            }

            throw new InvalidCastException($"Conversion of {GetType().Name} to {conversionType.Name} is not supported.");
        }

        internal class TemporalValueComparer<T> : IComparer<T>
        where T: TemporalValue, IComparable<T>
        {
            public int Compare(T x, T y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (y is null) return 1;
                if (x is null) return -1;
                return x.CompareTo(y);
            }
        }

    }
}