// Copyright (c) 2002-2020 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
	public class RoutingTableCollectorTest
	{
        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new RoutingTableCollector();

            collector.Collect(null);

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new RoutingTableCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> { { RoutingTableCollector.RoutingTableKey, "some string" } };
            var collector = new RoutingTableCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should()
                .Contain($"Expected '{RoutingTableCollector.RoutingTableKey}' metadata to be of type 'Dictionary<string, object>', but got 'String'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var metadata = new Dictionary<string, object> { { RoutingTableCollector.RoutingTableKey, new Dictionary<string, object> { { "Key1", "Value1" },
                                                                                                                                      { "Key2", "Value2" },
                                                                                                                                      { "Key3", "Value3" } } } };
            var collector = new RoutingTableCollector();

            collector.Collect(metadata);

            collector.Collected.Should().HaveCount(3).And.Contain(new [] { new KeyValuePair<string, object>("Key1", "Value1"),
                                                                           new KeyValuePair<string, object>("Key2", "Value2"),
                                                                           new KeyValuePair<string, object>("Key3", "Value3") });            
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object> { { RoutingTableCollector.RoutingTableKey, new Dictionary<string, object> { { "Key1", "Value1" },
                                                                                                                                      { "Key2", "Value2" },
                                                                                                                                      { "Key3", "Value3" } } } };
            var collector = new RoutingTableCollector();

            collector.Collect(metadata);

            ((IMetadataCollector)collector).Collected.Should().BeSameAs(collector.Collected);
        }
    }
}
