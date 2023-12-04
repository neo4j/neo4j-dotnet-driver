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
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    public class ConfigurationHintsCollectorTests
    {
        private const string Key = ConfigurationHintsCollector.ConfigHintsKey;
        private const string RecvTimeoutKey = "connection.recv_timeout_seconds";

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new ConfigurationHintsCollector();

            collector.Collect(null);

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new ConfigurationHintsCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> { { Key, "WrongType" } };
            var collector = new ConfigurationHintsCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should()
                .BeOfType<ProtocolException>()
                .Which
                .Message.Should()
                .Contain($"Expected '{Key}' metadata to be of type 'Dictionary<string, object>', but got 'String'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var hintsMetadata = new Dictionary<string, object> { { RecvTimeoutKey, 5 } };
            var metadata = new Dictionary<string, object> { { Key, hintsMetadata } };
            var collector = new ConfigurationHintsCollector();

            collector.Collect(metadata);

            collector.Collected.Equals(hintsMetadata);
        }
    }
}
