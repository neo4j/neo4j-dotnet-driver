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
    public void ShouldCreateListOfMappedObjectsFromDictionaries()
    {
        var list = new List<object>
            { new Dictionary<string, object>(), new Dictionary<string, object>(), new Dictionary<string, object>() };

        var record = Mock.Of<IRecord>();

        _mocker.GetMock<IRecordObjectMapping>()
            .SetupSequence(x => x.Map(It.IsAny<DictAsRecord>(), typeof(Person)))
            .Returns(new Person { Name = "Alan", Age = 99 })
            .Returns(new Person { Name = "Basil", Age = 999 })
            .Returns(new Person { Name = "David", Age = 9999 });

        var subject = _mocker.CreateInstance<MappedListCreator>(true);

        var mappedList = subject.CreateMappedList(list, typeof(List<Person>), record);

        mappedList.Should()
            .BeEquivalentTo(
                new Person { Name = "Alan", Age = 99 },
                new Person { Name = "Basil", Age = 999 },
                new Person { Name = "David", Age = 9999 });
    }

    [Fact]
    public void ShouldCreateListOfMappedObjectsFromNodes()
    {
        var list = new List<IEntity> { Mock.Of<IEntity>(), Mock.Of<IEntity>(), Mock.Of<IEntity>() };
        var record = Mock.Of<IRecord>();

        _mocker.GetMock<IRecordObjectMapping>()
            .SetupSequence(x => x.Map(It.IsAny<DictAsRecord>(), typeof(Person)))
            .Returns(new Person { Name = "Alan", Age = 99 })
            .Returns(new Person { Name = "Basil", Age = 999 })
            .Returns(new Person { Name = "David", Age = 9999 });

        var subject = _mocker.CreateInstance<MappedListCreator>(true);

        var mappedList = subject.CreateMappedList(list, typeof(List<Person>), record);

        mappedList.Should()
            .BeEquivalentTo(
                new Person { Name = "Alan", Age = 99 },
                new Person { Name = "Basil", Age = 999 },
                new Person { Name = "David", Age = 9999 });
    }
}
