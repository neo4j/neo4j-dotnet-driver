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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class AsyncEnumerableExtensionsTests
{
    [Fact]
    public async Task ShouldMapAllRecordsFromCursor_01_Property()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(("name", "Alice"));
            var record2 = TestRecord.Create(("name", "Eve"));

            var result = new List<IRecord> { record1, record2 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync().ToListAsync((string name) => new { name });
        result.Should()
            .BeEquivalentTo(
            [
                new { name = "Alice" },
                new { name = "Eve" }
            ]);
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursor_02_Properties()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(("name", "Alice"), ("age", 25));
            var record2 = TestRecord.Create(("name", "Bob"), ("age", 30));

            var result = new List<IRecord> { record1, record2 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync().ToListAsync((string name, int age) => new { name, age });
        result.Should()
            .BeEquivalentTo(
                new { name = "Alice", age = 25 },
                new { name = "Bob", age = 30 });
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursor_03_Properties()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(("name", "Alice"), ("age", 25), ("city", "New York"));
            var record2 = TestRecord.Create(("name", "Bob"), ("age", 30), ("city", "Los Angeles"));

            var result = new List<IRecord> { record1, record2 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync()
            .ToListAsync((string name, int age, string city) => new { name, age, city });

        result.Should()
            .BeEquivalentTo(
                new { name = "Alice", age = 25, city = "New York" },
                new { name = "Bob", age = 30, city = "Los Angeles" });
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursor_04_Properties()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(("name", "Alice"), ("age", 25), ("city", "New York"), ("country", "USA"));
            var record2 = TestRecord.Create(("name", "Bob"), ("age", 30), ("city", "Los Angeles"), ("country", "USA"));

            var result = new List<IRecord> { record1, record2 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync()
            .ToListAsync((string name, int age, string city, string country) => new { name, age, city, country });

        result.Should()
            .BeEquivalentTo(
                new { name = "Alice", age = 25, city = "New York", country = "USA" },
                new { name = "Bob", age = 30, city = "Los Angeles", country = "USA" });
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursor_05_Properties()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"));

            var record2 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"));

            var result = new List<IRecord> { record1, record2 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync()
            .ToListAsync(
                (string name, int age, string city, string country, string job) =>
                    new { name, age, city, country, job });

        result.Should()
            .BeEquivalentTo(
                new { name = "Alice", age = 25, city = "New York", country = "USA", job = "Engineer" },
                new { name = "Bob", age = 30, city = "Los Angeles", country = "USA", job = "Doctor" });
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursor_06_Properties()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"),
                ("hobby", "Reading"));

            var record2 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"),
                ("hobby", "Swimming"));

            var result = new List<IRecord> { record1, record2 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync()
            .ToListAsync(
                (string name, int age, string city, string country, string job, string hobby) =>
                    new { name, age, city, country, job, hobby });

        result.Should()
            .BeEquivalentTo(
                new
                {
                    name = "Alice", age = 25, city = "New York", country = "USA", job = "Engineer", hobby = "Reading"
                },
                new
                {
                    name = "Bob", age = 30, city = "Los Angeles", country = "USA", job = "Doctor", hobby = "Swimming"
                });
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursor_07_Properties()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"),
                ("hobby", "Reading"),
                ("pet", "Dog"));

            var record2 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"),
                ("hobby", "Swimming"),
                ("pet", "Cat"));

            var result = new List<IRecord> { record1, record2 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync()
            .ToListAsync(
                (string name, int age, string city, string country, string job, string hobby, string pet) =>
                    new { name, age, city, country, job, hobby, pet });

        result.Should()
            .BeEquivalentTo(
                new
                {
                    name = "Alice", age = 25, city = "New York", country = "USA", job = "Engineer", hobby = "Reading",
                    pet = "Dog"
                },
                new
                {
                    name = "Bob", age = 30, city = "Los Angeles", country = "USA", job = "Doctor", hobby = "Swimming",
                    pet = "Cat"
                });
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursor_08_Properties()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"),
                ("hobby", "Reading"),
                ("pet", "Dog"),
                ("car", "Tesla"));

            var record2 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"),
                ("hobby", "Swimming"),
                ("pet", "Cat"),
                ("car", "BMW"));

            var result = new List<IRecord> { record1, record2 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync()
            .ToListAsync(
                (string name, int age, string city, string country, string job, string hobby, string pet, string car) =>
                    new { name, age, city, country, job, hobby, pet, car });

        result.Should()
            .BeEquivalentTo(
                new
                {
                    name = "Alice", age = 25, city = "New York", country = "USA", job = "Engineer", hobby = "Reading",
                    pet = "Dog", car = "Tesla"
                },
                new
                {
                    name = "Bob", age = 30, city = "Los Angeles", country = "USA", job = "Doctor", hobby = "Swimming",
                    pet = "Cat", car = "BMW"
                });
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursor_09_Properties()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"),
                ("hobby", "Reading"),
                ("pet", "Dog"),
                ("car", "Tesla"),
                ("food", "Pizza"));

            var record2 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"),
                ("hobby", "Swimming"),
                ("pet", "Cat"),
                ("car", "BMW"),
                ("food", "Burger"));

            var result = new List<IRecord> { record1, record2 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync()
            .ToListAsync(
                (
                    string name,
                    int age,
                    string city,
                    string country,
                    string job,
                    string hobby,
                    string pet,
                    string car,
                    string food) => new { name, age, city, country, job, hobby, pet, car, food });

        result.Should()
            .BeEquivalentTo(
                new
                {
                    name = "Alice", age = 25, city = "New York", country = "USA", job = "Engineer", hobby = "Reading",
                    pet = "Dog", car = "Tesla", food = "Pizza"
                },
                new
                {
                    name = "Bob", age = 30, city = "Los Angeles", country = "USA", job = "Doctor", hobby = "Swimming",
                    pet = "Cat", car = "BMW", food = "Burger"
                });
    }

    [Fact]
    public async Task ShouldMapAllRecordsFromCursor_10_Properties()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"),
                ("hobby", "Reading"),
                ("pet", "Dog"),
                ("car", "Tesla"),
                ("food", "Pizza"),
                ("sport", "Tennis"));

            var record2 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"),
                ("hobby", "Swimming"),
                ("pet", "Cat"),
                ("car", "BMW"),
                ("food", "Burger"),
                ("sport", "Football"));

            var result = new List<IRecord> { record1, record2 };

            foreach (var record in result)
            {
                await Task.Yield();
                yield return record;
            }
        }

        var result = await GetRecordsAsync()
            .ToListAsync(
                (
                    string name,
                    int age,
                    string city,
                    string country,
                    string job,
                    string hobby,
                    string pet,
                    string car,
                    string food,
                    string sport) => new { name, age, city, country, job, hobby, pet, car, food, sport });

        result.Should()
            .BeEquivalentTo(
                new
                {
                    name = "Alice", age = 25, city = "New York", country = "USA", job = "Engineer", hobby = "Reading",
                    pet = "Dog", car = "Tesla", food = "Pizza", sport = "Tennis"
                },
                new
                {
                    name = "Bob", age = 30, city = "Los Angeles", country = "USA", job = "Doctor", hobby = "Swimming",
                    pet = "Cat", car = "BMW", food = "Burger", sport = "Football"
                });
    }

    [Fact]
    public async Task AsObjectsFromBlueprintAsync_ShouldMapRecordsToObjects()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "New York"));

            var record2 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "Los Angeles"));

            await Task.Yield();
            yield return record1;
            yield return record2;
        }

        var result = await GetRecordsAsync()
            .AsObjectsFromBlueprintAsync(new { name = string.Empty, age = 0, city = string.Empty })
            .ToArrayAsync();

        result.Should()
            .BeEquivalentTo(
            [
                new { name = "Alice", age = 25, city = "New York" },
                new { name = "Bob", age = 30, city = "Los Angeles" }
            ]);
    }

    [Fact]
    public async Task AsObjectsFromBlueprintAsync_ShouldHandleEmptyEnumerable()
    {
        async IAsyncEnumerable<IRecord> GetRecordsAsync()
        {
            await Task.Yield();
            yield break;
        }

        var result = await GetRecordsAsync()
            .AsObjectsFromBlueprintAsync(new { name = string.Empty, age = 0, city = string.Empty })
            .ToArrayAsync();

        result.Should().BeEmpty();
    }
}
