using System.IO;

namespace Neo4j.Driver
{
    public static class SocketExtensions
    {
        public static void Write(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static int Read(this Stream stream, byte[] bytes)
        {
//            while()
            return stream.Read(bytes, 0, bytes.Length);
        }
    }
}