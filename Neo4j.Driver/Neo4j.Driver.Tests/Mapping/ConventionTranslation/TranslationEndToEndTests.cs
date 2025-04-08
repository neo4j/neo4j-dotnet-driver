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
    public void ShouldDoSimpleTranslation()
    {
        var record = TestRecord.Create(["personName", "yearBorn"], ["Bob", 1977]);
        RecordObjectMapping.SetRecordConventionCombiner<CamelCaseCombiner>();
        var person = record.AsObjectFromBlueprint(new { PersonName = "", YearBorn = 0 });
        person.PersonName.Should().Be("Bob");
        person.YearBorn.Should().Be(1977);
    }

    [Fact]
    public void ShouldMapKebabCaseFieldsToSnakeCaseProperties()
    {
        var record = TestRecord.Create(["person-name", "year-born"], ["Bob", 1977]);
        RecordObjectMapping.SetConventionTranslation<SnakeCaseExtractor, KebabCaseCombiner>();
        var person = record.AsObjectFromBlueprint(new { person_name = "", year_born = 0 });
        person.person_name.Should().Be("Bob");
        person.year_born.Should().Be(1977);
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
        RecordObjectMapping.SetRecordConventionCombiner<SnakeCaseCombiner>();
        var person = record.AsObject<ExplicitNamePerson>();
        person.Name.Should().Be("Bob");
        person.YearBorn.Should().Be(1977);
    }

    public record Person(int NumberOfMiddleNames, string FavouriteColor);

    public class FlightCrew(Person pilot, Person coPilot)
    {
        public Person Pilot { get; set; }
        public Person CoPilot { get; set; }
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
        RecordObjectMapping.SetRecordConventionCombiner<SnakeCaseCombiner>();

        var flightCrew = record.AsObject<FlightCrew>();

        flightCrew.Pilot.NumberOfMiddleNames.Should().Be(1);
        flightCrew.Pilot.FavouriteColor.Should().Be("red");
        flightCrew.CoPilot.NumberOfMiddleNames.Should().Be(2);
        flightCrew.CoPilot.FavouriteColor.Should().Be("blue");
    }
}
