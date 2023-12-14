// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

    public async Task<string> WriteResponseAsync(ProtocolObject protocolObject)
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
