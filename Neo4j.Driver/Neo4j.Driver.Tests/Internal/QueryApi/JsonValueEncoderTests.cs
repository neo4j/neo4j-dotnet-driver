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

using System;
using System.Text.Json.Nodes;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class JsonValueEncoderTests
{
    private static readonly JsonNode SentinelNode = JsonValue.Create("ENCODED")!;

    private static Mock<IQueryApiTypeCodec> Codec(object? value, bool canWrite)
    {
        var codec = new Mock<IQueryApiTypeCodec>();
        codec.Setup(c => c.CanWrite(value)).Returns(canWrite);
        return codec;
    }

    private static JsonValueEncoder Subject(params IQueryApiTypeCodec[] codecs)
    {
        return new JsonValueEncoder(new QueryApiWriteCodecSelector(codecs));
    }

    [Fact]
    public void Encode_ReturnsNodeFromMatchingCodec()
    {
        var value = new object();
        var codec = Codec(value, canWrite: true);
        var subject = Subject(codec.Object);

        subject.Encode(value);

        codec.Verify(c => c.CanWrite(value));
    }

    [Fact]
    public void Encode_Throws_WhenSeveralNonContainerCodecsCanWrite()
    {
        var value = new object();
        var first = Codec(value, canWrite: true);
        var second = Codec(value, canWrite: true);
        var subject = Subject(first.Object, second.Object);

        var act = () => subject.Encode(value);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Encode_SkipsCodec_ThatCannotWriteValue()
    {
        var value = new object();
        var skipped = Codec(value, canWrite: false);
        var chosen = Codec(value, canWrite: true);
        var subject = Subject(skipped.Object, chosen.Object);

        subject.Encode(value);

        skipped.Verify(c => c.CanWrite(value));
        chosen.Verify(c => c.CanWrite(value));
    }

    [Fact]
    public void Encode_Vector_UsesVectorCodec_NotListCodec()
    {
        var vector = Vector.Create(new sbyte[] { 1, -2, 127 });
        var subject = Subject(new QueryApiListCodec(), new QueryApiVectorCodec());

        var node = subject.Encode(vector);

        node!["$type"]!.GetValue<string>().Should().Be("Vector");
    }

    [Fact]
    public void Encode_Throws_WhenNoCodecCanWriteValue()
    {
        var value = new object();
        var subject = Subject(Codec(value, canWrite: false).Object);

        var act = () => subject.Encode(value);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Encode_Throws_WhenNoCodecsExist()
    {
        var subject = Subject();

        var act = () => subject.Encode(new object());

        act.Should().Throw<NotSupportedException>();
    }
}
