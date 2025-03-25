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

using FluentAssertions;
using Neo4j.Driver.Internal.Mapping.TypeConversion;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping.TypeConversion;

public class TypeConversionManagerTests
{
    [Fact]
    public void ShouldConvertTypes()
    {
        var manager = new MappingTypeConversionManager();
        manager.RegisterConverter<int, string>(i => i.ToString());
        manager.RegisterConverter<string, int>(int.Parse);

        manager.TryConvert(42, out string str).Should().BeTrue();
        str.Should().Be("42");

        manager.TryConvert("42", out int i).Should().BeTrue();
        i.Should().Be(42);
    }

    [Fact]
    public void ShouldNotConvertTypes()
    {
        var manager = new MappingTypeConversionManager();
        manager.RegisterConverter<int, string>(i => i.ToString());
        manager.RegisterConverter<string, int>(int.Parse);

        manager.TryConvert(42, out int _).Should().BeFalse();

        manager.TryConvert("42", out string _).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotConvertTypesWhenNoConverterRegistered()
    {
        var manager = new MappingTypeConversionManager();
        manager.TryConvert(42, out string _).Should().BeFalse();
        manager.TryConvert("42", out int _).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotConvertTypesWhenConverterIsNotRegistered()
    {
        var manager = new MappingTypeConversionManager();
        manager.RegisterConverter<int, string>(i => i.ToString());

        manager.TryConvert(42, out int _).Should().BeFalse();
    }

    [Fact]
    public void ShouldConvertTypesCalledWithDifferentTypes()
    {
        var manager = new MappingTypeConversionManager();
        manager.RegisterConverter<int, string>(i => i.ToString());
        manager.RegisterConverter<string, int>(int.Parse);

        manager.TryConvert(typeof(int), typeof(string), 42, out var str).Should().BeTrue();
        str.Should().Be("42");

        manager.TryConvert(typeof(string), typeof(int), "42", out var i).Should().BeTrue();
        i.Should().Be(42);
    }
}
