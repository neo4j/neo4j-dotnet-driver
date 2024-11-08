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
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class RecordMappingTests
{
    private class TestPerson
    {
        [MappingDefaultValue("A. Test Name")]
        [MappingSource("person.name")]
        public string Name { get; set; }

        [MappingOptional]
        [MappingSource("person.born")]
        public int? Born { get; set; }

        [MappingOptional]
        [MappingSource("hobbies")]
        public List<string> Hobbies { get; set; } = null!;
    }

    private class SimpleTestPerson
    {
        [MappingOptional]
        [MappingSource("name")]
        public string Name { get; set; } = "A. Test Name";

        [MappingOptional]
        [MappingSource("born")]
        public int? Born { get; set; }

        [MappingOptional]
        public List<string> Hobbies { get; set; } = null!;
    }

    [Fact]
    public void ShouldMapPrimitives()
    {
        var record = TestRecord.Create(["name", "born"], ["Bob", 1977]);
        var person = record.AsObject<SimpleTestPerson>();
        person.Name.Should().Be("Bob");
        person.Born.Should().Be(1977);
    }

    [Fact]
    public void ShouldMapList()
    {
        var record = TestRecord.Create(["hobbies"], [new List<string> { "Coding", "Swimming" }]);
        var person = record.AsObject<TestPerson>();
        person.Hobbies.Should().BeEquivalentTo("Coding", "Swimming");
    }

    private class PersonInDict
    {
        [MappingSource("person.name")]
        public string Name { get; set; } = "";

        [MappingSource("person.born")]
        public int Born { get; set; }
    }

    [Fact]
    public void ShouldMapFromInsideDictionaries()
    {
        var dict = new Dictionary<string, object> { { "name", "Dani" }, { "born", 1977 } };
        var record = TestRecord.Create(["Person"], [dict]);
        var person = record.AsObject<PersonInDict>();
        person.Name.Should().Be("Dani");
        person.Born.Should().Be(1977);
    }

    [Fact]
    public void ShouldLeaveDefaultsIfFieldAbsent()
    {
        var dict = new Dictionary<string, object> { { "born", 1977 } };
        var record = TestRecord.Create(["Person"], [dict]);
        var person = RecordObjectMapping.Map(record, typeof(TestPerson)) as TestPerson;
        person.Should().NotBeNull();
        person!.Name.Should().Be("A. Test Name");
        person!.Born.Should().Be(1977);
    }

    private class Movie
    {
        [MappingSource("title")]
        public string Title { get; set; } = "";

        [MappingSource("released")]
        public int Released { get; set; }

        [MappingOptional]
        [MappingSource("tagline")]
        public string Tagline { get; set; }
    }

    private class Person
    {
        [MappingSource("name")]
        public string Name { get; set; } = "";

        [MappingSource("born")]
        public int? Born { get; set; }
    }

    private class ProducingCareer
    {
        [MappingSource("person")]
        public Person Producer { get; set; } = null!;

        [MappingSource("titles")]
        public List<string> MovieTitleIdeas { get; set; } = null!;

        [MappingSource("movies")]
        public List<Movie> HistoricalMovies { get; set; } = null!;

        [MappingSource("moviesDict")]
        public List<Movie> OtherMovies { get; set; } = null!;
    }

    [Fact]
    public void ShouldMapComplexObjects()
    {
        var person = new Node(
            0,
            new[] { "Person" },
            new Dictionary<string, object> { { "name", "Ron Grazer" }, { "born", 1956 } });

        var movie1 = new Node(
            0,
            new[] { "Movie" },
            new Dictionary<string, object>
            {
                { "title", "Forrest Gump" },
                { "released", 1994 },
                { "tagline", "Life is like a box of chocolates..." }
            });

        var movie2 = new Node(
            0,
            new[] { "Movie" },
            new Dictionary<string, object>
            {
                { "title", "Cast Away" },
                { "released", 2000 },
                { "tagline", "At the edge of the world, his journey begins." }
            });

        var movie3 = new Node(
            0,
            new[] { "Movie" },
            new Dictionary<string, object>
            {
                { "title", "The Green Mile" },
                { "released", 1999 },
                { "tagline", null }
            });

        var movieNodes = new List<INode> { movie1, movie2, movie3 };

        var stringList = new List<string> { "A Band Apart", "Amazing Squad", "Ten Men Named Ben" };

        var movie4 = new Dictionary<string, object>
        {
            { "title", "The Blind Venetian" },
            { "released", 2023 },
            { "tagline", "Read between the lines" }
        };

        var movie5 = new Dictionary<string, object>
        {
            { "title", "When The Night Ends" },
            { "released", 2022 },
            { "tagline", "Just when you thought it was safe to go to sleep" }
        };

        var moviesDict = new List<IReadOnlyDictionary<string, object>> { movie4, movie5 };

        var record = TestRecord.Create(
            ["person", "movies", "titles", "moviesDict"],
            [person, movieNodes, stringList, moviesDict]);

        var mappedObject = record.AsObject<ProducingCareer>();

        mappedObject.Producer.Name.Should().Be("Ron Grazer");
        mappedObject.Producer.Born.Should().Be(1956);

        mappedObject.MovieTitleIdeas.Should().BeEquivalentTo("A Band Apart", "Amazing Squad", "Ten Men Named Ben");

        mappedObject.HistoricalMovies.Should()
            .BeEquivalentTo(
                new Movie
                {
                    Title = "Forrest Gump", Released = 1994, Tagline = "Life is like a box of chocolates..."
                },
                new Movie
                {
                    Title = "Cast Away", Released = 2000, Tagline = "At the edge of the world, his journey begins."
                },
                new Movie { Title = "The Green Mile", Released = 1999, Tagline = null });

        mappedObject.OtherMovies.Should()
            .BeEquivalentTo(
                new Movie { Title = "The Blind Venetian", Released = 2023, Tagline = "Read between the lines" },
                new Movie
                {
                    Title = "When The Night Ends", Released = 2022,
                    Tagline = "Just when you thought it was safe to go to sleep"
                });
    }

    [Fact]
    public void ShouldMapAllRecords()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(["name"], ["Bob"]);
            var record2 = TestRecord.Create(["name", "born"], ["Alice", 1988]);
            var record3 = TestRecord.Create(["name", "born"], ["Eve", 1999]);

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name", "born"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync<SimpleTestPerson>()
            .Result.Should()
            .BeEquivalentTo(
                new TestPerson { Name = "Bob" },
                new TestPerson { Name = "Alice", Born = 1988 },
                new TestPerson { Name = "Eve", Born = 1999 });
    }

    [Fact]
    public void ShouldMapAllRecordsFromBlueprint()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(["name", "born"], ["Bob", 1977]);
            var record2 = TestRecord.Create(["name", "born"], ["Alice", 1988]);
            var record3 = TestRecord.Create(["name", "born"], ["Eve", 1999]);

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name", "born"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsFromBlueprintAsync(new { name = "", born = 0 })
            .Result.Should()
            .BeEquivalentTo(
                new { name = "Bob", born = 1977 },
                new { name = "Alice", born = 1988 },
                new { name = "Eve", born = 1999 });
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursor()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(["name"], ["Bob"]);
            var record2 = TestRecord.Create(["name", "born"], ["Alice", 1988]);
            var record3 = TestRecord.Create(["name", "born"], ["Eve", 1999]);

            var result = new List<IRecord> { record1, record2, record3 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync().ToListAsync<SimpleTestPerson>();
        result.Should()
            .BeEquivalentTo(
                new TestPerson { Name = "Bob" },
                new TestPerson { Name = "Alice", Born = 1988 },
                new TestPerson { Name = "Eve", Born = 1999 });
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursorWithBlueprint()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(["name", "born"], ["Bob", 1977]);
            var record2 = TestRecord.Create(["name", "born"], ["Alice", 1988]);
            var record3 = TestRecord.Create(["name", "born"], ["Eve", 1999]);

            var result = new List<IRecord> { record1, record2, record3 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var blueprint = new { name = "", born = 0 };
        var result = await GetRecordsAsync().ToListFromBlueprintAsync(blueprint);
        result.Should()
            .BeEquivalentTo(
                new { name = "Bob", born = 1977 },
                new { name = "Alice", born = 1988 },
                new { name = "Eve", born = 1999 });
    }

    [Fact]
    public async Task ShouldMapRecordsAsyncEnumerable()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(["name"], ["Bob"]);
            var record2 = TestRecord.Create(["name", "born"], ["Alice", 1988]);
            var record3 = TestRecord.Create(["name", "born"], ["Eve", 1999]);

            var result = new List<IRecord> { record1, record2, record3 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var people = new List<SimpleTestPerson>();
        await foreach (var person in GetRecordsAsync().AsObjectsAsync<SimpleTestPerson>())
        {
            people.Add(person);
        }

        people.Should()
            .BeEquivalentTo(
                new SimpleTestPerson { Name = "Bob" },
                new SimpleTestPerson { Name = "Alice", Born = 1988 },
                new SimpleTestPerson { Name = "Eve", Born = 1999 });
    }

    private class CarAndPainting
    {
        [MappingSource("car")]
        public Car Car { get; set; } = null!;

        [MappingSource("painting")]
        public Painting Painting { get; set; } = null!;
    }

    private class Painting
    {
        [MappingSource("painting.artist")]
        public string Artist { get; set; } = "";

        [MappingSource("painting.title")]
        public string Title { get; set; } = "";
    }

    private class Car
    {
        [MappingSource("car.make")]
        public string Make { get; set; } = "";

        [MappingSource("car.model")]
        public string Model { get; set; } = "";

        [MappingDefaultValue("unset")]
        [MappingSource("car.madeup")]
        public string MadeUp { get; set; }
    }

    [Fact]
    public void ShouldMapSubNodesWithAbsolutePaths()
    {
        var carNode = new Node(
            0,
            new[] { "Car" },
            new Dictionary<string, object>
            {
                { "make", "Tesla" },
                { "model", "Model 3" }
            });

        var paintingNode = new Node(
            0,
            new[] { "Painting" },
            new Dictionary<string, object>
            {
                { "artist", "Leonardo da Vinci" },
                { "title", "Mona Lisa" }
            });

        var carAndPaintingRecord = TestRecord.Create(["car", "painting"], [carNode, paintingNode]);

        var mappedObject = carAndPaintingRecord.AsObject<CarAndPainting>();

        mappedObject.Car.Make.Should().Be("Tesla");
        mappedObject.Car.Model.Should().Be("Model 3");
        mappedObject.Painting.Artist.Should().Be("Leonardo da Vinci");
        mappedObject.Painting.Title.Should().Be("Mona Lisa");
        mappedObject.Car.MadeUp.Should().Be("unset");
    }

    private class PersonWithoutBornSetter
    {
        [MappingSource("name")]
        public string Name { get; set; } = "";

        public int? Born { get; } = 1999; // no setter
    }

    [Fact]
    public void DefaultMapperShouldIgnorePropertiesWithoutSetter()
    {
        var record = TestRecord.Create(["name", "born"], ["Bob", 1977]);
        var person = record.AsObject<PersonWithoutBornSetter>();
        person.Name.Should().Be("Bob");
        person.Born.Should().Be(1999);
    }

    private class TestPersonWithoutBornMapped
    {
        [MappingSource("name")]
        public string Name { get; set; } = "A. Test Name";

        [MappingIgnored]
        public int? Born { get; set; } = 9999;
    }

    [Fact]
    public void ShouldIgnorePropertiesWithDoNotMapAttribute()
    {
        var record = TestRecord.Create(["name", "born"], ["Bob", 1977]);
        var person = record.AsObject<TestPersonWithoutBornMapped>();
        person.Name.Should().Be("Bob");
        person.Born.Should().Be(9999);
    }

    private class Book
    {
        [MappingSource("title")]
        public string Title { get; set; }
    }

    private class Author
    {
        [MappingSource("author.name")]
        public string Name { get; set; }

        [MappingSource("author.books")]
        public List<Book> Books { get; set; }
    }

    [Fact]
    public void ShouldMapEntitiesWithListsOfNodes()
    {
        var bookNodeList = new List<Node>
        {
            new(0, new[] { "Book" }, new Dictionary<string, object> { { "title", "The Green Man" } }),
            new(0, new[] { "Book" }, new Dictionary<string, object> { { "title", "The Thin End" } })
        };

        var authorNode = new Node(
            0,
            new[] { "Author" },
            new Dictionary<string, object> { { "name", "Kate Grenville" }, { "books", bookNodeList } });

        var record = TestRecord.Create(["author"], [authorNode]);

        var mappedObject = record.AsObject<Author>();

        mappedObject.Name.Should().Be("Kate Grenville");
        mappedObject.Books.Should().HaveCount(2);
        mappedObject.Books[0].Title.Should().Be("The Green Man");
        mappedObject.Books[1].Title.Should().Be("The Thin End");
    }

    private record Song(
        [MappingSource("recordingArtist")] string Artist,
        [MappingSource("title")] string Title,
        [MappingSource("year")] int Year);

    [Fact]
    public void ShouldMapToRecords()
    {
        var record = TestRecord.Create(
            ["recordingArtist", "title", "year"],
            ["The Beatles", "Yellow Submarine", 1966]);

        var song = record.AsObject<Song>();
        song.Artist.Should().Be("The Beatles");
        song.Title.Should().Be("Yellow Submarine");
        song.Year.Should().Be(1966);
    }

    [Fact]
    public void ShouldFailMappingToRecordsWithNulls()
    {
        var record = TestRecord.Create(
            ["recordingArtist", "title", "year"],
            ["The Beatles", null, 1966]);

        var act = () => record.AsObject<Song>();

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailMappingToRecordsWithMissingFields()
    {
        var record = TestRecord.Create(
            ["recordingArtist", "year"],
            ["The Beatles", 1966]);

        var act = () => record.AsObject<Song>();

        act.Should().Throw<MappingFailedException>();
    }

    private class ClassWithInitProperties
    {
        [MappingSource("name")]
        public string Name { get; init; } = "";

        [MappingSource("age")]
        public int Age { get; init; }
    }

    [Fact]
    public void ShouldMapToInitProperties()
    {
        var record = TestRecord.Create(["name", "age"], ["Bob", 1977]);
        var person = record.AsObject<ClassWithInitProperties>();
        person.Name.Should().Be("Bob");
        person.Age.Should().Be(1977);
    }

    private class ClassWithDefaultConstructor(string forename, int age)
    {
        public string Name => forename;
        public int Age => age;
    }

    [Fact]
    public void ShouldMapToDefaultConstructorParameters()
    {
        var record = TestRecord.Create(["forename", "age"], ["Bob", 1977]);
        var person = record.AsObject<ClassWithDefaultConstructor>();
        person.Name.Should().Be("Bob");
        person.Age.Should().Be(1977);
    }

    private class ClassWithDefaultConstructorWithAttributes([MappingSource("forename")] string name, int age)
    {
        public string Name => name;
        public int Age => age;
    }

    [Fact]
    public void ShouldMapToDefaultConstructorParametersWithAttributes()
    {
        var record = TestRecord.Create(["forename", "age"], ["Bob", 1977]);
        var person = record.AsObject<ClassWithDefaultConstructorWithAttributes>();
        person.Name.Should().Be("Bob");
        person.Age.Should().Be(1977);
    }

    [Fact]
    public void ShouldFindPropertiesInNodes()
    {
        var node = new Node(
            0,
            new[] { "Person" },
            new Dictionary<string, object> { { "name", "Bob" }, { "born", 1977 } });

        var record = TestRecord.Create(["person"], [node]);
        var person = record.AsObject<TestPerson>();
        person.Name.Should().Be("Bob");
        person.Born.Should().Be(1977);
    }

    [Fact]
    public void ShouldFindPropertiesInDictionaries()
    {
        var dict = new Dictionary<string, object> { { "name", "Bob" }, { "born", 1977 } };
        var record = TestRecord.Create(["person"], [dict]);
        var person = record.AsObject<TestPerson>();
        person.Name.Should().Be("Bob");
        person.Born.Should().Be(1977);
    }

    [Fact]
    public void ShouldMapEntityToObjectThroughAsRecord()
    {
        var node = new Node(
            0,
            new[] { "Person" },
            new Dictionary<string, object> { { "name", "Bob" }, { "born", 1977 } });

        var record = node.AsRecord();

        var person = record.AsObject<SimpleTestPerson>();
        person.Name.Should().Be("Bob");
        person.Born.Should().Be(1977);
    }

    [Fact]
    public void ShouldMapRecordToAnonymousTypeWithBlueprint()
    {
        var record = TestRecord.Create(["x", "y"], [69, "test"]);

        var result = record.AsObject((int x, string y) => new { x, y });

        result.x.Should().Be(69);
        result.y.Should().Be("test");
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithLambda()
    {
        var record = TestRecord.Create(("x", 69), ("y", "test"));

        var result = record.AsObject((int x, string y) => new { x, y });

        result.x.Should().Be(69);
        result.y.Should().Be("test");
    }

    private record TestXY(int X, string Y)
    {
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithTypedLambda()
    {
        var record = TestRecord.Create(("x", 69), ("y", "test"));

        var result = record.AsObject((int x, string y) => new TestXY(x, y));

        result.X.Should().Be(69);
        result.Y.Should().Be("test");
    }

    [Fact]
    public void AsObject_ShouldMapRecordToObjectType()
    {
        var record = TestRecord.Create(("Name", "Alice"), ("Born", 1988));
        var expectedPerson = new { Name = "Alice", Born = 1988 };

        var result = record.AsObject(expectedPerson.GetType());

        result.Should().BeOfType(expectedPerson.GetType());
        result.Should().BeEquivalentTo(expectedPerson);
    }
}
