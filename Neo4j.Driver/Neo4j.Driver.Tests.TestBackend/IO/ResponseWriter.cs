// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

    private readonly StreamWriter _writerTarget;

    public ResponseWriter(StreamWriter writer)
    {
        _writerTarget = writer;
    }

    public Task<string> WriteResponseAsync(ProtocolObject protocolObject)
    {
        return WriteResponseAsync(protocolObject.Respond());
    }

    public Task<string> WriteResponseAsync(ProtocolResponse response)
    {
        return WriteResponseAsync(response.Encode());
    }

    public async Task<string> WriteResponseAsync(string response)
    {
        if (string.IsNullOrEmpty(response))
            return string.Empty;

        Trace.WriteLine($"Sending response: {response}\n");

        await _writerTarget.WriteLineAsync(OpenTag);
        await _writerTarget.WriteLineAsync(response);
        await _writerTarget.WriteLineAsync(CloseTag);
        await _writerTarget.FlushAsync();

        return response;
    }
}