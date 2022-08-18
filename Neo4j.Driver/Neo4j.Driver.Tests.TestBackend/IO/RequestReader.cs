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

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class RequestReader
{
    private const string OpenTag = "#request begin";
    private const string CloseTag = "#request end";
    private readonly StreamReader _inputReader;
    private bool _messageOpen;

    public RequestReader(StreamReader reader)
    {
        _inputReader = reader;
    }

    public string CurrentObjectData { get; set; }

    public async Task<bool> ParseNextRequest()
    {
        Trace.WriteLine("Listening for request");

        var stringBuilder = new StringBuilder();

        while (await ParseObjectData(stringBuilder))
        {
        }

        CurrentObjectData = stringBuilder.ToString();
        Trace.WriteLine($"\nRequest received: {CurrentObjectData}");

        return !string.IsNullOrEmpty(CurrentObjectData);
    }

    private async Task<bool> ParseObjectData(StringBuilder stringBuilder)
    {
        var input = await _inputReader.ReadLineAsync();

        if (string.IsNullOrEmpty(input))
            throw new IOException("The stream has been closed, and/or there is no more data on it.");

        if (IsOpenTag(input))
            return true;

        if (IsCloseTag(input))
            return false;

        if (_messageOpen)
            stringBuilder.Append(input);

        return true;
    }

    private bool IsOpenTag(string input)
    {
        if (input != OpenTag)
            return false;

        if (_messageOpen)
            throw new IOException($"Read {OpenTag}, but message already open");

        _messageOpen = true;
        return true;
    }

    private bool IsCloseTag(string input)
    {
        if (input != CloseTag)
            return false;

        if (!_messageOpen)
            throw new IOException($"Read {CloseTag}, but message already closed");

        _messageOpen = false;
        return true;
    }

    public ProtocolObject CreateObjectFromData()
    {
        return ProtocolObjectFactory.CreateObject(CurrentObjectData);
    }

    public Type GetObjectType()
    {
        return ProtocolObjectFactory.GetObjectType(CurrentObjectData);
    }
}