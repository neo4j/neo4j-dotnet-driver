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
using FluentAssertions;
using Neo4j.Driver.Preview.Mapping;
using Xunit;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests.Mapping
{
    public class DictAsRecordTests
    {
        [Fact]
        public void ShouldReturnCorrectValues()
        {
            var originalRecord = new Record(new[] {"name", "age"}, new object[] {"Bob", 42});
            var dict = new Dictionary<string, object>
            {
                {"key1", "value1"},
                {"key2", "value2"}
            };

            var subject = new DictAsRecord(dict, originalRecord);

            subject.Record.Should().BeSameAs(originalRecord);
            subject.Keys.Should().BeEquivalentTo(dict.Keys);
            subject.Values.Should().BeEquivalentTo(dict);
            subject[0].Should().Be("value1");
            subject[1].Should().Be("value2");
            subject["key1"].Should().Be("value1");
            subject["key2"].Should().Be("value2");
        }
    }
}
