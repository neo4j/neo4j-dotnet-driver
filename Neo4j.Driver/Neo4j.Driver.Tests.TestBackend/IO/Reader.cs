using System.Text;
using System.IO;
using System.Threading.Tasks;


namespace Neo4j.Driver.Tests.TestBackend
{
    internal class Reader
    {
        private StreamReader InputReader { get; }

        public Reader(Stream inputStream)
        {
            InputReader = new StreamReader(inputStream, Encoding.ASCII);
        }

        public async Task<string> Read()
        {
            return await InputReader.ReadLineAsync().ConfigureAwait(false);
        }

    }
}
