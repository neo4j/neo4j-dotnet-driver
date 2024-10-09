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
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class DefaultMapperTests
{
    private class SimpleClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    [Fact]
    public void ShouldMapSimpleClass()
    {
        var mapper = DefaultMapper.Get<SimpleClass>();

        var record = TestRecord.Create(new[] { "Id", "Name" }, new object[] { 1, "Foo" });
        var result = mapper.Map(record);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Foo");
    }

    private class ConstructorClass
    {
        public int Id { get; }
        public string Name { get; }

        public ConstructorClass(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    [Fact]
    public void ShouldMapConstructorClass()
    {
        var mapper = DefaultMapper.Get<ConstructorClass>();

        var record = TestRecord.Create(new[] { "id", "name" }, new object[] { 1, "Foo" });
        var result = mapper.Map(record);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Foo");
    }

    private class NonDefaultConstructorClass
    {
        public int Id { get; }
        public string Name { get; }

        public NonDefaultConstructorClass()
        {
            Id = -99;
            Name = "error";
        }

        [MappingConstructor]
        public NonDefaultConstructorClass(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    [Fact]
    public void ShouldMapNonDefaultConstructorClass()
    {
        var mapper = DefaultMapper.Get<NonDefaultConstructorClass>();

        var record = TestRecord.Create(new[] { "id", "name" }, new object[] { 1, "Foo" });
        var result = mapper.Map(record);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Foo");
    }

    private class Person
    {
        public Person(
            [MappingSource("person.name")] string name,
            [MappingSource("person.born")] int born)
        {
            Name = name;
            Born = born;
        }

        public string Name { get; }
        public int Born { get; }
    }

    [Fact]
    public void ShouldMapFromInsideDictionaries()
    {
        var dict = new Dictionary<string, object> { { "name", "Dani" }, { "born", 1977 } };
        var record = TestRecord.Create(new[] { "Person" }, new object[] { dict });
        var mapper = DefaultMapper.Get<Person>();
        var person = mapper.Map(record);
        person.Name.Should().Be("Dani");
        person.Born.Should().Be(1977);
    }

    [Fact]
    public void ShouldThrowWhenConstructorParametersUnavailable()
    {
        var record = TestRecord.Create(new[] { "something" }, new object[] { 69 });
        var mapper = DefaultMapper.Get<Person>();
        var act = () => mapper.Map(record);
        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapFromNodesInRecords()
    {
        var record = TestRecord.Create(
            new[] { "person" },
            new object[]
            {
                new Node(
                    1,
                    new[] { "Person" },
                    new Dictionary<string, object> { { "name", "Dani" }, { "born", 1977 } })
            });

        var mapper = DefaultMapper.Get<Person>();
        var person = mapper.Map(record);
        person.Name.Should().Be("Dani");
        person.Born.Should().Be(1977);
    }

    public class NaturalPhenomenon
    {
        public string Name { get; }
        public List<string> Components { get; }

        public NaturalPhenomenon(string name, List<string> components)
        {
            Name = name;
            Components = components;
        }
    }

    [Fact]
    public void ShouldMapListsThroughConstructor()
    {
        var record = TestRecord.Create(
            new[] { "name", "components" },
            new object[] { "Hurricane", new List<string> { "wind", "rain" } });

        var mapper = DefaultMapper.Get<NaturalPhenomenon>();
        var result = mapper.Map(record);
        result.Name.Should().Be("Hurricane");
        result.Components.Should().BeEquivalentTo("wind", "rain");
    }

    public class NaturalPhenomenonCommaSeparated
    {
        public string Name { get; }
        public string Components { get; }

        public NaturalPhenomenonCommaSeparated(string name, string components)
        {
            Name = name;
            Components = components;
        }
    }

    [Fact]
    public void ShouldMapCommaSeparatedListsThroughConstructor()
    {
        var record = TestRecord.Create(
            new[] { "name", "components" },
            new object[] { "Hurricane", new List<string> { "wind", "rain" } });

        var mapper = DefaultMapper.Get<NaturalPhenomenonCommaSeparated>();
        var result = mapper.Map(record);
        result.Name.Should().Be("Hurricane");
        result.Components.Should().Be("wind,rain");
    }

    public class HistoricalPhenomenon
    {
        public NaturalPhenomenon Phenomenon { get; }
        public int Year { get; }

        public HistoricalPhenomenon(NaturalPhenomenon phenomenon, int year)
        {
            Phenomenon = phenomenon;
            Year = year;
        }
    }

    [Fact]
    public void ShouldMapNestedObjectsThroughConstructor()
    {
        var record = TestRecord.Create(
            new[] { "phenomenon", "year" },
            new object[]
            {
                new Dictionary<string, object>
                    { { "name", "Hurricane" }, { "components", new List<string> { "wind", "rain" } } },
                2021
            });

        var mapper = DefaultMapper.Get<HistoricalPhenomenon>();
        var result = mapper.Map(record);
        result.Phenomenon.Name.Should().Be("Hurricane");
        result.Phenomenon.Components.Should().BeEquivalentTo("wind", "rain");
        result.Year.Should().Be(2021);
    }

    public class YearOfPhenomena
    {
        public int Year { get; }
        public List<NaturalPhenomenon> Phenomena { get; }

        public YearOfPhenomena(int year, List<NaturalPhenomenon> phenomena)
        {
            Year = year;
            Phenomena = phenomena;
        }
    }

    [Fact]
    public void ShouldMapListsOfNodesThroughConstructor()
    {
        var firstPhenomenon = new Node(
            1,
            new[] { "Phenomenon" },
            new Dictionary<string, object>
                { { "name", "Hurricane" }, { "components", new List<string> { "wind", "rain" } } });

        var secondPhenomenon = new Node(
            2,
            new[] { "Phenomenon" },
            new Dictionary<string, object>
                { { "name", "Tornado" }, { "components", new List<string> { "wind", "debris" } } });

        var thirdPhenomenon = new Node(
            3,
            new[] { "Phenomenon" },
            new Dictionary<string, object>
                { { "name", "Earthquake" }, { "components", new List<string> { "earth", "quaking" } } });

        var phenomena = new List<Node> { firstPhenomenon, secondPhenomenon, thirdPhenomenon };

        var record = TestRecord.Create(
            new[] { "year", "phenomena" },
            new object[] { 2021, phenomena });

        var mapper = DefaultMapper.Get<YearOfPhenomena>();
        var result = mapper.Map(record);
        result.Year.Should().Be(2021);
        result.Phenomena.Should().HaveCount(3);
        result.Phenomena[0].Name.Should().Be("Hurricane");
        result.Phenomena[0].Components.Should().BeEquivalentTo("wind", "rain");
        result.Phenomena[1].Name.Should().Be("Tornado");
        result.Phenomena[1].Components.Should().BeEquivalentTo("wind", "debris");
        result.Phenomena[2].Name.Should().Be("Earthquake");
        result.Phenomena[2].Components.Should().BeEquivalentTo("earth", "quaking");
    }

    private class ClassWithProperties
    {
        public ClassWithProperties(int year, string occurrence)
        {
            Year = year * 10;
            Occurrence = occurrence.ToLowerInvariant();
        }

        public int Year { get; }
        public string Occurrence { get; }

        [MappingSource("description")]
        public string Description { get; set; }
    }

    [Fact]
    public void ShouldSetPropertiesNotSetInConstructor()
    {
        var record = TestRecord.Create(
            new[] { "year", "occurrence", "description", "something" },
            new object[] { 2020, "PANDEMIC", "Covid-19", "something" });

        var mapper = DefaultMapper.Get<ClassWithProperties>();
        var result = mapper.Map(record);

        result.Year.Should().Be(20200);
        result.Occurrence.Should().Be("pandemic");
        result.Description.Should().Be("Covid-19");
    }

    private class ClassWithPropertiesWithMappingHints
    {
        public ClassWithPropertiesWithMappingHints(
            [MappingSource("year")] int year,
            string occurrence)
        {
            Year = year * 10;
            Occurrence = occurrence.ToLowerInvariant();
        }

        public int Year { get; }
        public string Occurrence { get; }

        [MappingSource("description")]
        public string OtherText { get; set; }
    }

    [Fact]
    public void ShouldSetPropertiesNotSetInConstructorWithMappingHints()
    {
        var record = TestRecord.Create(
            new[] { "year", "occurrence", "description", "something" },
            new object[] { 2021, "PANDEMIC", "Covid-19", "something" });

        var mapper = DefaultMapper.Get<ClassWithPropertiesWithMappingHints>();
        var result = mapper.Map(record);

        result.Year.Should().Be(20210);
        result.Occurrence.Should().Be("pandemic");
        result.OtherText.Should().Be("Covid-19");
    }

    private class NoConstructors
    {
        private NoConstructors()
        {
        }
    }

    [Fact]
    public void ShouldThrowIfClassHasNoConstructors()
    {
        var act = () => DefaultMapper.Get<NoConstructors>(null);
        act.Should().Throw<InvalidOperationException>();
    }
}
