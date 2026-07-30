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

namespace Neo4j.Driver.TestKitBackend.Connection;

// The adapter that confines TextReader to the composition edge; it does not own the reader's
// lifetime (the connection handler disposes the wrappers it creates).
internal class LineReader : ILineReader
{
    private readonly TextReader _reader;

    public LineReader(TextReader reader)
    {
        _reader = reader;
    }

    public Task<string?> ReadLineAsync()
    {
        return _reader.ReadLineAsync();
    }
}
