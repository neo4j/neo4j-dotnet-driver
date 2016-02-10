//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Linq;

namespace Neo4j.Driver.Extensions
{
    public static class ByteExtensions
    {
        public static byte[] PadRight(this byte[] bytes, int totalSize)
        {
            var output = new byte[totalSize];
            Array.Copy(bytes, output, bytes.Length);
            return output;
        }

        public static string ToHexString(this byte[] bytes, int start = 0, int size = -1, bool showX = false)
        {
            if (bytes == null)
                return "NULL";

            if (size < 0)
            {
                size = bytes.Length;
            }
           

            var destination = new byte[size];
            Array.Copy(bytes, start, destination, 0, size);
            var output = BitConverter.ToString(destination);

            if (showX)
                return $"0x{output.Replace("-", ", 0x")}";
                
            return output.Replace("-", " ");
        }

        /// <summary>
        /// Takes the format: 00 00 00 and converts to a byte array.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(this string hex)
        {
            hex = hex.Replace(" ", "").Replace(Environment.NewLine, "");
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    }
}