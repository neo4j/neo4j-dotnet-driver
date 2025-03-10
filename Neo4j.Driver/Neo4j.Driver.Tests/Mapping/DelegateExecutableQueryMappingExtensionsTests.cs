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
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class DelegateExecutableQueryMappingExtensionsTests
{
    [Fact]
    public void ShouldMapAllRecordsWith_01_Field()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(("name", "Bob"));
            var record2 = TestRecord.Create(("name", "Alice"));
            var record3 = TestRecord.Create(("name", "Eve"));

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync((string name) => new { name })
            .Result.Should()
            .BeEquivalentTo(
            [
                new { name = "Bob" },
                new { name = "Alice" },
                new { name = "Eve" }
            ]);
    }

    [Fact]
    public void ShouldMapAllRecordsWith_02_Fields()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(("name", "Bob"), ("age", 30));
            var record2 = TestRecord.Create(("name", "Alice"), ("age", 25));
            var record3 = TestRecord.Create(("name", "Eve"), ("age", 35));

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name", "age"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync((string name, int age) => new { name, age })
            .Result.Should()
            .BeEquivalentTo(
            [
                new { name = "Bob", age = 30 },
                new { name = "Alice", age = 25 },
                new { name = "Eve", age = 35 }
            ]);
    }

    [Fact]
    public void ShouldMapAllRecordsWith_03_Fields()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(("name", "Bob"), ("age", 30), ("city", "New York"));
            var record2 = TestRecord.Create(("name", "Alice"), ("age", 25), ("city", "Los Angeles"));

            var record3 = TestRecord.Create(("name", "Eve"), ("age", 35), ("city", "Chicago"));

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name", "age", "city"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync((string name, int age, string city) => new { name, age, city })
            .Result.Should()
            .BeEquivalentTo(
            [
                new { name = "Bob", age = 30, city = "New York" },
                new { name = "Alice", age = 25, city = "Los Angeles" },
                new { name = "Eve", age = 35, city = "Chicago" }
            ]);
    }

    [Fact]
    public void ShouldMapAllRecordsWith_04_Fields()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "New York"),
                ("country", "USA"));

            var record2 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "Los Angeles"),
                ("country", "USA"));

            var record3 = TestRecord.Create(
                ("name", "Eve"),
                ("age", 35),
                ("city", "Chicago"),
                ("country", "USA"));

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name", "age", "city", "country"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync((string name, int age, string city, string country) => new { name, age, city, country })
            .Result.Should()
            .BeEquivalentTo(
            [
                new { name = "Bob", age = 30, city = "New York", country = "USA" },
                new { name = "Alice", age = 25, city = "Los Angeles", country = "USA" },
                new { name = "Eve", age = 35, city = "Chicago", country = "USA" }
            ]);
    }

    [Fact]
    public void ShouldMapAllRecordsWith_05_Fields()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"));

            var record2 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"));

            var record3 = TestRecord.Create(
                ("name", "Eve"),
                ("age", 35),
                ("city", "Chicago"),
                ("country", "USA"),
                ("job", "Teacher"));

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name", "age", "city", "country", "job"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync(
                (string name, int age, string city, string country, string job) =>
                    new { name, age, city, country, job })
            .Result.Should()
            .BeEquivalentTo(
            [
                new { name = "Bob", age = 30, city = "New York", country = "USA", job = "Engineer" },
                new { name = "Alice", age = 25, city = "Los Angeles", country = "USA", job = "Doctor" },
                new { name = "Eve", age = 35, city = "Chicago", country = "USA", job = "Teacher" }
            ]);
    }

    [Fact]
    public void ShouldMapAllRecordsWith_06_Fields()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"),
                ("gender", "Male"));

            var record2 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"),
                ("gender", "Female"));

            var record3 = TestRecord.Create(
                ("name", "Eve"),
                ("age", 35),
                ("city", "Chicago"),
                ("country", "USA"),
                ("job", "Teacher"),
                ("gender", "Female"));

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name", "age", "city", "country", "job", "gender"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync(
                (string name, int age, string city, string country, string job, string gender) =>
                    new { name, age, city, country, job, gender })
            .Result.Should()
            .BeEquivalentTo(
            [
                new { name = "Bob", age = 30, city = "New York", country = "USA", job = "Engineer", gender = "Male" },
                new
                {
                    name = "Alice", age = 25, city = "Los Angeles", country = "USA", job = "Doctor", gender = "Female"
                },
                new { name = "Eve", age = 35, city = "Chicago", country = "USA", job = "Teacher", gender = "Female" }
            ]);
    }

    [Fact]
    public void ShouldMapAllRecordsWith_07_Fields()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"),
                ("gender", "Male"),
                ("maritalStatus", "Single"));

            var record2 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"),
                ("gender", "Female"),
                ("maritalStatus", "Married"));

            var record3 = TestRecord.Create(
                ("name", "Eve"),
                ("age", 35),
                ("city", "Chicago"),
                ("country", "USA"),
                ("job", "Teacher"),
                ("gender", "Female"),
                ("maritalStatus", "Divorced"));

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name", "age", "city", "country", "job", "gender", "maritalStatus"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync(
                (string name, int age, string city, string country, string job, string gender, string maritalStatus) =>
                    new { name, age, city, country, job, gender, maritalStatus })
            .Result.Should()
            .BeEquivalentTo(
            [
                new
                {
                    name = "Bob", age = 30, city = "New York", country = "USA", job = "Engineer", gender = "Male",
                    maritalStatus = "Single"
                },
                new
                {
                    name = "Alice", age = 25, city = "Los Angeles", country = "USA", job = "Doctor", gender = "Female",
                    maritalStatus = "Married"
                },
                new
                {
                    name = "Eve", age = 35, city = "Chicago", country = "USA", job = "Teacher", gender = "Female",
                    maritalStatus = "Divorced"
                }
            ]);
    }

    [Fact]
    public void ShouldMapAllRecordsWith_08_Fields()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"),
                ("gender", "Male"),
                ("maritalStatus", "Single"),
                ("children", 2));

            var record2 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"),
                ("gender", "Female"),
                ("maritalStatus", "Married"),
                ("children", 0));

            var record3 = TestRecord.Create(
                ("name", "Eve"),
                ("age", 35),
                ("city", "Chicago"),
                ("country", "USA"),
                ("job", "Teacher"),
                ("gender", "Female"),
                ("maritalStatus", "Divorced"),
                ("children", 1));

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name", "age", "city", "country", "job", "gender", "maritalStatus", "children"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync(
                (
                    string name,
                    int age,
                    string city,
                    string country,
                    string job,
                    string gender,
                    string maritalStatus,
                    int children) => new { name, age, city, country, job, gender, maritalStatus, children })
            .Result.Should()
            .BeEquivalentTo(
            [
                new
                {
                    name = "Bob", age = 30, city = "New York", country = "USA", job = "Engineer", gender = "Male",
                    maritalStatus = "Single", children = 2
                },
                new
                {
                    name = "Alice", age = 25, city = "Los Angeles", country = "USA", job = "Doctor", gender = "Female",
                    maritalStatus = "Married", children = 0
                },
                new
                {
                    name = "Eve", age = 35, city = "Chicago", country = "USA", job = "Teacher", gender = "Female",
                    maritalStatus = "Divorced", children = 1
                }
            ]);
    }

    [Fact]
    public void ShouldMapAllRecordsWith_09_Fields()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"),
                ("gender", "Male"),
                ("maritalStatus", "Single"),
                ("children", 2),
                ("education", "Bachelor's"));

            var record2 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"),
                ("gender", "Female"),
                ("maritalStatus", "Married"),
                ("children", 0),
                ("education", "Master's"));

            var record3 = TestRecord.Create(
                ("name", "Eve"),
                ("age", 35),
                ("city", "Chicago"),
                ("country", "USA"),
                ("job", "Teacher"),
                ("gender", "Female"),
                ("maritalStatus", "Divorced"),
                ("children", 1),
                ("education", "PhD"));

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                ["name", "age", "city", "country", "job", "gender", "maritalStatus", "children", "education"]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync(
                (
                    string name,
                    int age,
                    string city,
                    string country,
                    string job,
                    string gender,
                    string maritalStatus,
                    int children,
                    string education) => new
                    { name, age, city, country, job, gender, maritalStatus, children, education })
            .Result.Should()
            .BeEquivalentTo(
            [
                new
                {
                    name = "Bob", age = 30, city = "New York", country = "USA", job = "Engineer", gender = "Male",
                    maritalStatus = "Single", children = 2, education = "Bachelor's"
                },
                new
                {
                    name = "Alice", age = 25, city = "Los Angeles", country = "USA", job = "Doctor", gender = "Female",
                    maritalStatus = "Married", children = 0, education = "Master's"
                },
                new
                {
                    name = "Eve", age = 35, city = "Chicago", country = "USA", job = "Teacher", gender = "Female",
                    maritalStatus = "Divorced", children = 1, education = "PhD"
                }
            ]);
    }

    [Fact]
    public void ShouldMapAllRecordsWith_10_Fields()
    {
        Task<EagerResult<IReadOnlyList<IRecord>>> GetRecordsAsync()
        {
            var record1 = TestRecord.Create(
                ("name", "Bob"),
                ("age", 30),
                ("city", "New York"),
                ("country", "USA"),
                ("job", "Engineer"),
                ("gender", "Male"),
                ("maritalStatus", "Single"),
                ("children", 2),
                ("education", "Bachelor's"),
                ("income", 70000));

            var record2 = TestRecord.Create(
                ("name", "Alice"),
                ("age", 25),
                ("city", "Los Angeles"),
                ("country", "USA"),
                ("job", "Doctor"),
                ("gender", "Female"),
                ("maritalStatus", "Married"),
                ("children", 0),
                ("education", "Master's"),
                ("income", 80000));

            var record3 = TestRecord.Create(
                ("name", "Eve"),
                ("age", 35),
                ("city", "Chicago"),
                ("country", "USA"),
                ("job", "Teacher"),
                ("gender", "Female"),
                ("maritalStatus", "Divorced"),
                ("children", 1),
                ("education", "PhD"),
                ("income", 60000));

            var result = new EagerResult<IReadOnlyList<IRecord>>(
                new List<IRecord> { record1, record2, record3 },
                null,
                [
                    "name", "age", "city", "country", "job", "gender", "maritalStatus", "children", "education",
                    "income"
                ]);

            return Task.FromResult(result);
        }

        GetRecordsAsync()
            .AsObjectsAsync(
                (
                    string name,
                    int age,
                    string city,
                    string country,
                    string job,
                    string gender,
                    string maritalStatus,
                    int children,
                    string education,
                    int income) => new
                    { name, age, city, country, job, gender, maritalStatus, children, education, income })
            .Result.Should()
            .BeEquivalentTo(
            [
                new
                {
                    name = "Bob", age = 30, city = "New York", country = "USA", job = "Engineer", gender = "Male",
                    maritalStatus = "Single", children = 2, education = "Bachelor's", income = 70000
                },
                new
                {
                    name = "Alice", age = 25, city = "Los Angeles", country = "USA", job = "Doctor", gender = "Female",
                    maritalStatus = "Married", children = 0, education = "Master's", income = 80000
                },
                new
                {
                    name = "Eve", age = 35, city = "Chicago", country = "USA", job = "Teacher", gender = "Female",
                    maritalStatus = "Divorced", children = 1, education = "PhD", income = 60000
                }
            ]);
    }
}
