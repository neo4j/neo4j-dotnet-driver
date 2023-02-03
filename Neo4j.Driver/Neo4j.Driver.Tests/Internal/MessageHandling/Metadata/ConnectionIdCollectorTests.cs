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
    public class ConnectionIdCollectorTests
    {
        private const string Key = ConnectionIdCollector.ConnectionIdKey;

        internal static KeyValuePair<string, object> TestMetadata => new(Key, "id-1");

        internal static string TestMetadataCollected => "id-1";

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new ConnectionIdCollector();

            collector.Collect(null);

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new ConnectionIdCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> { { Key, 5 } };
            var collector = new ConnectionIdCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should()
                .BeOfType<ProtocolException>()
                .Which
                .Message.Should()
                .Contain($"Expected '{Key}' metadata to be of type 'String', but got 'Int32'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var metadata = new Dictionary<string, object> { { Key, "id-5" } };
            var collector = new ConnectionIdCollector();

            collector.Collect(metadata);

            collector.Collected.Should().Be("id-5");
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object> { { Key, "id-5" } };
            var collector = new ConnectionIdCollector();

            collector.Collect(metadata);

            ((IMetadataCollector)collector).Collected.Should().BeSameAs(collector.Collected);
        }
    }
}
