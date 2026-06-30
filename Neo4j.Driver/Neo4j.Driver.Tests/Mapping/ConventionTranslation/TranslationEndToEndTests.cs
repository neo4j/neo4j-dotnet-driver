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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Mapping.ConventionTranslation;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping.ConventionTranslation;

public class TranslationEndToEndTests : MappingTestWithGlobalState
{
    [Fact]
    public void ShouldDoDefaultTranslation()
    {
        var record = TestRecord.Create(["personName", "yearBorn"], ["Bob", 1977]);
        RecordObjectMapping.TranslateIdentifiers();
        var person = record.AsObjectFromBlueprint(new { PersonName = "", YearBorn = 0 });
        person.PersonName.Should().Be("Bob");
        person.YearBorn.Should().Be(1977);
    }

    [Fact]
    public void ShouldMapKebabCaseFieldsToSnakeCaseProperties()
    {
        var record = TestRecord.Create(["person-name", "year-born"], ["Bob", 1977]);
        RecordObjectMapping.TranslateIdentifiers(IdentifierCaseConvention.SnakeCase, FieldCaseConvention.KebabCase);
        var person = record.AsObjectFromBlueprint(new { person_name = "", year_born = 0 });
        person.person_name.Should().Be("Bob");
        person.year_born.Should().Be(1977);
    }

    [Fact]
    public void ShouldMapSnakeCaseFieldsToKebabCaseProperties()
    {
        var record = TestRecord.Create(["person_name", "year_born"], ["Bob", 1977]);
        RecordObjectMapping.TranslateIdentifiers(
            IdentifierCaseConvention.ScreamingSnakeCase,
            FieldCaseConvention.SnakeCase);

        var person = record.AsObjectFromBlueprint(new { PERSON_NAME = "", YEAR_BORN = 0 });
        person.PERSON_NAME.Should().Be("Bob");
        person.YEAR_BORN.Should().Be(1977);
    }

    private class ExplicitNamePerson
    {
        [MappingSource("name-of-person")]
        public string Name { get; set; }

        public int YearBorn { get; set; }
    }

    [Fact]
    public void ShouldNotTranslateWhenPropertyIsMarkedWithMappingSourceAttribute()
    {
        var record = TestRecord.Create(["name-of-person", "year_born"], ["Bob", 1977]);
        RecordObjectMapping.TranslateIdentifiers(FieldCaseConvention.SnakeCase);
        var person = record.AsObject<ExplicitNamePerson>();
        person.Name.Should().Be("Bob");
        person.YearBorn.Should().Be(1977);
    }

    public record Person(int NumberOfMiddleNames, string FavouriteColor);

    public class FlightCrew(Person pilot, Person coPilot)
    {
        public Person Pilot { get; set; } = pilot;
        public Person CoPilot { get; set; } = coPilot;
    }

    [Fact]
    public void ShouldTranslateThroughNesting()
    {
        var pilotNode = new Node(
            0,
            [],
            new Dictionary<string, object> { ["number_of_middle_names"] = 1, ["favourite_color"] = "red" });

        var coPilotNode = new Node(
            1,
            [],
            new Dictionary<string, object> { ["number_of_middle_names"] = 2, ["favourite_color"] = "blue" });

        var record = TestRecord.Create(("pilot", pilotNode), ("co_pilot", coPilotNode));
        RecordObjectMapping.TranslateIdentifiers(FieldCaseConvention.SnakeCase);

        var flightCrew = record.AsObject<FlightCrew>();

        flightCrew.Pilot.NumberOfMiddleNames.Should().Be(1);
        flightCrew.Pilot.FavouriteColor.Should().Be("red");
        flightCrew.CoPilot.NumberOfMiddleNames.Should().Be(2);
        flightCrew.CoPilot.FavouriteColor.Should().Be("blue");
    }

    public class CrewMember(string personName, int yearsOfService = 10)
    {
        public string PersonName { get; set; } = personName;
        public int YearsOfService { get; set; } = yearsOfService;
    }

    [Fact]
    public void ShouldNotReMapOptionalConstructorParametersWhenTranslating()
    {
        // "years_of_service" is absent, so the optional constructor parameter falls back to its default.
        // under identifier translation the public property resolves to the same record field as the parameter,
        // so it must not be re-mapped via its setter (which would fail because the field is absent).
        var record = TestRecord.Create(["person_name"], ["Bob"]);
        RecordObjectMapping.TranslateIdentifiers(FieldCaseConvention.SnakeCase);

        var crewMember = record.AsObject<CrewMember>();

        crewMember.PersonName.Should().Be("Bob");
        crewMember.YearsOfService.Should().Be(10);
    }

    public class NestedDottedMember
    {
        [MappingBindings(Path = "mainPerson.fullName")]
        public string Name { get; set; }
    }

    [Fact]
    public void ShouldTranslateEachSegmentOfDotSeparatedNonExplicitPath()
    {
        var personNode = new Node(0, [], new Dictionary<string, object> { ["full_name"] = "Bob" });
        var record = TestRecord.Create(("main_person", personNode));
        RecordObjectMapping.TranslateIdentifiers(FieldCaseConvention.SnakeCase);

        var result = record.AsObject<NestedDottedMember>();

        result.Name.Should().Be("Bob");
    }

    public class ExplicitParamMember
    {
        [MappingConstructor]
        public ExplicitParamMember([MappingSource("title_field")] string titleField = "from-ctor")
        {
            TitleField = titleField;
        }

        public string TitleField { get; set; }
    }

    [Fact]
    public void ShouldRebuildDefaultMapperWhenTranslationConfigChanges()
    {
        RecordObjectMapping.TranslateIdentifiers(FieldCaseConvention.SnakeCase);
        var underSnake = TestRecord.Create(("title_field", "snake"), ("title-field", "kebab"))
        .AsObject<ExplicitParamMember>();

        underSnake.TitleField.Should().Be("snake");

        RecordObjectMapping.TranslateIdentifiers(FieldCaseConvention.KebabCase);
        var underKebab = TestRecord.Create(("title_field", "snake"), ("title-field", "kebab"))
            .AsObject<ExplicitParamMember>();
            
        underKebab.TitleField.Should().Be("kebab");
    }
}
