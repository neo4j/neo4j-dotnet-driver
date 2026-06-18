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

using System.Collections.Generic;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiCodecAssert;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Round-trips the HTTP Query API primitive types through <see cref="QueryApiPrimitiveCodec"/>.
/// Write tests assert the returned <see cref="System.Text.Json.Nodes.JsonNode"/> has the expected
/// <c>{"$type":"...","_value":...}</c> shape. Integers and floats travel as JSON strings; bytes are
/// written via <see cref="IBase64Encoder"/> and read via <see cref="IBase64Decoder"/>.
/// </summary>
public class QueryApiPrimitiveCodecTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    private QueryApiPrimitiveCodec Subject() => _fixture.Create<QueryApiPrimitiveCodec>();

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
    public void Write_ReturnsTypedEnvelope(object? value, string expectedType, string expectedValueJson)
    {
        var result = Subject().Write(value, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be(expectedType);
        // JsonNode? is null for JSON null; convert to "null" string for comparison
        var valueJson = result["_value"]?.ToJsonString() ?? "null";
        valueJson.Should().Be(expectedValueJson);
    }

    [Fact]
    public void Write_Bytes_AsBase64()
    {
        var bytes = new byte[] { 1, 2, 3 };

        _fixture.Freeze<Mock<IBase64Encoder>>()
            .Setup(e => e.Encode(bytes))
            .Returns("AQID");

        var result = Subject().Write(bytes, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be("Base64");
        result["_value"]!.GetValue<string>().Should().Be("AQID");

        result.Should().NotBeSameAs(bytes);
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
        var result = Subject().Read(document.RootElement, Mock.Of<IJsonValueDecoder>());
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
        Subject().Read(document.RootElement, Mock.Of<IJsonValueDecoder>()).Should().BeSameAs(bytes);
    }

    [Fact]
    public void CanRead_CorrectTypes()
    {
        CanRead(
            Subject(),
            "Null",
            "Boolean",
            "Integer",
            "Float",
            "String",
            "Base64",
            "Unsupported");
    }

    [Fact]
    public void CanWrite_CorrectTypes()
    {
        CanWrite(
            Subject(),
            typeof(NullValue),
            typeof(bool),
            typeof(long),
            typeof(int),
            typeof(short),
            typeof(sbyte),
            typeof(double),
            typeof(float),
            typeof(string),
            typeof(byte[]));
    }
}
