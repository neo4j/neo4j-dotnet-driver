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
using Neo4j.Driver.Internal.Util;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Util;

public class BytesToTypedArrayHelperTests
{
    [Fact]
    public void ConvertBytesToTypedArray_SByte_ReturnsCorrectArray()
    {
        // Arrange
        var bytes = new byte[] { 0x7F, 0x80, 0x00, 0xFF }; // max, min, zero, -1

        // Act
        var result = BytesToTypedArrayHelper.ConvertBytesToTypedArray(bytes, typeof(sbyte));

        // Assert
        result.Should().BeOfType<sbyte[]>();
        var sbyteArray = (sbyte[])result;
        sbyteArray.Should().HaveCount(4);
        sbyteArray.Should().Equal(127, -128, 0, -1);
    }

    [Fact]
    public void ConvertBytesToTypedArray_Short_ReturnsCorrectArray()
    {
        // Arrange - big-endian bytes for short values
        var bytes = new byte[] { 0x7F, 0xFF, 0x80, 0x00, 0x00, 0x00, 0xFF, 0xFF }; // 32767, -32768, 0, -1

        // Act
        var result = BytesToTypedArrayHelper.ConvertBytesToTypedArray(bytes, typeof(short));

        // Assert
        result.Should().BeOfType<short[]>();
        var shortArray = (short[])result;
        shortArray.Should().HaveCount(4);
        shortArray.Should().Equal(32767, -32768, 0, -1);
    }

    [Fact]
    public void ConvertBytesToTypedArray_Int_ReturnsCorrectArray()
    {
        // Arrange - big-endian bytes for int values
        var bytes = new byte[]
        {
            0x7F, 0xFF, 0xFF, 0xFF, // 2147483647
            0x80, 0x00, 0x00, 0x00, // -2147483648
            0x00, 0x00, 0x00, 0x00, // 0
            0xFF, 0xFF, 0xFF, 0xFF  // -1
        };

        // Act
        var result = BytesToTypedArrayHelper.ConvertBytesToTypedArray(bytes, typeof(int));

        // Assert
        result.Should().BeOfType<int[]>();
        var intArray = (int[])result;
        intArray.Should().HaveCount(4);
        intArray.Should().Equal(2147483647, -2147483648, 0, -1);
    }

    [Fact]
    public void ConvertBytesToTypedArray_Long_ReturnsCorrectArray()
    {
        // Arrange - big-endian bytes for long values
        var bytes = new byte[]
        {
            0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 9223372036854775807
            0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // -9223372036854775808
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 0
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF  // -1
        };

        // Act
        var result = BytesToTypedArrayHelper.ConvertBytesToTypedArray(bytes, typeof(long));

        // Assert
        result.Should().BeOfType<long[]>();
        var longArray = (long[])result;
        longArray.Should().HaveCount(4);
        longArray.Should().Equal(9223372036854775807L, -9223372036854775808L, 0L, -1L);
    }

    [Fact]
    public void ConvertBytesToTypedArray_Float_ReturnsCorrectArray()
    {
        // Arrange - big-endian bytes for float values (IEEE 754)
        var bytes = new byte[]
        {
            0x3F, 0x80, 0x00, 0x00, // 1.0f
            0xBF, 0x80, 0x00, 0x00, // -1.0f
            0x00, 0x00, 0x00, 0x00, // 0.0f
            0x42, 0x28, 0x00, 0x00  // 42.0f
        };

        // Act
        var result = BytesToTypedArrayHelper.ConvertBytesToTypedArray(bytes, typeof(float));

        // Assert
        result.Should().BeOfType<float[]>();
        var floatArray = (float[])result;
        floatArray.Should().HaveCount(4);
        floatArray.Should().Equal(1.0f, -1.0f, 0.0f, 42.0f);
    }

    [Fact]
    public void ConvertBytesToTypedArray_Double_ReturnsCorrectArray()
    {
        // Arrange - big-endian bytes for double values (IEEE 754)
        var bytes = new byte[]
        {
            0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 1.0
            0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // -1.0
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 0.0
            0x40, 0x45, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00  // 42.0
        };

        // Act
        var result = BytesToTypedArrayHelper.ConvertBytesToTypedArray(bytes, typeof(double));

        // Assert
        result.Should().BeOfType<double[]>();
        var doubleArray = (double[])result;
        doubleArray.Should().HaveCount(4);
        doubleArray.Should().Equal(1.0, -1.0, 0.0, 42.0);
    }

    [Fact]
    public void ConvertBytesToTypedArray_EmptyArray_ReturnsEmptyTypedArray()
    {
        // Arrange
        var bytes = Array.Empty<byte>();

        // Act
        var result = BytesToTypedArrayHelper.ConvertBytesToTypedArray(bytes, typeof(int));

        // Assert
        result.Should().BeOfType<int[]>();
        var intArray = (int[])result;
        intArray.Should().BeEmpty();
    }

    [Fact]
    public void ConvertBytesToTypedArray_SingleElement_ReturnsCorrectArray()
    {
        // Arrange
        var bytes = new byte[] { 0x00, 0x00, 0x00, 0x2A }; // 42 in big-endian

        // Act
        var result = BytesToTypedArrayHelper.ConvertBytesToTypedArray(bytes, typeof(int));

        // Assert
        result.Should().BeOfType<int[]>();
        var intArray = (int[])result;
        intArray.Should().ContainSingle().Which.Should().Be(42);
    }

    [Theory]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(float))]
    [InlineData(typeof(double))]
    public void ConvertBytesToTypedArray_ValidTypes_ReturnsCorrectType(Type elementType)
    {
        // Arrange
        var elementSize = System.Runtime.InteropServices.Marshal.SizeOf(elementType);
        var bytes = new byte[elementSize * 2]; // 2 elements

        // Act
        var result = BytesToTypedArrayHelper.ConvertBytesToTypedArray(bytes, elementType);

        // Assert
        result.Should().BeOfType(elementType.MakeArrayType());
        var array = (Array)result;
        array.Should().HaveCount(2);
    }

    [Fact]
    public void ConvertBytesToTypedArray_DoesNotModifyOriginalByteArray()
    {
        // Arrange
        var originalBytes = new byte[] { 0x00, 0x00, 0x00, 0x01 }; // 1 in big-endian
        var bytesCopy = (byte[])originalBytes.Clone();

        // Act
        BytesToTypedArrayHelper.ConvertBytesToTypedArray(bytesCopy, typeof(int));

        // Assert - on little-endian systems, bytes should be reversed
        if (BitConverter.IsLittleEndian)
        {
            bytesCopy.Should().Equal(originalBytes);
        }
    }
}
