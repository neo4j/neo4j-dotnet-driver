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
using FluentAssertions;
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class PropertyTypeInspectorTests
{
    private static readonly BoltValueSerializationSchemeVersion Baseline1_0 = new(1, 0);

    private readonly PropertyTypeInspector _subject = new();

    public static TheoryData<object> UnsupportedValues()
    {
        return new()
        {
            new Dictionary<string, object> { ["k"] = 1L },
            new object(),
            new List<object> { new List<long> { 1L } },
            Enumerable.Range(1, 2).Select(i => (long)i)
        };
    }

    public static TheoryData<object> SupportedAadValues()
    {
        return new()
        {
            true,
            5L,
            5,
            (short)5,
            (sbyte)5,
            (byte)5,
            "row-42",
            new byte[] { 1, 2, 3 },
            new LocalDate(2026, 10, 6),
            new DateOnly(2026, 10, 6),
            new LocalTime(12, 30, 0),
            new TimeOnly(12, 30, 0),
            new OffsetTime(12, 30, 0, 3600),
            new Point(7203, 1.0, 2.0),
            new Point(9157, 1.0, 2.0, 3.0),
            Guid.Parse("6f1c4e0a-3b8d-4c2e-9a5f-1d2e3f4a5b6c")
        };
    }

    public static TheoryData<object> UnsupportedAadValues()
    {
        return new()
        {
            1.5,
            1.5f,
            1.5m,
            'c',
            new List<object> { "a" },
            new[] { "a" },
            new Dictionary<string, object> { ["k"] = 1L },
            new DateTime(2026, 10, 6, 12, 30, 0, DateTimeKind.Utc),
            new DateTimeOffset(2026, 10, 6, 12, 30, 0, TimeSpan.Zero),
            new LocalDateTime(2026, 10, 6, 12, 30, 0),
            new ZonedDateTime(new DateTimeOffset(2026, 10, 6, 12, 30, 0, TimeSpan.Zero)),
            new Duration(60),
            TimeSpan.FromSeconds(60),
            new object()
        };
    }

    public static TheoryData<object> HeterogeneousLists()
    {
        return new()
        {
            new List<object> { 1L, "a" },
            new List<object> { 1L, 2.0 }
        };
    }

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

    [Theory]
    [MemberData(nameof(SupportedAadValues))]
    public void ValidateAad_Accepts_TheAadTypesTheAdrAllows(object aad)
    {
        var act = () => _subject.ValidateAad(aad);

        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(UnsupportedAadValues))]
    public void ValidateAad_Throws_ForTypesTheAdrDoesNotAllowAsAad(object aad)
    {
        var act = () => _subject.ValidateAad(aad);

        act.Should().Throw<ArgumentException>();
    }
}
