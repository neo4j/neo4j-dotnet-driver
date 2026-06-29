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

#nullable enable

using System.Buffers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class JsonEnvelopeWriterTests
{
    private readonly JsonEnvelopeWriter _subject = new();

    [Fact]
    public void OpenTypedEnvelope_WrapsScalarBody_InTypedEnvelope()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        using (_subject.OpenTypedEnvelope(writer, "Integer"))
        {
            writer.WriteStringValue("42");
        }

        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);

        result.Should().Be("""{"$type":"Integer","_value":"42"}""");
    }

    [Fact]
    public void OpenTypedEnvelope_WrapsObjectBody_InTypedEnvelope()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        using (_subject.OpenTypedEnvelope(writer, "Map"))
        {
            writer.WriteStartObject();
            writer.WriteString("k", "v");
            writer.WriteEndObject();
        }

        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);

        result.Should().Be("""{"$type":"Map","_value":{"k":"v"}}""");
    }
}
