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
using JsonVector = Neo4j.Driver.Internal.QueryApi.QueryApiVectorCodec.JsonVector;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiVectorCodecTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    private QueryApiVectorCodec SubjectDecoding(JsonVector vector)
    {
        _fixture.Freeze<Mock<IJsonDeserializer>>()
            .Setup(d => d.MapObject<JsonVector>(It.IsAny<JsonElement>()))
            .Returns(vector);

        return _fixture.Create<QueryApiVectorCodec>();
    }

    [Fact]
    public void CanRead_TrueForVector()
    {
        var subject = _fixture.Create<QueryApiVectorCodec>();
        subject.CanRead("Vector").Should().BeTrue();
    }

    [Fact]
    public void CanRead_FalseForOtherTypes()
    {
        var subject = _fixture.Create<QueryApiVectorCodec>();
        subject.CanRead("String").Should().BeFalse();
    }

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
        var subject = SubjectDecoding(new() { CoordinatesType = coordinatesType, Coordinates = coordinates });

        var result = subject.Read(default, Mock.Of<IJsonValueDecoder>());

        result.Should().BeAssignableTo(expectedVectorType);
    }

    [Fact]
    public void CanWrite_FalseForVector()
    {
        var subject = _fixture.Create<QueryApiVectorCodec>();
        subject.CanWrite(Vector.CreateDynamic(new[] { 1.0 })).Should().BeFalse();
    }

    [Fact]
    public void Write_Throws()
    {
        var subject = _fixture.Create<QueryApiVectorCodec>();
        var act = () => subject.Write(Vector.CreateDynamic(new[] { 1.0 }), Mock.Of<IJsonValueEncoder>());

        act.Should().Throw<NotSupportedException>();
    }
}
