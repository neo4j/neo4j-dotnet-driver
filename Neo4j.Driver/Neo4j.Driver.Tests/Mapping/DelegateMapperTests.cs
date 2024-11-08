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

public class DelegateMapperTests
{
    [Fact]
    public void ShouldMap_01_Property()
    {
        var record = TestRecord.Create(("field1", 1));
        var result = record.AsObject((int field1) => new { property1 = field1 });

        result.property1.Should().Be(1);
    }

    [Fact]
    public void ShouldMap_02_Properties()
    {
        var record = TestRecord.Create(("field1", 1), ("field2", "value2"));
        var result = record.AsObject((int field1, string field2) => new { property1 = field1, property2 = field2 });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
    }

    [Fact]
    public void ShouldMap_03_Properties()
    {
        var record = TestRecord.Create(("field1", 1), ("field2", "value2"), ("field3", true));
        var result = record.AsObject(
            (int field1, string field2, bool field3) =>
                new { property1 = field1, property2 = field2, property3 = field3 });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
    }

    [Fact]
    public void ShouldMap_04_Properties()
    {
        var record = TestRecord.Create(("field1", 1), ("field2", "value2"), ("field3", true), ("field4", 3.14));
        var result = record.AsObject(
            (int field1, string field2, bool field3, double field4) => new
                { property1 = field1, property2 = field2, property3 = field3, property4 = field4 });

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
            ("field5", 10L));

        var result = record.AsObject(
            (int field1, string field2, bool field3, double field4, long field5) => new
                { property1 = field1, property2 = field2, property3 = field3, property4 = field4, property5 = field5 });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(10L);
    }

    [Fact]
    public void ShouldMap_06_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", 10L),
            ("field6", "value6"));

        var result = record.AsObject(
            (int field1, string field2, bool field3, double field4, long field5, string field6) => new
            {
                property1 = field1, property2 = field2, property3 = field3, property4 = field4, property5 = field5,
                property6 = field6
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(10L);
        result.property6.Should().Be("value6");
    }

    [Fact]
    public void ShouldMap_07_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", 10L),
            ("field6", "value6"),
            ("field7", "value7"));

        var result = record.AsObject(
            (int field1, string field2, bool field3, double field4, long field5, string field6, string field7) => new
            {
                property1 = field1, property2 = field2, property3 = field3, property4 = field4, property5 = field5,
                property6 = field6, property7 = field7
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(10L);
        result.property6.Should().Be("value6");
        result.property7.Should().Be("value7");
    }

    [Fact]
    public void ShouldMap_08_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", 10L),
            ("field6", "value6"),
            ("field7", "value7"),
            ("field8", "value8"));

        var result = record.AsObject(
            (
                int field1,
                string field2,
                bool field3,
                double field4,
                long field5,
                string field6,
                string field7,
                string field8) => new
            {
                property1 = field1, property2 = field2, property3 = field3, property4 = field4, property5 = field5,
                property6 = field6, property7 = field7, property8 = field8
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(10L);
        result.property6.Should().Be("value6");
        result.property7.Should().Be("value7");
        result.property8.Should().Be("value8");
    }

    [Fact]
    public void ShouldMap_09_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", 10L),
            ("field6", "value6"),
            ("field7", "value7"),
            ("field8", "value8"),
            ("field9", "value9"));

        var result = record.AsObject(
            (
                int field1,
                string field2,
                bool field3,
                double field4,
                long field5,
                string field6,
                string field7,
                string field8,
                string field9) => new
            {
                property1 = field1, property2 = field2, property3 = field3, property4 = field4, property5 = field5,
                property6 = field6, property7 = field7, property8 = field8, property9 = field9
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(10L);
        result.property6.Should().Be("value6");
        result.property7.Should().Be("value7");
        result.property8.Should().Be("value8");
        result.property9.Should().Be("value9");
    }

    [Fact]
    public void ShouldMap_10_Properties()
    {
        var record = TestRecord.Create(
            ("field1", 1),
            ("field2", "value2"),
            ("field3", true),
            ("field4", 3.14),
            ("field5", 10L),
            ("field6", "value6"),
            ("field7", "value7"),
            ("field8", "value8"),
            ("field9", "value9"),
            ("field10", "value10"));

        var result = record.AsObject(
            (
                int field1,
                string field2,
                bool field3,
                double field4,
                long field5,
                string field6,
                string field7,
                string field8,
                string field9,
                string field10) => new
            {
                property1 = field1, property2 = field2, property3 = field3, property4 = field4, property5 = field5,
                property6 = field6, property7 = field7, property8 = field8, property9 = field9, property10 = field10
            });

        result.property1.Should().Be(1);
        result.property2.Should().Be("value2");
        result.property3.Should().Be(true);
        result.property4.Should().Be(3.14);
        result.property5.Should().Be(10L);
        result.property6.Should().Be("value6");
        result.property7.Should().Be("value7");
        result.property8.Should().Be("value8");
        result.property9.Should().Be("value9");
        result.property10.Should().Be("value10");
    }

    [Fact]
    public void ShouldThrowMappingFailedExceptionForMissingProperty()
    {
        var record = TestRecord.Create(("x", 69));

        Action act = () => record.AsObject((int x, string y) => new { x, y });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldThrowMappingFailedExceptionForMismatchedTypes()
    {
        var record = TestRecord.Create(("x", "test"));

        Action act = () => record.AsObject((int x) => new { x });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapWithUserSuppliedLambdaExpression()
    {
        var record = TestRecord.Create(("count", 3), ("letter", 'A'));
        var result = record.AsObject((int count, char letter) => new string(letter, count));

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

        var spokenStatement = record.AsObject(
            (bool isTrue, string description) =>
                isTrue ? new Truth(description) : (SpokenStatement)new Myth(description));

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
            record.AsObject((Person person) => new Truth($"{person.Name} is {person.Age} years old"));

        result.IsTrue.Should().BeTrue();
        result.Description.Should().Be("Alice is 30 years old");
    }

    [Fact]
    public void ShouldMapToObjectParameterIfPossible()
    {
        var dict = new Dictionary<string, object> { ["name"] = "Alice", ["age"] = 30 };
        var record = TestRecord.Create(("person", dict));

        var result = record.AsObject((Person person) => new { person.Name, person.Age });

        result.Name.Should().Be("Alice");
        result.Age.Should().Be(30);
    }

    [Fact]
    public void ShouldSucceedWithMethodInsteadOfLambda()
    {
        var record = TestRecord.Create(("country", "Sweden"), ("population", 1234));
        string MakeStatement(string c,int p) => $"{c} has a population of {p}";

        var result = record.AsObject((string country, int population) => MakeStatement(country, population));

        result.Should().Be("Sweden has a population of 1234");
    }

    [Fact]
    public void ShouldFailIfDelegateThrowsException()
    {
        var record = TestRecord.Create(("x", 69));

        Action act = () => record.AsObject((int x) =>
        {
            if(x == 69)
            {
                throw new Exception("Test exception");
            }

            return new string('A', x);
        });

        act.Should().Throw<MappingFailedException>().WithInnerException<Exception>().WithMessage("Test exception");
    }
}
