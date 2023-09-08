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

namespace Neo4j.Driver.Tests.Mapping
{
    public class DictAsRecordTests
    {
        [Fact]
        public void ShouldReturnCorrectValues()
        {
            var dict = new Dictionary<string, object>
            {
                {"key1", "value1"},
                {"key2", "value2"}
            };
            var record = new DictAsRecord(dict, null);

            record.Keys.Should().BeEquivalentTo(dict.Keys);
            record.Values.Should().BeEquivalentTo(dict);
            record[0].Should().Be(dict["key1"]);
            record[1].Should().Be(dict["key2"]);
            record["key1"].Should().Be(dict["key1"]);
            record["key2"].Should().Be(dict["key2"]);
        }
    }
}
