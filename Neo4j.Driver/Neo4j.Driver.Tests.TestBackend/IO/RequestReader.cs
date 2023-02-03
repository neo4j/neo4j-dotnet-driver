// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class RequestReader
{
    private const string OpenTag = "#request begin";
    private const string CloseTag = "#request end";

    public RequestReader(StreamReader reader)
    {
        InputReader = reader;
    }

    private StreamReader InputReader { get; }
    private bool MessageOpen { get; set; }

    public string CurrentObjectData { get; set; }

    public async Task<bool> ParseNextRequest()
    {
        Trace.WriteLine("Listening for request");

        CurrentObjectData = string.Empty;

        while (await ParseObjectData().ConfigureAwait(false))
        {
            ;
        }

        Trace.WriteLine($"\nRequest recieved: {CurrentObjectData}");

        return !string.IsNullOrEmpty(CurrentObjectData);
    }

    private async Task<bool> ParseObjectData()
    {
        var input = await InputReader.ReadLineAsync();

        if (string.IsNullOrEmpty(input))
        {
            throw new IOException("The stream has been closed, and/or there is no more data on it.");
        }

        if (IsOpenTag(input))
        {
            return true;
        }

        if (IsCloseTag(input))
        {
            return false;
        }

        if (MessageOpen)
        {
            CurrentObjectData += input;
        }

        return true;
    }

    private bool IsOpenTag(string input)
    {
        if (input == OpenTag)
        {
            if (MessageOpen)
            {
                throw new IOException($"Read {OpenTag}, but message already open");
            }

            MessageOpen = true;
            return true;
        }

        return false;
    }

    private bool IsCloseTag(string input)
    {
        if (input == CloseTag)
        {
            if (!MessageOpen)
            {
                throw new IOException($"Read {CloseTag}, but message already closed");
            }

            MessageOpen = false;
            return true;
        }

        return false;
    }

    public IProtocolObject CreateObjectFromData()
    {
        return ProtocolObjectFactory.CreateObject(CurrentObjectData);
    }

    public Type GetObjectType()
    {
        return ProtocolObjectFactory.GetObjectType(CurrentObjectData);
    }
}
