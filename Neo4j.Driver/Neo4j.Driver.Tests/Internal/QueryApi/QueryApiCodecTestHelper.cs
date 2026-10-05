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

using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiCodecTestHelper
{
    private ArrayBufferWriter<byte> _buffer;

    public QueryApiCodecTestHelper()
    {
        _buffer = new ArrayBufferWriter<byte>();
        Writer = new Utf8JsonWriter(_buffer);
    }

    public Utf8JsonWriter Writer { get; }

    public string WrittenJson
    {
        get
        {
            Writer.Flush();
            return Encoding.UTF8.GetString(_buffer.WrittenSpan);
        }
    }
}
