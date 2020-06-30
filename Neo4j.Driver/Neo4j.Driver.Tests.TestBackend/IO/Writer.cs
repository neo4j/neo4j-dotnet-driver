using System.IO;
using System.Threading.Tasks;
using System.Text;


namespace Neo4j.Driver.Tests.TestBackend
{
    internal class Writer
    {
        private StreamWriter OutputWriter { get; }

        public Writer(Stream outputStream)
        {
            OutputWriter = new StreamWriter(outputStream, Encoding.ASCII);
        }

        public async Task WriteAsync(string data)
        {   
            await OutputWriter.WriteLineAsync(data).ConfigureAwait(false);
            OutputWriter.Flush();
            await OutputWriter.BaseStream.FlushAsync();
        }

        public void Write(string data)
        {
            OutputWriter.WriteLine(data);
            OutputWriter.Flush();
            OutputWriter.BaseStream.Flush();
        }
    }
}
