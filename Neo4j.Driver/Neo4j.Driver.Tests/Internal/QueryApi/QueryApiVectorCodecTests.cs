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
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiVectorCodecTests
{
    private readonly QueryApiVectorCodec _subject = new();

    private static JsonElement VectorElement(string coordinatesType, string[] coordinates)
    {
        var json = $$"""{"coordinatesType":"{{coordinatesType}}","coordinates":[{{string.Join(",", coordinates.Select(c => $"\"{c}\""))}}]}""";
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public void CanRead_TrueForVector() => _subject.CanRead("Vector").Should().BeTrue();

    [Fact]
    public void CanRead_FalseForOtherTypes() => _subject.CanRead("String").Should().BeFalse();

    public static IEnumerable<object[]> VectorCoordinateTypes() =>
    [
        ["FLOAT64", new[] { "1.5", "-2.5", "3.0" }, typeof(IVector<double>)],
        ["FLOAT32", new[] { "1.5", "-2.5", "3.0" }, typeof(IVector<float>)],
        ["INT64",   new[] { "100", "-200", "300" },  typeof(IVector<long>)],
        ["INT32",   new[] { "10",  "-20",  "30" },   typeof(IVector<int>)],
        ["INT16",   new[] { "1",   "-5",   "10" },   typeof(IVector<short>)],
        ["INT8",    new[] { "1",   "-5",   "10" },   typeof(IVector<sbyte>)]
    ];

    [Theory]
    [MemberData(nameof(VectorCoordinateTypes))]
    public void Read_ReturnsTypedVector(string coordinatesType, string[] coordinates, Type expectedVectorType)
    {
        var element = VectorElement(coordinatesType, coordinates);
        var result = _subject.Read(element, Mock.Of<IJsonValueDecoder>());
        result.Should().BeAssignableTo(expectedVectorType);
    }

    [Fact]
    public void CanWrite_FalseForVector() =>
        _subject.CanWrite(Vector.CreateDynamic(new[] { 1.0 })).Should().BeFalse();

    [Fact]
    public void Write_ShouldWork()
    {
        var act = () => _subject.Write(Vector.CreateDynamic(new[] { 1.0 }), Mock.Of<IJsonValueEncoder>());
        // TODO - this test is just to make sure that we implement this method
        act.Should().NotThrow();
    }

    [Fact]
    public void VectorCodec_RequiresV1_1()
    {
        _subject.RequiredVersion.Should().Be(QueryApiMediaVersion.V1_1);
    }
}
