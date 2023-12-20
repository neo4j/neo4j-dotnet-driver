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
using FluentAssertions;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.MessageHandling.Metadata;

public class QueryIdCollectorTests
{
    private const string Key = QueryIdCollector.QueryIdKey;

    internal static KeyValuePair<string, object> TestMetadata => new(Key, 45L);

    internal static long TestMetadataCollected => 45L;

    [Fact]
    public void ShouldNotCollectIfMetadataIsNull()
    {
        var collector = new QueryIdCollector();

        collector.Collect(null);

        collector.Collected.Should().Be(-1);
    }

    [Fact]
    public void ShouldNotCollectIfNoValueIsGiven()
    {
        var collector = new QueryIdCollector();

        collector.Collect(new Dictionary<string, object>());

        collector.Collected.Should().Be(-1);
    }

    [Fact]
    public void ShouldThrowIfValueIsOfWrongType()
    {
        var metadata = new Dictionary<string, object> { { Key, false } };
        var collector = new QueryIdCollector();

        var ex = Record.Exception(() => collector.Collect(metadata));

        ex.Should()
            .BeOfType<ProtocolException>()
            .Which
            .Message.Should()
            .Contain($"Expected '{Key}' metadata to be of type 'Int64', but got 'Boolean'.");
    }

    [Fact]
    public void ShouldCollect()
    {
        var metadata = new Dictionary<string, object> { { Key, 5L } };
        var collector = new QueryIdCollector();

        collector.Collect(metadata);

        collector.Collected.Should().Be(5L);
    }

    [Fact]
    public void ShouldReturnSameCollected()
    {
        var metadata = new Dictionary<string, object> { { Key, 5L } };
        var collector = new QueryIdCollector();

        collector.Collect(metadata);

        ((IMetadataCollector)collector).Collected.Should().Be(collector.Collected);
    }
}
