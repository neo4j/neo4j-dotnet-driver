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

public class PropertyTypeValidatorTests
{
    private readonly IPropertyTypeValidator _subject = new PropertyTypeValidator();

    public static IEnumerable<object[]> SupportedValues => new[]
    {
        new object[] { true },
        new object[] { 42L },
        new object[] { 3.14 },
        new object[] { "hello" },
        new object[] { new byte[] { 1, 2, 3 } },
        new object[] { new List<long> { 1L, 2L, 3L } },
        new object[] { new List<string> { "a", "b" } }
    };

    public static IEnumerable<object[]> UnsupportedValues => new[]
    {
        new object[] { new Dictionary<string, object> { ["k"] = 1L } },
        new object[] { new object() },
        new object[] { new List<object> { new List<long> { 1L } } }
    };

    [Theory]
    [MemberData(nameof(SupportedValues))]
    public void EnsureSupported_DoesNotThrow_ForPropertyType(object value)
    {
        var act = () => _subject.EnsureSupported(value);

        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(UnsupportedValues))]
    public void EnsureSupported_Throws_ForUnsupportedType(object value)
    {
        var act = () => _subject.EnsureSupported(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnsureSupported_Throws_ForNull()
    {
        var act = () => _subject.EnsureSupported(null!);

        act.Should().Throw<ArgumentException>();
    }
}
