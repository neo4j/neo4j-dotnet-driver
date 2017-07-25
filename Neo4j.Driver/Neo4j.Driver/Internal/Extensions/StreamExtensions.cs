using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo4j.Driver.Internal
{
    internal static class StreamExtensions
    {
        public static void Write(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static int Read(this Stream stream, byte[] bytes)
        {
            int hasRead = 0, offset = 0, toRead = bytes.Length;
            do
            {
                hasRead = stream.Read(bytes, offset, toRead);
                offset += hasRead;
                toRead -= hasRead;
            } while (toRead > 0 && hasRead > 0);

            if (hasRead <= 0)
            {
                throw new IOException($"Failed to read more from input stream. Expected {bytes.Length} bytes, received {offset}.");
            }
            return offset;
        }

    }
}
