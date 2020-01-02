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

using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    public class TypeCollectorTests
    {
        private const string Key = TypeCollector.TypeKey;

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new TypeCollector();

            collector.Collect(null);

            collector.Collected.Should().Be(QueryType.Unknown);
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new TypeCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().Be(QueryType.Unknown);
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, 3.14}};
            var collector = new TypeCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should().Contain($"Expected '{Key}' metadata to be of type 'String', but got 'Double'.");
        }

        [Fact]
        public void ShouldThrowIfValueIsInvalid()
        {
            var metadata = new Dictionary<string, object> {{Key, "xxx"}};
            var collector = new TypeCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should().Contain($"An invalid value of 'xxx' was passed as '{Key}' metadata.");
        }

        [Theory]
        [InlineData("r", QueryType.ReadOnly)]
        [InlineData("rw", QueryType.ReadWrite)]
        [InlineData("w", QueryType.WriteOnly)]
        [InlineData("s", QueryType.SchemaWrite)]
        public void ShouldCollect(string value, QueryType expectedValue)
        {
            var metadata = new Dictionary<string, object> {{Key, value}};
            var collector = new TypeCollector();

            collector.Collect(metadata);

            collector.Collected.Should().Be(expectedValue);
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object> {{Key, "rw"}};
            var collector = new TypeCollector();

            collector.Collect(metadata);

            ((IMetadataCollector) collector).Collected.Should().Be(collector.Collected);
        }

        internal static KeyValuePair<string, object> TestMetadata =>
            new KeyValuePair<string, object>(Key, "rw");

        internal static QueryType TestMetadataCollected => QueryType.ReadWrite;
    }
}