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
using System.Linq;

namespace Neo4j.Driver.Internal
{
    internal static class ByteExtensions
    {
        public static byte[] PadRight(this byte[] bytes, int totalSize)
        {
            var output = new byte[totalSize];
            Array.Copy(bytes, output, bytes.Length);
            return output;
        }

        public static string ToHexString(this byte[] bytes, string separator, int start = 0, int size = -1)
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

            return output.Replace("-", separator);
        }

        public static string ToHexString(this byte[] bytes, int start = 0, int size = -1, bool showX = false)
        {
            var hexStr = bytes.ToHexString(" ", start, size);
            return showX ? $"0x{hexStr.Replace(" ", ", 0x")}" : hexStr;
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
