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
}
