﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Util;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    public class ServerVersionCollectorTests
    {
        private const string Key = ServerVersionCollector.ServerKey;

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new ServerVersionCollector();

            collector.Collect(null);

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new ServerVersionCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, true}};
            var collector = new ServerVersionCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should().Contain($"Expected '{Key}' metadata to be of type 'String', but got 'Boolean'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var versionStr = "Neo4j/3.5.12-alpha1";
            var metadata = new Dictionary<string, object> {{Key, versionStr}};
            var collector = new ServerVersionCollector();

            collector.Collect(metadata);

            collector.Collected.Product.Should().Be("Neo4j");
            collector.Collected.Major.Should().Be(3);
            collector.Collected.Minor.Should().Be(5);
            collector.Collected.Patch.Should().Be(12);
            collector.Collected.ToString().Should().Be(versionStr);
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var versionStr = "Neo4j/3.5.12-alpha1";
            var metadata = new Dictionary<string, object> {{Key, versionStr}};
            var collector = new ServerVersionCollector();

            collector.Collect(metadata);

            ((IMetadataCollector) collector).Collected.Should().BeSameAs(collector.Collected);
        }

        internal static KeyValuePair<string, object> TestMetadata =>
            new KeyValuePair<string, object>(Key, "Neo4j/3.5.2");

        internal static ServerVersion TestMetadataCollected => ServerVersion.From("Neo4j/3.5.2");
    }
}