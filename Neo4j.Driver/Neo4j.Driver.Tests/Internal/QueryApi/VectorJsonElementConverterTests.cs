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
using System.Text.Json;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations.JsonConverters;
using Xunit;
using JsonVector = Neo4j.Driver.Internal.QueryApi.Implementations.JsonConverters.VectorJsonElementConverter.JsonVector;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class VectorJsonElementConverterTests
{
    private static VectorJsonElementConverter Converter(JsonVector returnValue)
    {
        var deserializer = new Mock<IJsonDeserializer>();
        deserializer
            .Setup(d => d.MapObject<JsonVector>(It.IsAny<JsonElement>()))
            .Returns(returnValue);
        return new VectorJsonElementConverter(deserializer.Object);
    }

    [Fact]
    public void CanConvert_ReturnsTrueForVector() =>
        new VectorJsonElementConverter(Mock.Of<IJsonDeserializer>())
            .CanConvert("Vector").Should().BeTrue();

    [Fact]
    public void CanConvert_ReturnsFalseForOtherTypes() =>
        new VectorJsonElementConverter(Mock.Of<IJsonDeserializer>())
            .CanConvert("String").Should().BeFalse();

    [Fact]
    public void Convert_Float64_ReturnsDoubleVector()
    {
        var result = Converter(new() { CoordinatesType = "FLOAT64", Coordinates = ["1.5", "-2.5", "3.0"] })
            .Convert(default);

        result.Should().BeAssignableTo<IVector<double>>().Subject
            .Values.Should().Equal(1.5, -2.5, 3.0);
    }

    [Fact]
    public void Convert_Float32_ReturnsFloatVector()
    {
        var result = Converter(new() { CoordinatesType = "FLOAT32", Coordinates = ["1.5", "-2.5", "3.0"] })
            .Convert(default);

        result.Should().BeAssignableTo<IVector<float>>().Subject
            .Values.Should().Equal(1.5f, -2.5f, 3.0f);
    }

    [Fact]
    public void Convert_Int64_ReturnsLongVector()
    {
        var result = Converter(new() { CoordinatesType = "INT64", Coordinates = ["100", "-200", "300"] })
            .Convert(default);

        result.Should().BeAssignableTo<IVector<long>>().Subject
            .Values.Should().Equal(100L, -200L, 300L);
    }

    [Fact]
    public void Convert_Int32_ReturnsIntVector()
    {
        var result = Converter(new() { CoordinatesType = "INT32", Coordinates = ["10", "-20", "30"] })
            .Convert(default);

        result.Should().BeAssignableTo<IVector<int>>().Subject
            .Values.Should().Equal(10, -20, 30);
    }

    [Fact]
    public void Convert_Int16_ReturnsShortVector()
    {
        var result = Converter(new() { CoordinatesType = "INT16", Coordinates = ["1", "-5", "10"] })
            .Convert(default);

        result.Should().BeAssignableTo<IVector<short>>().Subject
            .Values.Should().Equal((short)1, (short)-5, (short)10);
    }

    [Fact]
    public void Convert_Int8_ReturnsSbyteVector()
    {
        var result = Converter(new() { CoordinatesType = "INT8", Coordinates = ["1", "-5", "10"] })
            .Convert(default);

        result.Should().BeAssignableTo<IVector<sbyte>>().Subject
            .Values.Should().Equal((sbyte)1, (sbyte)-5, (sbyte)10);
    }

    [Fact]
    public void Convert_UnsupportedCoordinatesType_ThrowsNotSupportedException()
    {
        var act = () => Converter(new() { CoordinatesType = "FLOAT16", Coordinates = ["1.0"] })
            .Convert(default);

        act.Should().Throw<NotSupportedException>().WithMessage("*FLOAT16*");
    }

    [Fact]
    public void Convert_DeserializerReturnsNull_ThrowsInvalidOperationException()
    {
        var deserializer = new Mock<IJsonDeserializer>();
        deserializer
            .Setup(d => d.MapObject<JsonVector>(It.IsAny<JsonElement>()))
            .Returns((JsonVector)null!);

        var act = () => new VectorJsonElementConverter(deserializer.Object).Convert(default);

        act.Should().Throw<InvalidOperationException>().WithMessage("*deserialize*");
    }
}
