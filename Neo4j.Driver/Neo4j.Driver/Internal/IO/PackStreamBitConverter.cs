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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo4j.Driver.Internal.IO
{
    internal static class PackStreamBitConverter
    {
        /// <summary>
        ///     Converts a byte to bytes.
        /// </summary>
        /// <param name="value">The byte value to convert.</param>
        /// <returns>The specified byte value as an array of bytes.</returns>
        public static byte[] GetBytes(byte value)
        {
            byte[] bytes = { value };
            return bytes;
        }

        /// <summary>
        ///     Converts a shot (Int16) to bytes.
        /// </summary>
        /// <param name="value">The short (Int16) value to convert.</param>
        /// <returns>The specified short (Int16) value as an array of bytes.</returns>
        public static byte[] GetBytes(short value)
        {
            var bytes = System.BitConverter.GetBytes(value);

            return ToTargetEndian(bytes);
        }

        /// <summary>
        ///     Converts a shot (UInt16) to bytes.
        /// </summary>
        /// <param name="value">The short (UInt16) value to convert.</param>
        /// <returns>The specified short (UInt16) value as an array of bytes.</returns>
        public static byte[] GetBytes(ushort value)
        {
            var bytes = System.BitConverter.GetBytes(value);

            return ToTargetEndian(bytes);
        }

        /// <summary>
        ///     Converts an int (Int32) to bytes.
        /// </summary>
        /// <param name="value">The int (Int32) value to convert.</param>
        /// <returns>The specified int (Int32) value as an array of bytes.</returns>
        public static byte[] GetBytes(int value)
        {
            var bytes = System.BitConverter.GetBytes(value);

            return ToTargetEndian(bytes);
        }

        /// <summary>
        ///     Converts an uint (UInt32) to bytes.
        /// </summary>
        /// <param name="value">The uint (UInt32) value to convert.</param>
        /// <returns>The specified uint (UInt32) value as an array of bytes.</returns>
        public static byte[] GetBytes(uint value)
        {
            var bytes = System.BitConverter.GetBytes(value);

            return ToTargetEndian(bytes);
        }

        /// <summary>
        ///     Converts an int (Int64) to bytes.
        /// </summary>
        /// <param name="value">The int (Int64) value to convert.</param>
        /// <returns>The specified int (Int64) value as an array of bytes.</returns>
        public static byte[] GetBytes(long value)
        {
            var bytes = System.BitConverter.GetBytes(value);

            return ToTargetEndian(bytes);
        }

        /// <summary>
        ///     Converts an int (double) to bytes.
        /// </summary>
        /// <param name="value">The int (double) value to convert.</param>
        /// <returns>The specified int (double) value as an array of bytes.</returns>
        public static byte[] GetBytes(double value)
        {
            var bytes = System.BitConverter.GetBytes(value);

            return ToTargetEndian(bytes);
        }

        /// <summary>
        ///     Converts an string to bytes.
        /// </summary>
        /// <param name="value">The string value to convert.</param>
        /// <returns>The specified string value as an array of bytes.</returns>
        public static byte[] GetBytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        /// <summary>
        ///     Converts an byte array to a short.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <returns>A short converted from the byte array.</returns>
        public static short ToInt16(byte[] bytes)
        {
            bytes = ToPlatformEndian(bytes);
            return System.BitConverter.ToInt16(bytes, 0);
        }

        /// <summary>
        ///     Converts an byte array to a unsigned short.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <returns>A unsigned short converted from the byte array.</returns>
        public static ushort ToUInt16(byte[] bytes)
        {
            bytes = ToPlatformEndian(bytes);
            return System.BitConverter.ToUInt16(bytes, 0);
        }

        /// <summary>
        ///     Converts an byte array to a int (Int32).
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <returns>A int (Int32) converted from the byte array.</returns>
        public static int ToInt32(byte[] bytes)
        {
            bytes = ToPlatformEndian(bytes);
            return System.BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        ///     Converts an byte array to a int (Int64).
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <returns>A int (Int64) converted from the byte array.</returns>
        public static long ToInt64(byte[] bytes)
        {
            bytes = ToPlatformEndian(bytes);
            return System.BitConverter.ToInt64(bytes, 0);
        }

        /// <summary>
        ///     Converts an byte array to a int (double).
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <returns>A int (double) converted from the byte array.</returns>
        public static double ToDouble(byte[] bytes)
        {
            bytes = ToPlatformEndian(bytes);
            return System.BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        ///     Converts an byte array of a UTF8 encoded string to a string
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <returns>A string converted from the byte array</returns>
        public static string ToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        ///     Converts the bytes to big endian.
        /// </summary>
        /// <param name="bytes">The bytes to convert.</param>
        /// <returns>The bytes converted to big endian.</returns>
        private static byte[] ToTargetEndian(byte[] bytes)
        {
            if (System.BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        /// <summary>
        ///     Converts the bytes to the platform endian type.
        /// </summary>
        /// <param name="bytes">The bytes to convert.</param>
        /// <returns>The bytes converted to the platform endian type.</returns>
        private static byte[] ToPlatformEndian(byte[] bytes)
        {
            if (System.BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

    }
}
