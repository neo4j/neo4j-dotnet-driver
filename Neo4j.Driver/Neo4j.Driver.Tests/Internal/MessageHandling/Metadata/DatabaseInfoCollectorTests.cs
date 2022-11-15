// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
// you may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata;

public class DatabaseInfoCollectorTests
{
    private const string Key = DatabaseInfoCollector.DbKey;

    internal static KeyValuePair<string, object> TestMetadata => new(Key, "foo");

    internal static IDatabaseInfo TestMetadataCollected => new DatabaseInfo("foo");

    [Fact]
    public void ShouldCollectFalseIfMetadataIsNull()
    {
        var collector = new DatabaseInfoCollector();

        collector.Collect(null);

        collector.Collected.Name.Should().BeNull();
    }

    [Fact]
    public void ShouldCollectFalseIfNoValueIsGiven()
    {
        var collector = new DatabaseInfoCollector();

        collector.Collect(new Dictionary<string, object>());

        collector.Collected.Name.Should().BeNull();
    }

    [Fact]
    public void ShouldThrowIfValueIsOfWrongType()
    {
        var metadata = new Dictionary<string, object> { { Key, 1L } };
        var collector = new DatabaseInfoCollector();

        var ex = Record.Exception(() => collector.Collect(metadata));

        ex.Should()
            .BeOfType<ProtocolException>()
            .Which
            .Message.Should()
            .Contain($"Expected '{Key}' metadata to be of type 'string', but got 'Int64'.");
    }

    [Fact]
    public void ShouldCollect()
    {
        var metadata = new Dictionary<string, object> { { Key, "my-database" } };
        var collector = new DatabaseInfoCollector();

        collector.Collect(metadata);

        collector.Collected.Name.Should().Be("my-database");
    }

    [Fact]
    public void ShouldReturnSameCollected()
    {
        var metadata = new Dictionary<string, object> { { Key, "my-database" } };
        var collector = new DatabaseInfoCollector();

        collector.Collect(metadata);

        ((IMetadataCollector)collector).Collected.Should().Be(collector.Collected);
    }
}
