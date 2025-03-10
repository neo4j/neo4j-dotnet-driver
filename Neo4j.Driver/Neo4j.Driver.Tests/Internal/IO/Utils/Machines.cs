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

using System;
using System.IO;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Tests.Internal.IO.Utils;

public class PackStreamWriterMachine
{
    private readonly MemoryStream _output;

    internal PackStreamWriterMachine(Func<Stream, PackStreamWriter> writerFactory)
    {
        _output = new MemoryStream();
        Writer = writerFactory(_output);
    }

    internal PackStreamWriter Writer { get; }

    public void Reset()
    {
        _output.SetLength(0);
    }

    public byte[] GetOutput()
    {
        return _output.ToArray();
    }
}

public class PackStreamReaderMachine
{
    private readonly MemoryStream _input;
    private readonly PackStreamReader _reader;

    internal PackStreamReaderMachine(byte[] bytes, Func<MemoryStream, PackStreamReader> readerFactory)
    {
        _input = new MemoryStream(bytes);
        _reader = readerFactory(_input);
    }

    internal PackStreamReader Reader()
    {
        return _reader;
    }
}
