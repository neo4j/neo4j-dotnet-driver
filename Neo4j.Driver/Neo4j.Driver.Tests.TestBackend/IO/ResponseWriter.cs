using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class ResponseWriter
    {
        const string OpenTag = "#response begin\n";
        const string CloseTag = "\n#response end";
        private Writer WriterTarget { get; set; }


        public ResponseWriter(Writer writer)
        {
            WriterTarget = writer;
        }

        public async Task<string> WriteResponseAsync(IProtocolObject protocolObject)
        {
            var response = protocolObject.Response();
            Trace.WriteLine($"Sending response: {response}\n");

            var message = EncapsulateString(response);
            await WriterTarget.WriteAsync(message).ConfigureAwait(false);
            return message;
        }

        public string WriteResponse(IProtocolObject protocolObject)
        {
            var message = EncapsulateString(protocolObject.Response());
            WriterTarget.Write(message);
            return message;
        }

        private string EncapsulateString(string original)
        {
            string responseString = OpenTag;
            responseString += original;
            responseString += CloseTag;

            return responseString;
        }
    }
}
