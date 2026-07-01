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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiCodecAssert;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiPointCodecTests
{
    private readonly QueryApiPointCodec _subject = new();

    public static IEnumerable<object[]> RoundTripCases() =>
    [
        [new Point(7203, 0.0, 0.0), "SRID=7203;POINT (0 0)"],
        [new Point(4326, 12.5, -34.25), "SRID=4326;POINT (12.5 -34.25)"],
        [new Point(7203, -0.0, -1.0), "SRID=7203;POINT (-0 -1)"],
        [new Point(9157, 0.0, 0.0, 0.0), "SRID=9157;POINT Z (0 0 0)"],
        [new Point(4979, 1.5, 2.5, 3.5), "SRID=4979;POINT Z (1.5 2.5 3.5)"],
        [
            new Point(9157, double.NegativeInfinity, double.PositiveInfinity, double.NaN),
            "SRID=9157;POINT Z (-Infinity Infinity NaN)"
        ]
    ];

    public static IEnumerable<object[]> ReadOnlyCases() =>
    [
        ["SRID=7203;POINT(1 2)", new Point(7203, 1.0, 2.0)],
        ["SRID=4326;  POINT   (  1.5   -2.5  )", new Point(4326, 1.5, -2.5)],
        ["SRID=9157;POINT Z (1 2 3)", new Point(9157, 1.0, 2.0, 3.0)]
    ];

    public static IEnumerable<object[]> ExtremeFloatCases() =>
    [
        [new Point(4979, Math.Pow(2, 1023), Math.Pow(2, -1022), 9007199254740991.0)],
        [new Point(9157, -9007199254740991.0, -(2 + 1 + 2e-51), 1.7976931348623157E+308)]
    ];

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Write_ReturnsTypedEnvelope(Point value, string expectedValue)
    {
        var result = _subject.Write(value, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be("Point");
        result["_value"]!.GetValue<string>().Should().Be(expectedValue);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Read_ReturnsPoint(Point expected, string wireValue)
    {
        Read(wireValue).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(ReadOnlyCases))]
    public void Read_AcceptsLenientForms(string wireValue, Point expected)
    {
        Read(wireValue).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(ExtremeFloatCases))]
    public void Write_Then_Read_RoundTrips(Point value)
    {
        var written = _subject.Write(value, Mock.Of<IJsonValueEncoder>())!;

        Read(written["_value"]!.GetValue<string>()).Should().Be(value);
    }

    private object? Read(string wireValue)
    {
        using var document = JsonDocument.Parse($$"""{"$type":"Point","_value":"{{wireValue}}"}""");
        return _subject.Read(document.RootElement, Mock.Of<IJsonValueDecoder>());
    }

    [Fact]
    public void CanRead_CorrectTypes()
    {
        CanRead(_subject, "Point");
    }

    [Fact]
    public void CanWrite_TrueForPoint()
    {
        _subject.CanWrite(new Point(7203, 0.0, 0.0)).Should().BeTrue();
    }

    [Fact]
    public void CanWrite_FalseForOtherTypes()
    {
        CanWrite(_subject);
    }
}
