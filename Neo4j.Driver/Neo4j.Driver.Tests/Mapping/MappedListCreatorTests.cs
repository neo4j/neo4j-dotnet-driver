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
using System.Linq;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Preview.Mapping;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class MappedListCreatorTests
{
    private readonly AutoMocker _mocker = new();

    [Fact]
    public void ShouldCreateList()
    {
        var creator = new MappedListCreator();
        var list = creator.CreateMappedList(Enumerable.Range(0, 10), typeof(List<string>), null);
        list.Should().BeEquivalentTo(Enumerable.Range(0, 10).Select(x => x.ToString()));
    }

    private class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    [Fact]
    public void ShouldCreateListOfMappedObjectsFromEntities()
    {
        var dave = new Dictionary<string, object> { { "Name", "Dave" }, { "Age", 42 } };
        var barney = new Dictionary<string, object> { { "Name", "Barney" }, { "Age", 38 } };
        var ziggy = new Dictionary<string, object> { { "Name", "Ziggy" }, { "Age", 2 } };
        var list = new List<object> { dave, barney, ziggy };

        var record = Mock.Of<IRecord>();

        var firstPerson = new Person { Name = "Alan", Age = 99 };
        var secondPerson = new Person { Name = "Basil", Age = 999 };
        var thirdPerson = new Person { Name = "Michael", Age = 9999 };

        _mocker.GetMock<IRecordObjectMapping>()
            .SetupSequence(x => x.Map(It.IsAny<DictAsRecord>(), typeof(Person)))
            .Returns(firstPerson)
            .Returns(secondPerson)
            .Returns(thirdPerson);

        var subject = _mocker.CreateInstance<MappedListCreator>(true);
        var mappedList = subject.CreateMappedList(list, typeof(List<Person>), record);

        mappedList.Should().BeEquivalentTo(firstPerson, secondPerson, thirdPerson);
    }

    [Fact]
    public void ShouldCreateListOfMappedObjectsFromDictionaries()
    {
        var dave = new Node(
            0,
            new[] { "person" },
            new Dictionary<string, object> { { "name", "Dave" }, { "age", 42 } });

        var barney = new Node(
            1,
            new[] { "person" },
            new Dictionary<string, object> { { "name", "Barney" }, { "age", 38 } });

        var ziggy = new Node(
            2,
            new[] { "person" },
            new Dictionary<string, object> { { "name", "Ziggy" }, { "age", 2 } });

        var list = new List<object> { dave, barney, ziggy };

        var record = Mock.Of<IRecord>();

        var firstPerson = new Person { Name = "Alan", Age = 99 };
        var secondPerson = new Person { Name = "Basil", Age = 999 };
        var thirdPerson = new Person { Name = "Michael", Age = 9999 };

        _mocker.GetMock<IRecordObjectMapping>()
            .SetupSequence(x => x.Map(It.IsAny<DictAsRecord>(), typeof(Person)))
            .Returns(firstPerson)
            .Returns(secondPerson)
            .Returns(thirdPerson);

        var subject = _mocker.CreateInstance<MappedListCreator>(true);
        var mappedList = subject.CreateMappedList(list, typeof(List<Person>), record);

        mappedList.Should().BeEquivalentTo(firstPerson, secondPerson, thirdPerson);
    }
}
