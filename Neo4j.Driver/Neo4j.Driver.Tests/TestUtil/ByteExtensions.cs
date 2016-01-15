using System;
using System.Linq;

namespace Neo4j.Driver.Tests
{
    public static class ByteExtensions
    {
        public static byte[] PadRight(this byte[] bytes, int totalSize)
        {
            var output = new byte[totalSize];
            Array.Copy(bytes, output, bytes.Length);
            return output;
        }

        public static string ToHexString(this byte[] bytes, int start, int size)
        {
            if (bytes == null)
                return "NULL";

            var destination = new byte[size];
            Array.Copy(bytes, start, destination, 0, size);

            return BitConverter.ToString(destination).Replace("-", " ");
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