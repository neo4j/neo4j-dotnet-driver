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
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    public class FieldsCollectorTests
    {
        private const string Key = FieldsCollector.FieldsKey;

        internal static KeyValuePair<string, object> TestMetadata =>
            new(Key, new List<object> { "field-1", "field-2", "field-3", "field-4" });

        internal static string[] TestMetadataCollected => new[] { "field-1", "field-2", "field-3", "field-4" };

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new FieldsCollector();

            collector.Collect(null);

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new FieldsCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> { { Key, "some string" } };
            var collector = new FieldsCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should()
                .BeOfType<ProtocolException>()
                .Which
                .Message.Should()
                .Contain($"Expected '{Key}' metadata to be of type 'List<Object>', but got 'String'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var metadata = new Dictionary<string, object>
                { { Key, new List<object> { "field-1", "field-2", "field-3" } } };

            var collector = new FieldsCollector();

            collector.Collect(metadata);

            collector.Collected.Should()
                .HaveCount(3)
                .And
                .ContainInOrder("field-1", "field-2", "field-3");
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object>
                { { Key, new List<object> { "field-1", "field-2", "field-3" } } };

            var collector = new FieldsCollector();

            collector.Collect(metadata);

            ((IMetadataCollector)collector).Collected.Should().BeSameAs(collector.Collected);
        }
    }
}
