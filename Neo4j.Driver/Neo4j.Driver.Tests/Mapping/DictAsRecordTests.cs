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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Preview.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class DictAsRecordTests
{
    [Fact]
    public void ShouldUsePropertiesOfDict()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        subject.Record.Should().BeSameAs(originalRecord);
        subject.Keys.Should().BeEquivalentTo(dict.Keys);
        subject.Values.Should().BeEquivalentTo(dict);
        subject[0].Should().Be("value1");
        subject[1].Should().Be("value2");
        subject["key1"].Should().Be("value1");
        subject["KEY2"].Should().Be("value2");
    }

    [Fact]
    public void ShouldUsePropertiesOfEntity()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var entity = new Node(
            1,
            new[] { "Person" },
            new Dictionary<string, object> { { "key1", "value1" }, { "key2", "value2" } });

        var subject = new DictAsRecord(entity, originalRecord);

        subject.Record.Should().BeSameAs(originalRecord);
        subject.Keys.Should().BeEquivalentTo(entity.Properties.Keys);
        subject.Values.Should().BeEquivalentTo(entity.Properties);
        subject[0].Should().Be("value1");
        subject[1].Should().Be("value2");
        subject["key1"].Should().Be("value1");
        subject["KEY2"].Should().Be("value2");
    }

    [Fact]
    public void ShouldThrowIfDictIsNotAnEntityOrDictionary()
    {
        var act = () => new DictAsRecord(42, null);
        act.Should().Throw<InvalidOperationException>();
    }
}
