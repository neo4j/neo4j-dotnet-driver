// Copyright (c) 2002-2019 "Neo4j,"
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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    public class CountersCollectorTests
    {
        public const string Key = CountersCollector.CountersKey;

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new CountersCollector();

            collector.Collect(null);

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new CountersCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfValueIsNull()
        {
            var collector = new CountersCollector();

            collector.Collect(new Dictionary<string, object> {{Key, null}});

            collector.Collected.Should().BeNull();
        }


        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, true}};
            var collector = new CountersCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should()
                .Contain($"Expected '{Key}' metadata to be of type 'IDictionary<String,Object>', but got 'Boolean'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"nodes-created", 1L},
                        {"nodes-deleted", 2L},
                        {"relationships-created", 3L},
                        {"relationships-deleted", 4L},
                        {"properties-set", 5L},
                        {"labels-added", 6L},
                        {"labels-removed", 7L},
                        {"indexes-added", 8L},
                        {"indexes-removed", 9L},
                        {"constraints-added", 10L},
                        {"constraints-removed", 11L},
                    }
                }
            };
            var collector = new CountersCollector();

            collector.Collect(metadata);

            collector.Collected.Should().NotBeNull();
            collector.Collected.NodesCreated.Should().Be(1);
            collector.Collected.NodesDeleted.Should().Be(2);
            collector.Collected.RelationshipsCreated.Should().Be(3);
            collector.Collected.RelationshipsDeleted.Should().Be(4);
            collector.Collected.PropertiesSet.Should().Be(5);
            collector.Collected.LabelsAdded.Should().Be(6);
            collector.Collected.LabelsRemoved.Should().Be(7);
            collector.Collected.IndexesAdded.Should().Be(8);
            collector.Collected.IndexesRemoved.Should().Be(9);
            collector.Collected.ConstraintsAdded.Should().Be(10);
            collector.Collected.ConstraintsRemoved.Should().Be(11);
        }

        [Fact]
        public void ShouldCollectMissingCountersAsZero()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"nodes-created", 1L},
                        {"relationships-created", 2L},
                        {"properties-set", 3L},
                        {"labels-added", 4L},
                        {"indexes-added", 5L},
                        {"constraints-added", 6L},
                    }
                }
            };
            var collector = new CountersCollector();

            collector.Collect(metadata);

            collector.Collected.Should().NotBeNull();
            collector.Collected.NodesCreated.Should().Be(1);
            collector.Collected.NodesDeleted.Should().Be(0);
            collector.Collected.RelationshipsCreated.Should().Be(2);
            collector.Collected.RelationshipsDeleted.Should().Be(0);
            collector.Collected.PropertiesSet.Should().Be(3);
            collector.Collected.LabelsAdded.Should().Be(4);
            collector.Collected.LabelsRemoved.Should().Be(0);
            collector.Collected.IndexesAdded.Should().Be(5);
            collector.Collected.IndexesRemoved.Should().Be(0);
            collector.Collected.ConstraintsAdded.Should().Be(6);
            collector.Collected.ConstraintsRemoved.Should().Be(0);
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"nodes-created", 1L},
                        {"nodes-deleted", 2L},
                        {"relationships-created", 3L},
                        {"relationships-deleted", 4L},
                        {"properties-set", 5L},
                        {"labels-added", 6L},
                        {"labels-removed", 7L},
                        {"indexes-added", 8L},
                        {"indexes-removed", 9L},
                        {"constraints-added", 10L},
                        {"constraints-removed", 11L},
                    }
                }
            };
            var collector = new CountersCollector();

            collector.Collect(metadata);

            ((IMetadataCollector) collector).Collected.Should().BeSameAs(collector.Collected);
        }

        internal static KeyValuePair<string, object> TestMetadata =>
            new KeyValuePair<string, object>(Key, new Dictionary<string, object>
            {
                {"nodes-created", 1L},
                {"nodes-deleted", 2L},
                {"relationships-created", 3L},
                {"relationships-deleted", 4L},
                {"properties-set", 5L},
                {"labels-added", 6L},
                {"labels-removed", 7L},
                {"indexes-added", 8L},
                {"indexes-removed", 9L},
                {"constraints-added", 10L},
                {"constraints-removed", 11L},
            });

        internal static ICounters TestMetadataCollected => new Counters(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
    }
}