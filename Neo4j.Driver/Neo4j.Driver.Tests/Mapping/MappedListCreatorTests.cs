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
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
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

    [Fact]
    public void ShouldCreateListOfMappedObjectsFromDictionaries()
    {
        var list = new List<object>
            { new Dictionary<string, object>(), new Dictionary<string, object>(), new Dictionary<string, object>() };

        var record = Mock.Of<IRecord>();
        var people = new List<Person> { new("Alan", 99), new("Basil", 999), new("David", 9999) };

        _mocker.GetMock<IRecordObjectMapping>()
            .SetupSequence(x => x.Map(It.IsAny<DictAsRecord>(), typeof(Person)))
            .ReturnsSequence(people);

        var subject = _mocker.CreateInstance<MappedListCreator>(true);
        var mappedList = subject.CreateMappedList(list, typeof(List<Person>), record);

        mappedList.Should().BeEquivalentTo(people);
    }

    [Fact]
    public void ShouldCreateListOfMappedObjectsFromNodes()
    {
        var mockEntity = new Mock<IEntity>();
        mockEntity.Setup(x => x.Properties).Returns(new Dictionary<string, object>());
        var list = new List<IEntity> { mockEntity.Object, mockEntity.Object, mockEntity.Object };
        var record = Mock.Of<IRecord>();
        var people = new List<Person> { new("Alan", 99), new("Basil", 999), new("David", 9999) };

        _mocker.GetMock<IRecordObjectMapping>()
            .SetupSequence(x => x.Map(It.IsAny<DictAsRecord>(), typeof(Person)))
            .ReturnsSequence(people);

        var subject = _mocker.CreateInstance<MappedListCreator>(true);
        var mappedList = subject.CreateMappedList(list, typeof(List<Person>), record);

        mappedList.Should().BeEquivalentTo(people);
    }

    private class Person
    {
        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public string Name { get; set; }
        public int Age { get; set; }
    }
}
