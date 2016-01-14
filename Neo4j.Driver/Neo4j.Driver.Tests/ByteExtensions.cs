using System;

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
    }
}