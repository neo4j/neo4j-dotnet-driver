// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using Neo4j.Driver.Preview.Mapping;
using Xunit;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests.Mapping
{
    public class RecordMappingTests
    {
        private class TestPerson
        {
            public string Name { get; set; } = "A. Test Name";
            public int? Born { get; set; }
            public List<string> Hobbies { get; set; } = null!;
        }

        [Fact]
        public void ShouldMapPrimitives()
        {
            var record = new Record(new[] { "name", "born" }, new object[] { "Bob", 1977 });
            var person = record.AsObject<TestPerson>();
            person.Name.Should().Be("Bob");
            person.Born.Should().Be(1977);
        }

        [Fact]
        public void ShouldMapList()
        {
            var record = new Record(new[] { "hobbies" }, new object[] { new List<string> { "Coding", "Swimming" } });
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
            var record = new Record(new[] { "Person" }, new object[] { dict });
            var person = record.AsObject<PersonInDict>();
            person.Name.Should().Be("Dani");
            person.Born.Should().Be(1977);
        }

        [Fact]
        public void ShouldLeaveDefaultsIfFieldAbsent()
        {
            var record = new Record(new[] { "born" }, new object[] { 1977 });
            var person = record.AsObject<TestPerson>();
            person.Name.Should().Be("A. Test Name");
            person.Born.Should().Be(1977);
        }

        private class Movie
        {
            public string Title { get; set; } = "";
            public int Released { get; set; }
            public string? Tagline { get; set; }
        }

        private class Person
        {
            public string Name { get; set; } = "";
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

            var record = new Record(
                new[] { "person", "movies", "titles", "moviesDict" },
                new object[] { person, movieNodes, stringList, moviesDict });

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
                var record1 = new Record(new[] { "name", }, new object[] { "Bob", });
                var record2 = new Record(new[] { "name", "born" }, new object[] { "Alice", 1988 });
                var record3 = new Record(new[] { "name", "born" }, new object[] { "Eve", 1999 });

                var result = new EagerResult<IReadOnlyList<IRecord>>(
                    new List<IRecord> { record1, record2, record3 },
                    null,
                    new[] { "name", "born" });

                return Task.FromResult(result);
            }

            GetRecordsAsync()
                .AsObjectsAsync<TestPerson>()
                .Result.Should()
                .BeEquivalentTo(
                    new TestPerson { Name = "Bob" },
                    new TestPerson { Name = "Alice", Born = 1988 },
                    new TestPerson { Name = "Eve", Born = 1999 });
        }

        [Fact]
        public void ShouldMapAllRecordsFromCursor()
        {
            async IAsyncEnumerable<IRecord> GetRecordsAsync()
            {
                var record1 = new Record(new[] { "name", }, new object[] { "Bob", });
                var record2 = new Record(new[] { "name", "born" }, new object[] { "Alice", 1988 });
                var record3 = new Record(new[] { "name", "born" }, new object[] { "Eve", 1999 });

                var result = new List<IRecord> { record1, record2, record3 };

                foreach (var record in result)
                {
                    await Task.Yield();
                    yield return record;
                }
            }

            GetRecordsAsync()
                .ToListAsync<TestPerson>()
                .Result.Should()
                .BeEquivalentTo(
                    new TestPerson { Name = "Bob" },
                    new TestPerson { Name = "Alice", Born = 1988 },
                    new TestPerson { Name = "Eve", Born = 1999 });
        }

        private class CarAndPainting
        {
            public Car Car { get; set; } = null!;
            public Painting Painting { get; set; } = null!;
        }

        private class Painting
        {
            public string Artist { get; set; } = "";
            public string Title { get; set; } = "";
        }

        private class Car
        {
            [MappingSource("car.make")]
            public string Make { get; set; } = "";

            [MappingSource("model")]
            public string Model { get; set; } = "";

            [MappingSource("car.madeup")]
            public string MadeUp { get; set; } = "unset";
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

            var carAndPaintingRecord = new Record(new[] { "car", "painting" }, new object[] { carNode, paintingNode });

            var mappedObject = carAndPaintingRecord.AsObject<CarAndPainting>();

            mappedObject.Car.Make.Should().Be("Tesla");
            mappedObject.Car.Model.Should().Be("Model 3");
            mappedObject.Painting.Artist.Should().Be("Leonardo da Vinci");
            mappedObject.Painting.Title.Should().Be("Mona Lisa");
            mappedObject.Car.MadeUp.Should().Be("unset");
        }

        private class PersonWithoutBornSetter
        {
            public string Name { get; set; } = "";
            public int? Born { get; } = 1999; // no setter
        }

        [Fact]
        public void DefaultMapperShouldIgnorePropertiesWithoutSetter()
        {
            var record = new Record(new[] { "name", "born" }, new object[] { "Bob", 1977 });
            var person = record.AsObject<PersonWithoutBornSetter>();
            person.Name.Should().Be("Bob");
            person.Born.Should().Be(1999);
        }

        private class TestPersonWithoutBornMapped
        {
            public string Name { get; set; } = "A. Test Name";

            [DoNotMap]
            public int? Born { get; set; } = 9999;
        }

        [Fact]
        public void ShouldIgnorePropertiesWithDoNotMapAttribute()
        {
            var record = new Record(new[] { "name", "born" }, new object[] { "Bob", 1977 });
            var person = record.AsObject<TestPersonWithoutBornMapped>();
            person.Name.Should().Be("Bob");
            person.Born.Should().Be(9999);
        }

        private class Book
        {
            public string Title { get; set; }
        }

        private class Author
        {
            public string Name { get; set; }
            public List<Book> Books { get; set; }
        }

        [Fact]
        public void ShouldMapEntitiesWithListsOfNodes()
        {
            var bookNodeList = new List<Node>
            {
                new Node(0, new[] { "Book" }, new Dictionary<string, object> { { "title", "The Green Man" } }),
                new Node(0, new[] { "Book" }, new Dictionary<string, object> { { "title", "The Thin End" } })
            };

            var authorNode = new Node(
                0,
                new[] { "Author" },
                new Dictionary<string, object> { { "name", "Kate Grenville" }, { "books", bookNodeList } });

            var record = new Record(new[] { "author" }, new object[] { authorNode });

            var mappedObject = record.AsObject<Author>();

            mappedObject.Name.Should().Be("Kate Grenville");
            mappedObject.Books.Should().HaveCount(2);
            mappedObject.Books[0].Title.Should().Be("The Green Man");
            mappedObject.Books[1].Title.Should().Be("The Thin End");
        }
    }
}
