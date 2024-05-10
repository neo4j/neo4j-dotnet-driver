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
using System.Linq.Expressions;
using FluentAssertions;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Preview.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class LambdaMapperTests
{
    [Fact]
    public void ShouldMap_01_Property()
    {
        var record = TestRecord.Create(("field1", 1));
        var result = record.AsObject(field1 => new { property1 = field1.As<int>() });

        result.property1.Should().Be(1);
    }

    [Fact]
    public void ShouldMap_02_Properties()
    {
        var record = TestRecord.Create(("field1", 1), ("field2", "value2"));
        var result = record.AsObject(
            (field1, field2) => new { property1 = field1.As<int>(), property2 = field2.As<string>() });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
    }

    [Fact]
    public void ShouldMap_03_Properties()
    {
        var record = TestRecord.Create(("field1", 1), ("field2", "value2"), ("field3", true));
        var result = record.AsObject(
            (field1, field2, field3) => new
                { property1 = field1.As<int>(), property2 = field2.As<string>(), property3 = field3.As<bool>() });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
    }

    [Fact]
    public void ShouldMap_04_Properties()
    {
        var record = TestRecord.Create(("field1", 1), ("field2", "value2"), ("field3", true), ("field4", 3.14));
        var result = record.AsObject(
            (field1, field2, field3, field4) => new
            {
                property1 = field1.As<int>(), property2 = field2.As<string>(), property3 = field3.As<bool>(),
                property4 = field4.As<double>()
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
    }

    [Fact]
    public void ShouldMap_05_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", new DateTime(2021, 1, 1)));

        var result = record.AsObject(
            (field1, field2, field3, field4, field5) => new
            {
                property1 = field1.As<int>(), property2 = field2.As<string>(), property3 = field3.As<bool>(),
                property4 = field4.As<double>(), property5 = field5.As<DateTime>()
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(new DateTime(2021, 1, 1));
    }

    [Fact]
    public void ShouldMap_06_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", new DateTime(1955, 11, 5, 6, 15, 0)),
            ("field6", "extra1"));

        var result = record.AsObject(
            (field1, field2, field3, field4, field5, field6) => new
            {
                property1 = field1.As<int>(), property2 = field2.As<string>(), property3 = field3.As<bool>(),
                property4 = field4.As<double>(), property5 = field5.As<DateTime>(), property6 = field6.As<string>()
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
        result.property6.Should().Be("extra1");
    }

    [Fact]
    public void ShouldMap_07_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", new DateTime(1955, 11, 5, 6, 15, 0)),
            ("field6", "extra1"),
            ("field7", "extra2"));

        var result = record.AsObject(
            (field1, field2, field3, field4, field5, field6, field7) => new
            {
                property1 = field1.As<int>(), property2 = field2.As<string>(), property3 = field3.As<bool>(),
                property4 = field4.As<double>(), property5 = field5.As<DateTime>(), property6 = field6.As<string>(),
                property7 = field7.As<string>()
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
        result.property6.Should().Be("extra1");
        result.property7.Should().Be("extra2");
    }

    [Fact]
    public void ShouldMap_08_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", new DateTime(1955, 11, 5, 6, 15, 0)),
            ("field6", "extra1"),
            ("field7", "extra2"),
            ("field8", "extra3"));

        var result = record.AsObject(
            (field1, field2, field3, field4, field5, field6, field7, field8) => new
            {
                property1 = field1.As<int>(), property2 = field2.As<string>(), property3 = field3.As<bool>(),
                property4 = field4.As<double>(), property5 = field5.As<DateTime>(), property6 = field6.As<string>(),
                property7 = field7.As<string>(), property8 = field8.As<string>()
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
        result.property6.Should().Be("extra1");
        result.property7.Should().Be("extra2");
        result.property8.Should().Be("extra3");
    }

    [Fact]
    public void ShouldMap_09_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", new DateTime(1955, 11, 5, 6, 15, 0)),
            ("field6", "extra1"),
            ("field7", "extra2"),
            ("field8", "extra3"),
            ("field9", "extra4"));

        var result = record.AsObject(
            (field1, field2, field3, field4, field5, field6, field7, field8, field9) => new
            {
                property1 = field1.As<int>(), property2 = field2.As<string>(), property3 = field3.As<bool>(),
                property4 = field4.As<double>(), property5 = field5.As<DateTime>(), property6 = field6.As<string>(),
                property7 = field7.As<string>(), property8 = field8.As<string>(), property9 = field9.As<string>()
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
        result.property6.Should().Be("extra1");
        result.property7.Should().Be("extra2");
        result.property8.Should().Be("extra3");
        result.property9.Should().Be("extra4");
    }

    [Fact]
    public void ShouldMap_10_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", new DateTime(1955, 11, 5, 6, 15, 0)),
            ("field6", "extra1"),
            ("field7", "extra2"),
            ("field8", "extra3"),
            ("field9", "extra4"),
            ("field10", "extra5"));

        var result = record.AsObject(
            (field1, field2, field3, field4, field5, field6, field7, field8, field9, field10) => new
            {
                property1 = field1.As<int>(), property2 = field2.As<string>(), property3 = field3.As<bool>(),
                property4 = field4.As<double>(), property5 = field5.As<DateTime>(), property6 = field6.As<string>(),
                property7 = field7.As<string>(), property8 = field8.As<string>(), property9 = field9.As<string>(),
                property10 = field10.As<string>()
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
        result.property6.Should().Be("extra1");
        result.property7.Should().Be("extra2");
        result.property8.Should().Be("extra3");
        result.property9.Should().Be("extra4");
        result.property10.Should().Be("extra5");
    }

    [Fact]
    public void ShouldThrowMappingFailedExceptionForMissingProperty()
    {
        var record = TestRecord.Create(("x", 69));

        Action act = () => record.AsObject((x, y) => new { x = x.As<int>(), y = y.As<string>() });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldThrowMappingFailedExceptionForMismatchedTypes()
    {
        var record = TestRecord.Create(("x", "test"));

        Action act = () => record.AsObject(x => new { x = x.As<int>() });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapWithUserSuppliedLambdaExpression()
    {
        var record = TestRecord.Create(("count", 3), ("letter", 'A'));
        var result = record.AsObject<string>((int count, char letter) => new string(letter, count));

        result.Should().Be("AAA");
    }

    private abstract class SpokenStatement
    {
        public abstract bool IsTrue { get; }
        public string Description { get; }

        protected SpokenStatement(string description)
        {
            Description = description;
        }
    }

    private class Truth : SpokenStatement
    {
        public override bool IsTrue => true;

        public Truth(string description) : base(description)
        {
        }
    }

    private class Myth : SpokenStatement
    {
        public override bool IsTrue => false;

        public Myth(string description) : base(description)
        {
        }
    }

    [Fact]
    public void ShouldSucceedForExampleGivenInDocumentation()
    {
        var record = TestRecord.Create(("isTrue", true), ("description", "This is true"));

        var spokenStatement = record.AsObject<SpokenStatement>(
            (bool isTrue, string description) => isTrue
                ? new Truth(description) as SpokenStatement
                : new Myth(description));

        spokenStatement.Should().BeOfType<Truth>();
        spokenStatement.Description.Should().Be("This is true");
    }

    private record Person([MappingSource("name")] string Name, [MappingSource("age")] int Age);

    [Fact]
    public void ShouldPassMappedObjectsToLambdaWhenRequired()
    {
        var dict = new Dictionary<string, object> { ["name"] = "Alice", ["age"] = 30 };
        var record = TestRecord.Create(("person", dict));

        var result =
            record.AsObject<SpokenStatement>((Person person) => new Truth($"{person.Name} is {person.Age} years old"));

        result.IsTrue.Should().BeTrue();
        result.Description.Should().Be("Alice is 30 years old");
    }

    [Fact]
    public void ShouldFailWhenLambdaResultTimeNotCompatibleWithTypeParameter()
    {
        var dict = new Dictionary<string, object> { ["name"] = "Alice", ["age"] = 30 };
        var record = TestRecord.Create(("person", dict));

        Action act = () => record.AsObject<SpokenStatement>((Person person) => person);

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWhenDelegateParameterCannotBeMapped()
    {
        var dict = new Dictionary<string, object> { ["name"] = "Alice", ["age"] = 30 };
        var record = TestRecord.Create(("person", dict));

        Action act = () => record.AsObject<SpokenStatement>((int x) => new Truth(""));

        act.Should().Throw<MappingFailedException>();
    }
}
