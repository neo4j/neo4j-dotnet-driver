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

using System;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Preview.Mapping;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class MappableValueProviderTests
{
    private AutoMocker _mocker = new();

    [Fact]
    public void ShouldReturnNullWhenFieldNotFound()
    {
        var entityMappingInfo = new EntityMappingInfo("field-name", EntityMappingSource.Property);
        _mocker.GetMock<IMappingSourceDelegateBuilder>()
            .Setup(x => x.GetMappingDelegate(entityMappingInfo))
            .Returns(Getter);

        var subject = _mocker.CreateInstance<MappableValueProvider>(true);

        var result = subject.GetConvertedValue(
            Mock.Of<IRecord>(),
            entityMappingInfo,
            typeof(object));

        result.Should().BeNull();

        return;

        bool Getter(IRecord record, out object value)
        {
            value = null;
            return false;
        }
    }
}
