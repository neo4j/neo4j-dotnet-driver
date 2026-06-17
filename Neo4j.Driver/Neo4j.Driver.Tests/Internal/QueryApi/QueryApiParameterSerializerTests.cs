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
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Unit tests for the write dispatch in <see cref="QueryApiParameterSerializer"/>: it selects the first codec
/// whose <see cref="IQueryApiTypeCodec.CanWrite"/> accepts the value and delegates the whole envelope to that
/// codec's <see cref="IQueryApiTypeCodec.Write"/>, or throws when none can. The codecs are mocked, so only the
/// serializer's selection logic is exercised here; per-type encoding is covered by the codec tests.
/// </summary>
public class QueryApiParameterSerializerTests
{
    private static readonly IJsonValueEncoder Encoder = Mock.Of<IJsonValueEncoder>();

    private static Mock<IQueryApiTypeCodec> Codec(object? value, bool canWrite, string? body = null)
    {
        var codec = new Mock<IQueryApiTypeCodec>();
        codec.Setup(c => c.CanWrite(value)).Returns(canWrite);

        if (body is not null)
        {
            codec.Setup(c => c.Write(value, It.IsAny<IJsonValueEncoder>()))
                .Returns(JsonValue.Create(body));
        }

        return codec;
    }

    [Fact]
    public void Write_DelegatesToCodec_ThatCanWriteValue()
    {
        var value = new object();
        var subject = new QueryApiParameterSerializer([Codec(value, canWrite: true, body: "DISPATCHED").Object], Encoder);

        subject.GetWrittenJson(value).Should().Be("\"DISPATCHED\"");
    }

    [Fact]
    public void Write_SelectsFirstCodec_WhenSeveralCanWrite()
    {
        var value = new object();
        var first = Codec(value, canWrite: true, body: "FIRST");
        var second = Codec(value, canWrite: true, body: "SECOND");
        var subject = new QueryApiParameterSerializer([first.Object, second.Object], Encoder);

        subject.GetWrittenJson(value).Should().Be("\"FIRST\"");
    }

    [Fact]
    public void Write_SkipsCodec_ThatCannotWriteValue()
    {
        var value = new object();
        var skipped = Codec(value, canWrite: false, body: "SKIPPED");
        var chosen = Codec(value, canWrite: true, body: "CHOSEN");
        var subject = new QueryApiParameterSerializer([skipped.Object, chosen.Object], Encoder);

        subject.GetWrittenJson(value).Should().Be("\"CHOSEN\"");
    }

    [Fact]
    public void Write_Throws_WhenNoCodecCanWriteValue()
    {
        var value = new object();
        var subject = new QueryApiParameterSerializer([Codec(value, canWrite: false).Object], Encoder);

        var act = () => subject.GetWrittenJson(value);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Write_Throws_WhenNoCodecsEvenExist()
    {
        var value = new object();
        var subject = new QueryApiParameterSerializer([], Encoder);

        var act = () => subject.GetWrittenJson(value);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Serialize_RoutesParameterValues_ThroughSelectedCodec()
    {
        var value = new object();
        var subject = new QueryApiParameterSerializer([Codec(value, canWrite: true, body: "ENCODED").Object], Encoder);

        var result = subject.Serialize(new Dictionary<string, object?> { ["x"] = value });

        result.Should().Be("""{"x":"ENCODED"}""");
    }
}

file static class SubjectExtensions
{
    private static readonly JsonSerializerOptions Options = new();

    public static string GetWrittenJson(this QueryApiParameterSerializer subject, object? value)
    {
        var helper = new QueryApiCodecTestHelper();
        subject.Write(helper.Writer, value, Options);
        return helper.WrittenJson;
    }
}
