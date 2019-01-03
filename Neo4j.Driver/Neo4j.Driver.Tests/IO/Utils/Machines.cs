// Copyright (c) 2002-2019 "Neo4j,"
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
using System.IO;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Tests.IO.Utils
{
    public class PackStreamWriterMachine
    {
        private readonly MemoryStream _output;
        private readonly IPackStreamWriter _writer;

        internal PackStreamWriterMachine(Func<Stream, IPackStreamWriter> writerFactory)
        {
            this._output = new MemoryStream();
            this._writer = writerFactory(_output);
        }

        public void Reset()
        {
            _output.SetLength(0);
        }

        public byte[] GetOutput()
        {
            return _output.ToArray();
        }

        internal IPackStreamWriter Writer()
        {
            return _writer;
        }

    }

    public class PackStreamReaderMachine
    {
        private readonly MemoryStream _input;
        private readonly IPackStreamReader _reader;

        internal PackStreamReaderMachine(byte[] bytes, Func<Stream, IPackStreamReader> readerFactory)
        {
            this._input = new MemoryStream(bytes);
            this._reader = readerFactory(_input);
        }

        internal IPackStreamReader Reader()
        {
            return _reader;
        }

    }
}