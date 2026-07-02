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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiCodecAssert;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiVectorCodecTests
{
    private readonly QueryApiVectorCodec _subject = new();

    public static IEnumerable<object[]> WriteCases() =>
    [
        [Vector.Create(new sbyte[] { 1, -5, 10 }), "INT8", new[] { "1", "-5", "10" }],
        [Vector.Create(new short[] { 1, -5, 10 }), "INT16", new[] { "1", "-5", "10" }],
        [Vector.Create(new[] { 10, -20, 30 }), "INT32", new[] { "10", "-20", "30" }],
        [Vector.Create(new[] { 100L, -200L, 300L }), "INT64", new[] { "100", "-200", "300" }],
        [Vector.Create(new[] { 1.5f, -2.5f, 3f }), "FLOAT32", new[] { "1.5", "-2.5", "3" }],
        [Vector.Create(new[] { 1.5, -2.5, 3.0 }), "FLOAT64", new[] { "1.5", "-2.5", "3" }]
    ];

    public static IEnumerable<object[]> RoundTripCases() =>
    [
        [Vector.Create(new sbyte[] { sbyte.MinValue, 0, sbyte.MaxValue })],
        [Vector.Create(new short[] { short.MinValue, 0, short.MaxValue })],
        [Vector.Create(new[] { int.MinValue, 0, int.MaxValue })],
        [Vector.Create(new[] { long.MinValue, 0L, long.MaxValue })],
        [Vector.Create(new[] { 0.1f, -0.1f, float.Epsilon, float.MaxValue, 1f / 3f })],
        [Vector.Create(new[] { 0.1, -0.1, double.Epsilon, double.MaxValue, 1.0 / 3.0 })]
    ];

    [Theory]
    [MemberData(nameof(WriteCases))]
    public void Write_ReturnsTypedEnvelope(IVector vector, string coordinatesType, string[] expected)
    {
        var result = _subject.Write(vector, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be("Vector");
        result["_value"]!["coordinatesType"]!.GetValue<string>().Should().Be(coordinatesType);
        result["_value"]!["coordinates"]!.AsArray()
            .Select(c => c!.GetValue<string>())
            .Should()
            .Equal(expected);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void WriteThenRead_RoundTrips(IVector vector)
    {
        var envelope = _subject.Write(vector, Mock.Of<IJsonValueEncoder>())!;
        using var document = JsonDocument.Parse(envelope.ToJsonString());

        _subject.Read(document.RootElement, Mock.Of<IJsonValueDecoder>()).Should().Be(vector);
    }

    [Theory]
    [MemberData(nameof(WriteCases))]
    public void Read_ReturnsVector(IVector expected, string coordinatesType, string[] coordinates)
    {
        Read(coordinatesType, coordinates).Should().Be(expected);
    }

    private object? Read(string coordinatesType, string[] coordinates)
    {
        var value = new JsonObject
        {
            ["coordinatesType"] = coordinatesType,
            ["coordinates"] = new JsonArray(coordinates.Select(c => (JsonNode)JsonValue.Create(c)!).ToArray())
        };

        using var document = JsonDocument.Parse(
            new JsonObject { ["$type"] = "Vector", ["_value"] = value }.ToJsonString());

        return _subject.Read(document.RootElement, Mock.Of<IJsonValueDecoder>());
    }

    [Fact]
    public void CanRead_CorrectTypes()
    {
        CanRead(_subject, "Vector");
    }

    [Fact]
    public void CanWrite_TrueForVector()
    {
        _subject.CanWrite(Vector.Create(new[] { 1.0 })).Should().BeTrue();
    }

    [Fact]
    public void CanWrite_FalseForOtherTypes()
    {
        CanWrite(_subject);
    }

    [Fact]
    public void VectorCodec_RequiresV1_1()
    {
        _subject.RequiredVersion.Should().Be(QueryApiMediaVersion.V1_1);
    }
}
