using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class ResponseWriter
    {
        const string OpenTag = "#response begin";
        const string CloseTag = "#response end";
        private StreamWriter WriterTarget { get; set; }


        public ResponseWriter(StreamWriter writer)
        {
            WriterTarget = writer;
        }

        public async Task<string> WriteResponseAsync(IProtocolObject protocolObject)
        {
            return await WriteResponseAsync(protocolObject.Respond());
        }

        public async Task<string> WriteResponseAsync(ProtocolResponse response)
        {
            return await WriteResponseAsync(response.Encode());
        }

        private async Task<string> WriteResponseAsync(string response)
        {
            Trace.WriteLine($"Sending response: {response}\n");

            WriterTarget.WriteLine(OpenTag);
            WriterTarget.WriteLine(response);
            WriterTarget.WriteLine(CloseTag);
            WriterTarget.Flush();

            return response;
        }
    }
}
