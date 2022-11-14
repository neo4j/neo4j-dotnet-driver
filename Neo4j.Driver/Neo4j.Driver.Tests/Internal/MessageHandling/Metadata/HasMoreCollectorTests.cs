// Copyright (c) "Neo4j"
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
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    public class HasMoreCollectorTests
    {
        private const string Key = HasMoreCollector.HasMoreKey;

        [Fact]
        public void ShouldCollectFalseIfMetadataIsNull()
        {
            var collector = new HasMoreCollector();

            collector.Collect(null);

            collector.Collected.Should().BeFalse();
        }

        [Fact]
        public void ShouldCollectFalseIfNoValueIsGiven()
        {
            var collector = new HasMoreCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().BeFalse();
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, "some string"}};
            var collector = new HasMoreCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should()
                .Contain($"Expected '{Key}' metadata to be of type 'Boolean', but got 'String'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var metadata = new Dictionary<string, object> {{Key, true}};
            var collector = new HasMoreCollector();

            collector.Collect(metadata);

            collector.Collected.Should().BeTrue();
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object> {{Key, true}};
            var collector = new HasMoreCollector();

            collector.Collect(metadata);

            ((IMetadataCollector) collector).Collected.Should().Be(collector.Collected);
        }

        internal static KeyValuePair<string, object> TestMetadata =>
            new KeyValuePair<string, object>(Key, true);

        internal static bool TestMetadataCollected => true;
    }
}