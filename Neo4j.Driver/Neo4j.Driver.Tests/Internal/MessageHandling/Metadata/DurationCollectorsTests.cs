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
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    public class ResultAvailableAfterCollectorTests
    {
        private const string Key = ResultAvailableAfterCollector.ResultAvailableAfterKey;

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new ResultAvailableAfterCollector();

            collector.Collect(null);

            collector.Collected.Should().Be(-1);
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new ResultAvailableAfterCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().Be(-1);
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, false}};
            var collector = new ResultAvailableAfterCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should().Contain($"Expected '{Key}' metadata to be of type 'Int64', but got 'Boolean'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var metadata = new Dictionary<string, object> {{Key, 5L}};
            var collector = new ResultAvailableAfterCollector();

            collector.Collect(metadata);

            collector.Collected.Should().Be(5L);
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object> {{Key, 5L}};
            var collector = new ResultAvailableAfterCollector();

            collector.Collect(metadata);

            ((IMetadataCollector) collector).Collected.Should().Be(collector.Collected);
        }

        internal static KeyValuePair<string, object> TestMetadata =>
            new KeyValuePair<string, object>(Key, 15L);

        internal static long TestMetadataCollected => 15L;
    }

    public class ResultConsumedAfterCollectorTests
    {
        private const string Key = ResultConsumedAfterCollector.ResultConsumedAfterKey;

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new ResultConsumedAfterCollector();

            collector.Collect(null);

            collector.Collected.Should().Be(-1);
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new ResultConsumedAfterCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().Be(-1);
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, false}};
            var collector = new ResultConsumedAfterCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should().Contain($"Expected '{Key}' metadata to be of type 'Int64', but got 'Boolean'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var metadata = new Dictionary<string, object> {{Key, 5L}};
            var collector = new ResultConsumedAfterCollector();

            collector.Collect(metadata);

            collector.Collected.Should().Be(5L);
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object> {{Key, 5L}};
            var collector = new ResultConsumedAfterCollector();

            collector.Collect(metadata);

            ((IMetadataCollector) collector).Collected.Should().Be(collector.Collected);
        }

        internal static KeyValuePair<string, object> TestMetadata =>
            new KeyValuePair<string, object>(Key, 25L);

        internal static long TestMetadataCollected => 25L;
    }

    public class TimeToFirstCollectorTests
    {
        private const string Key = TimeToFirstCollector.TimeToFirstKey;

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new TimeToFirstCollector();

            collector.Collect(null);

            collector.Collected.Should().Be(-1);
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new TimeToFirstCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().Be(-1);
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, false}};
            var collector = new TimeToFirstCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should().Contain($"Expected '{Key}' metadata to be of type 'Int64', but got 'Boolean'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var metadata = new Dictionary<string, object> {{Key, 5L}};
            var collector = new TimeToFirstCollector();

            collector.Collect(metadata);

            collector.Collected.Should().Be(5L);
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object> {{Key, 5L}};
            var collector = new TimeToFirstCollector();

            collector.Collect(metadata);

            ((IMetadataCollector) collector).Collected.Should().Be(collector.Collected);
        }

        internal static KeyValuePair<string, object> TestMetadata =>
            new KeyValuePair<string, object>(Key, 35L);

        internal static long TestMetadataCollected => 35L;
    }

    public class TimeToLastCollectorTests
    {
        private const string Key = TimeToLastCollector.TimeToLastKey;

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new TimeToLastCollector();

            collector.Collect(null);

            collector.Collected.Should().Be(-1);
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new TimeToLastCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().Be(-1);
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, false}};
            var collector = new TimeToLastCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should().Contain($"Expected '{Key}' metadata to be of type 'Int64', but got 'Boolean'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var metadata = new Dictionary<string, object> {{Key, 5L}};
            var collector = new TimeToLastCollector();

            collector.Collect(metadata);

            collector.Collected.Should().Be(5L);
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object> {{Key, 5L}};
            var collector = new TimeToLastCollector();

            collector.Collect(metadata);

            ((IMetadataCollector) collector).Collected.Should().Be(collector.Collected);
        }

        internal static KeyValuePair<string, object> TestMetadata =>
            new KeyValuePair<string, object>(Key, 45L);

        internal static long TestMetadataCollected => 45L;
    }
}