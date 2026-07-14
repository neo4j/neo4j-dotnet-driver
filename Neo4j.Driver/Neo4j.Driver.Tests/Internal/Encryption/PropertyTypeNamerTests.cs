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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class PropertyTypeNamerTests
{
    private readonly PropertyTypeNamer _subject = new();

    [Theory]
    [InlineData(true, "BOOLEAN")]
    [InlineData(5L, "INTEGER")]
    [InlineData(1.5, "FLOAT")]
    [InlineData("hello", "STRING")]
    public void GetTypeName_ReturnsCanonicalName_ForScalars(object value, string expected)
    {
        _subject.GetTypeName(value).Should().Be(expected);
    }

    [Fact]
    public void GetTypeName_ReturnsBytes_ForByteArray()
    {
        _subject.GetTypeName(new byte[] { 1, 2, 3 }).Should().Be("BYTES");
    }

    [Fact]
    public void GetTypeName_ReturnsList_ForList()
    {
        _subject.GetTypeName(new List<long> { 1, 2 }).Should().Be("LIST");
    }

    [Fact]
    public void GetTypeName_Throws_ForUnsupportedType()
    {
        var act = () => _subject.GetTypeName(this);

        act.Should().Throw<System.ArgumentException>();
    }
}
