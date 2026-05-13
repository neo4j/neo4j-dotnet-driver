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
using Neo4j.Driver.Internal.QueryApi.Implementations.JsonConverters;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class VectorJsonElementConverterTests
{
    private static readonly VectorJsonElementConverter Converter = new();

    private static JsonElement Element(string coordinatesType, string coordinates)
    {
        return JsonDocument.Parse(
                $$"""
                  {
                      "$type": "Vector",
                      "_value": {
                          "coordinatesType": "{{coordinatesType}}",
                          "coordinates": {{coordinates}}
                      }
                  }
                  """)
            .RootElement;
    }

    [Fact]
    public void CanConvert_ReturnsTrueForVector()
    {
        Converter.CanConvert("Vector").Should().BeTrue();
    }

    [Fact]
    public void CanConvert_ReturnsFalseForOtherTypes()
    {
        Converter.CanConvert("String").Should().BeFalse();
    }

    [Fact]
    public void Convert_Float64_ReturnsDoubleVector()
    {
        var result = Converter.Convert(Element("FLOAT64", """["1.5","-2.5","3.0"]"""));

        var vector = result.Should().BeAssignableTo<IVector<double>>().Subject;
        vector.Values.Should().Equal(1.5, -2.5, 3.0);
    }

    [Fact]
    public void Convert_Float32_ReturnsFloatVector()
    {
        var result = Converter.Convert(Element("FLOAT32", """["1.5","-2.5","3.0"]"""));

        var vector = result.Should().BeAssignableTo<IVector<float>>().Subject;
        vector.Values.Should().Equal(1.5f, -2.5f, 3.0f);
    }

    [Fact]
    public void Convert_Int64_ReturnsLongVector()
    {
        var result = Converter.Convert(Element("INT64", """["100","-200","300"]"""));

        var vector = result.Should().BeAssignableTo<IVector<long>>().Subject;
        vector.Values.Should().Equal(100L, -200L, 300L);
    }

    [Fact]
    public void Convert_Int32_ReturnsIntVector()
    {
        var result = Converter.Convert(Element("INT32", """["10","-20","30"]"""));

        var vector = result.Should().BeAssignableTo<IVector<int>>().Subject;
        vector.Values.Should().Equal(10, -20, 30);
    }

    [Fact]
    public void Convert_Int16_ReturnsShortVector()
    {
        var result = Converter.Convert(Element("INT16", """["1","-5","10"]"""));

        var vector = result.Should().BeAssignableTo<IVector<short>>().Subject;
        vector.Values.Should().Equal((short)1, (short)-5, (short)10);
    }

    [Fact]
    public void Convert_Int8_ReturnsSbyteVector()
    {
        var result = Converter.Convert(Element("INT8", """["1","-5","10"]"""));

        var vector = result.Should().BeAssignableTo<IVector<sbyte>>().Subject;
        vector.Values.Should().Equal((sbyte)1, (sbyte)-5, (sbyte)10);
    }

    [Fact]
    public void Convert_UnsupportedCoordinatesType_ThrowsNotSupportedException()
    {
        var act = () => Converter.Convert(Element("FLOAT16", """["1.0"]"""));

        act.Should().Throw<NotSupportedException>().WithMessage("*FLOAT16*");
    }
}
