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
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class BlueprintMappingTests
{
    [Fact]
    public void ShouldSucceedWithSimpleIllustrativeExample()
    {
        var record = TestRecord.Create(("name", "Alice"), ("age", 69), ("isAdult", true));

        var result = record.AsObjectFromBlueprint(
            new
            {
                name = "",
                age = 0,
                isAdult = false
            });

        result.name.Should().Be("Alice");
        result.age.Should().Be(69);
        result.isAdult.Should().Be(true);
    }

    [Fact]
    public void ShouldMapRecordToBlueprint()
    {
        var record = TestRecord.Create(("x", 69));
        var blueprint = new { x = 0 };

        var result = record.AsObjectFromBlueprint(blueprint);

        result.x.Should().Be(69);
    }

    [Fact]
    public void ShouldMapRecordToBlueprintWithTwoProperties()
    {
        var record = TestRecord.Create(("x", 69), ("y", "test"));
        var blueprint = new { x = 0, y = string.Empty };

        var result = record.AsObjectFromBlueprint(blueprint);

        result.x.Should().Be(69);
        result.y.Should().Be("test");
    }

    [Fact]
    public void ShouldMapRecordToBlueprintWithThreeProperties()
    {
        var record = TestRecord.Create(("x", 69), ("y", "test"), ("z", true));
        var blueprint = new { x = 0, y = string.Empty, z = false };

        var result = record.AsObjectFromBlueprint(blueprint);

        result.x.Should().Be(69);
        result.y.Should().Be("test");
        result.z.Should().Be(true);
    }

    [Fact]
    public void ShouldMapRecordToBlueprintWithMixedTypes()
    {
        var record = TestRecord.Create(("x", 69), ("y", "test"), ("z", true), ("a", 3.14));
        var blueprint = new { x = 0, y = string.Empty, z = false, a = 0.0 };

        var result = record.AsObjectFromBlueprint(blueprint);

        result.x.Should().Be(69);
        result.y.Should().Be("test");
        result.z.Should().Be(true);
        result.a.Should().Be(3.14);
    }

    [Fact]
    public void ShouldThrowMappingFailedExceptionForMissingProperty()
    {
        var record = TestRecord.Create(("x", 69));
        var blueprint = new { x = 0, y = string.Empty };

        Action act = () => record.AsObjectFromBlueprint(blueprint);

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldThrowMappingFailedExceptionForMismatchedTypes()
    {
        var record = TestRecord.Create(("x", "test"));
        var blueprint = new { x = 0 };

        Action act = () => record.AsObjectFromBlueprint(blueprint);

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapPropertiesOfMappableTypes()
    {
        var pointDict = new Dictionary<string, object> { ["x"] = 1, ["y"] = 2 };
        var record = TestRecord.Create(("point", pointDict), ("color", "red"));
        var blueprint = new { point = new Point(0, 0), color = string.Empty };

        var result = record.AsObjectFromBlueprint(blueprint);

        result.point.X.Should().Be(1);
        result.point.Y.Should().Be(2);
        result.color.Should().Be("red");
    }

    [Fact]
    public void ShouldMapPropertiesOfAnonymousTypes()
    {
        var record = TestRecord.Create(
            ("point", new Dictionary<string, object> { ["x"] = 1, ["y"] = 2 }),
            ("color", "red"));

        var result = record.AsObjectFromBlueprint(
            new
            {
                point = new
                {
                    x = 0,
                    y = 0
                },
                color = string.Empty
            });

        result.point.x.Should().Be(1);
        result.point.y.Should().Be(2);
        result.color.Should().Be("red");
    }

    private record Point(
        [MappingSource("x")] int X,
        [MappingSource("y")] int Y);
}
