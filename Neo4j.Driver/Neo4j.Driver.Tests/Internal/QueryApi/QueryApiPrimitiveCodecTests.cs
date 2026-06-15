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
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Round-trips the HTTP Query API primitive types through <see cref="QueryApiPrimitiveCodec"/>. The codec delegates
/// envelope framing to a mocked <see cref="IJsonEnvelopeWriter"/>, so the Write tests assert the body the codec
/// emits and check that the codec opened the envelope with the right <c>$type</c> and disposed the returned scope.
/// Integers and floats travel as JSON strings on the wire; bytes are written via <see cref="IBase64Encoder"/> and
/// read via <see cref="IBase64Decoder"/>.
/// </summary>
public class QueryApiPrimitiveCodecTests
{
    private static readonly JsonSerializerOptions Options = new();
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    public static IEnumerable<object?[]> WriteCases() =>
    [
        [null, "Null", "null"],
        [true, "Boolean", "true"],
        [42L, "Integer", "\"42\""],
        [7, "Integer", "\"7\""],
        [3.5, "Float", "\"3.5\""],
        ["hi", "String", "\"hi\""]
    ];

    [Theory]
    [MemberData(nameof(WriteCases))]
    public void Write_EmitsBody_WithinTypedEnvelope(object? value, string expectedType, string expectedBody)
    {
        var helper = new QueryApiCodecTestHelper();

        var scope = _fixture.Freeze<Mock<IDisposable>>();
        _fixture.Freeze<Mock<IJsonEnvelopeWriter>>()
            .Setup(e => e.OpenTypedEnvelope(helper.Writer, expectedType))
            .Returns(scope.Object);

        var subject = _fixture.Create<QueryApiPrimitiveCodec>();
        subject.Write(helper.Writer, value, Options);

        helper.WrittenJson.Should().Be(expectedBody);
        scope.Verify(d => d.Dispose());
    }

    [Fact]
    public void Write_Bytes_AsBase64()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var helper = new QueryApiCodecTestHelper();

        var scope = _fixture.Freeze<Mock<IDisposable>>();
        _fixture.Freeze<Mock<IJsonEnvelopeWriter>>()
            .Setup(e => e.OpenTypedEnvelope(helper.Writer, "Base64"))
            .Returns(scope.Object);

        _fixture.Freeze<Mock<IBase64Encoder>>()
            .Setup(e => e.Encode(bytes))
            .Returns("AQID");

        var subject = _fixture.Create<QueryApiPrimitiveCodec>();
        subject.Write(helper.Writer, bytes, Options);

        helper.WrittenJson.Should().Be("\"AQID\"");
        scope.Verify(d => d.Dispose());
    }

    public static IEnumerable<object?[]> ReadCases() =>
    [
        ["""{"$type":"Null","_value":null}""", null],
        ["""{"$type":"Boolean","_value":false}""", false],
        ["""{"$type":"Integer","_value":"42"}""", 42L],
        ["""{"$type":"Float","_value":"3.5"}""", 3.5],
        ["""{"$type":"String","_value":"hi"}""", "hi"]
    ];

    [Theory]
    [MemberData(nameof(ReadCases))]
    public void Read_ReturnsClrValue(string json, object? expected)
    {
        using var document = JsonDocument.Parse(json);
        var subject = _fixture.Create<QueryApiPrimitiveCodec>();

        var result = subject.Read(document.RootElement, Mock.Of<IJsonValueConverter>());

        result.Should().Be(expected);
    }

    [Fact]
    public void Read_Base64_AsBytes()
    {
        var bytes = new byte[] { 1, 2, 3 };

        _fixture.Freeze<Mock<IBase64Decoder>>()
            .Setup(d => d.Decode("AQID"))
            .Returns(bytes);

        using var document = JsonDocument.Parse("""{"$type":"Base64","_value":"AQID"}""");
        var subject = _fixture.Create<QueryApiPrimitiveCodec>();

        var result = subject.Read(document.RootElement, Mock.Of<IJsonValueConverter>());

        result.Should().BeSameAs(bytes);
    }

    // --- Selection ---

    [Theory]
    [InlineData("Null")]
    [InlineData("Boolean")]
    [InlineData("Integer")]
    [InlineData("Float")]
    [InlineData("String")]
    [InlineData("Base64")]
    public void CanRead_TrueForPrimitives(string typeName)
    {
        var subject = _fixture.Create<QueryApiPrimitiveCodec>();
        subject.CanRead(typeName).Should().BeTrue();
    }

    [Fact]
    public void CanRead_FalseForVector()
    {
        var subject = _fixture.Create<QueryApiPrimitiveCodec>();
        subject.CanRead("Vector").Should().BeFalse();
    }

    public static IEnumerable<object?[]> WritablePrimitives() =>
    [
        [null],
        [true],
        [42L],
        [7],
        [(short)1],
        [(sbyte)1],
        [3.14],
        [1.5f],
        ["hi"],
        [new byte[] { 1, 2, 3 }]
    ];

    [Theory]
    [MemberData(nameof(WritablePrimitives))]
    public void CanWrite_TrueForPrimitives(object? value)
    {
        var subject = _fixture.Create<QueryApiPrimitiveCodec>();
        subject.CanWrite(value).Should().BeTrue();
    }

    [Fact]
    public void CanWrite_FalseForUnknownType()
    {
        var subject = _fixture.Create<QueryApiPrimitiveCodec>();
        subject.CanWrite(new object()).Should().BeFalse();
    }
}
