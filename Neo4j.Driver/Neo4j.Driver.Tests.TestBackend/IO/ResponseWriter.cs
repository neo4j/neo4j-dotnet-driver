using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ResponseWriter
{
    private const string OpenTag = "#response begin";
    private const string CloseTag = "#response end";

    public ResponseWriter(StreamWriter writer)
    {
        WriterTarget = writer;
    }

    private StreamWriter WriterTarget { get; }

    public async Task<string> WriteResponseAsync(IProtocolObject protocolObject)
    {
        return await WriteResponseAsync(protocolObject.Respond());
    }

    public async Task<string> WriteResponseAsync(ProtocolResponse response)
    {
        return await WriteResponseAsync(response.Encode());
    }

    public async Task<string> WriteResponseAsync(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            return string.Empty;
        }

        Trace.WriteLine($"Sending response: {response}\n");

        await WriterTarget.WriteLineAsync(OpenTag);
        await WriterTarget.WriteLineAsync(response);
        await WriterTarget.WriteLineAsync(CloseTag);
        await WriterTarget.FlushAsync();

        return response;
    }
}
