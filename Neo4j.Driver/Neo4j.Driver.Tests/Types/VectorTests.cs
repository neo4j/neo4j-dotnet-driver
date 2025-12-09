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

using System;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Tests.Types;

public class VectorTests
{
    [Fact]
    public void ShouldNotThrowForFloatType()
    {
        Action act = () => _ = new Vector<float>([1.0f, 2.0f, 3.0f]);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShouldNotThrowForSByteType()
    {
        Action act = () => _ = new Vector<sbyte>([1, 2, 3]);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShouldNotThrowForShortType()
    {
        Action act = () => _ = new Vector<short>([1, 2, 3]);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShouldNotThrowForIntType()
    {
        Action act = () => _ = new Vector<int>([1, 2, 3]);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShouldNotThrowForLongType()
    {
        Action act = () => _ = new Vector<long>([1, 2, 3]);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShouldNotThrowForDoubleType()
    {
        Action act = () => _ = new Vector<double>([1.0, 2.0, 3.0]);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShouldThrowForByteType()
    {
        Action act = () => _ = new Vector<byte>();
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ShouldThrowForBoolType()
    {
        Action act = () => _ = new Vector<bool>();
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ShouldInitializeVectorWithValues()
    {
        var values = new[] { 1, 2, 3 };
        var vector = new Vector<int>(values);

        Assert.Equal(values, vector.Values);
    }

    [Fact]
    public void ShouldThrowForNull()
    {
        Assert.Throws<ArgumentException>(() => _ = new Vector<int>(null));
    }

    [Theory]
    [InlineData(new sbyte[] { }, "vector([], 0, INTEGER8 NOT NULL)")]
    [InlineData(new sbyte[] { 0 }, "vector([0], 1, INTEGER8 NOT NULL)")]
    [InlineData(new sbyte[] { 1, -2, 127 }, "vector([1, -2, 127], 3, INTEGER8 NOT NULL)")]
    public void ToString_ReturnsCorrectFormat_SByte(sbyte[] values, string expected)
    {
        var vector = new Vector<sbyte>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new short[] { }, "vector([], 0, INTEGER16 NOT NULL)")]
    [InlineData(new short[] { 0, 100, -32768 }, "vector([0, 100, -32768], 3, INTEGER16 NOT NULL)")]
    public void ToString_ReturnsCorrectFormat_Short(short[] values, string expected)
    {
        var vector = new Vector<short>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new int[] { }, "vector([], 0, INTEGER32 NOT NULL)")]
    [InlineData(new[] { 42, -1000 }, "vector([42, -1000], 2, INTEGER32 NOT NULL)")]
    public void ToString_ReturnsCorrectFormat_Int(int[] values, string expected)
    {
        var vector = new Vector<int>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new long[] { }, "vector([], 0, INTEGER NOT NULL)")]
    [InlineData(new[] { 0, 9223372036854775807L }, "vector([0, 9223372036854775807], 2, INTEGER NOT NULL)")]
    public void ToString_ReturnsCorrectFormat_Long(long[] values, string expected)
    {
        var vector = new Vector<long>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new float[] { }, "vector([], 0, FLOAT32 NOT NULL)")]
    [InlineData(new[] { 0.5f }, "vector([0.5], 1, FLOAT32 NOT NULL)")]
    [InlineData(new[] { -1.5f, 3f }, "vector([-1.5, 3], 2, FLOAT32 NOT NULL)")]
    public void ToString_ReturnsCorrectFormat_Float(float[] values, string expected)
    {
        var vector = new Vector<float>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new double[] { }, "vector([], 0, FLOAT NOT NULL)")]
    [InlineData(new[] { 0.0 }, "vector([0], 1, FLOAT NOT NULL)")]
    [InlineData(new[] { 1.23, -4.56 }, "vector([1.23, -4.56], 2, FLOAT NOT NULL)")]
    public void ToString_ReturnsCorrectFormat_Double(double[] values, string expected)
    {
        var vector = new Vector<double>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new sbyte[] { }, "vector([], 0, INTEGER8 NOT NULL)")]
    public void ToString_HandlesSpecialFloatValues_SByte(sbyte[] values, string expected)
    {
        var vector = new Vector<sbyte>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new short[] { }, "vector([], 0, INTEGER16 NOT NULL)")]
    public void ToString_HandlesSpecialFloatValues_Short(short[] values, string expected)
    {
        var vector = new Vector<short>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new int[] { }, "vector([], 0, INTEGER32 NOT NULL)")]
    public void ToString_HandlesSpecialFloatValues_Int(int[] values, string expected)
    {
        var vector = new Vector<int>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new long[] { }, "vector([], 0, INTEGER NOT NULL)")]
    public void ToString_HandlesSpecialFloatValues_Long(long[] values, string expected)
    {
        var vector = new Vector<long>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new[] { float.NaN }, "vector([NaN], 1, FLOAT32 NOT NULL)")]
    [InlineData(new[] { float.PositiveInfinity }, "vector([Infinity], 1, FLOAT32 NOT NULL)")]
    [InlineData(new[] { float.NegativeInfinity }, "vector([-Infinity], 1, FLOAT32 NOT NULL)")]
    public void ToString_HandlesSpecialFloatValues_Float(float[] values, string expected)
    {
        var vector = new Vector<float>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new[] { double.NaN }, "vector([NaN], 1, FLOAT NOT NULL)")]
    [InlineData(new[] { double.PositiveInfinity }, "vector([Infinity], 1, FLOAT NOT NULL)")]
    [InlineData(new[] { double.NegativeInfinity }, "vector([-Infinity], 1, FLOAT NOT NULL)")]
    [InlineData(new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }, "vector([NaN, Infinity, -Infinity], 3, FLOAT NOT NULL)")]
    public void ToString_HandlesSpecialFloatValues_Double(double[] values, string expected)
    {
        var vector = new Vector<double>(values);
        var result = vector.ToString();
        result.Should().Be(expected);
    }
}
