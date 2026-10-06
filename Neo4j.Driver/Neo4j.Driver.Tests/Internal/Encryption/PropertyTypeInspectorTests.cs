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
using FluentAssertions;
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class PropertyTypeInspectorTests
{
    private static readonly BoltValueSerializationSchemeVersion Baseline1_0 = new(1, 0);

    private readonly PropertyTypeInspector _subject = new();

    public static IEnumerable<object[]> UnsupportedValues => new[]
    {
        new object[] { new Dictionary<string, object> { ["k"] = 1L } },
        new object[] { new object() },
        new object[] { new List<object> { new List<long> { 1L } } }
    };

    public static IEnumerable<object[]> HeterogeneousLists => new[]
    {
        new object[] { new List<object> { 1L, "a" } },
        new object[] { new List<object> { 1L, 2.0 } }
    };

    [Theory]
    [InlineData(true, "BOOLEAN")]
    [InlineData(5L, "INTEGER")]
    [InlineData(1.5, "FLOAT")]
    [InlineData("hello", "STRING")]
    public void GetPropertyTypeInfo_ReturnsCanonicalNameAndBaseline1_0_ForScalars(object value, string expectedName)
    {
        var info = _subject.GetPropertyTypeInfo(value);

        info.Name.Should().Be(expectedName);
        info.Baseline.Should().Be(Baseline1_0);
    }

    [Theory]
    [InlineData((sbyte)5, "INTEGER")]
    [InlineData((byte)5, "INTEGER")]
    [InlineData((short)5, "INTEGER")]
    [InlineData(5, "INTEGER")]
    [InlineData(1.5f, "FLOAT")]
    public void GetPropertyTypeInfo_ReturnsTheSamePropertyType_ForNarrowerClrNumerics(object value, string expectedName)
    {
        var info = _subject.GetPropertyTypeInfo(value);

        info.Name.Should().Be(expectedName);
        info.Baseline.Should().Be(Baseline1_0);
    }

    [Fact]
    public void GetPropertyTypeInfo_TreatsIntAndLongElementsAsOneListType()
    {
        var info = _subject.GetPropertyTypeInfo(new List<object> { 1, 2L });

        info.Name.Should().Be("LIST");
    }

    [Fact]
    public void GetPropertyTypeInfo_ReturnsBytesAndBaseline1_0_ForByteArray()
    {
        var info = _subject.GetPropertyTypeInfo(new byte[] { 1, 2, 3 });

        info.Name.Should().Be("BYTES");
        info.Baseline.Should().Be(Baseline1_0);
    }

    [Fact]
    public void GetPropertyTypeInfo_ReturnsListAndBaseline1_0_ForHomogeneousList()
    {
        var info = _subject.GetPropertyTypeInfo(new List<long> { 1, 2 });

        info.Name.Should().Be("LIST");
        info.Baseline.Should().Be(Baseline1_0);
    }

    [Fact]
    public void GetPropertyTypeInfo_ReturnsListAndBaseline1_0_ForEmptyList()
    {
        var info = _subject.GetPropertyTypeInfo(new List<object>());

        info.Name.Should().Be("LIST");
        info.Baseline.Should().Be(Baseline1_0);
    }

    [Theory]
    [MemberData(nameof(UnsupportedValues))]
    public void GetPropertyTypeInfo_Throws_ForUnsupportedType(object value)
    {
        var act = () => _subject.GetPropertyTypeInfo(value);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(HeterogeneousLists))]
    public void GetPropertyTypeInfo_Throws_ForHeterogeneousList(object value)
    {
        var act = () => _subject.GetPropertyTypeInfo(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetPropertyTypeInfo_ReturnsNullAndBaseline1_0_ForNull()
    {
        var info = _subject.GetPropertyTypeInfo(null);

        info.Name.Should().Be("NULL");
        info.Baseline.Should().Be(Baseline1_0);
    }

    [Fact]
    public void GetPropertyTypeInfo_Throws_ForANullInsideAList()
    {
        var act = () => _subject.GetPropertyTypeInfo(new List<object?> { 1L, null });

        act.Should().Throw<ArgumentException>();
    }
}
