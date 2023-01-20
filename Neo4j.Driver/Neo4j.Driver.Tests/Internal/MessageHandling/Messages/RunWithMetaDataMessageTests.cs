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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Messages
{
    public class RunWithMetaDataMessageTests
    {
        [Fact]
        public void ShouldHaveCorrectSerializer()
        {
            var rm = new RunWithMetadataMessage(BoltProtocolVersion.V3_0, null);

            rm.Serializer.Should().BeOfType<RunWithMetadataMessageSerializer>();
        }

        [Fact]
        public void ShouldOptimiseWriteMode()
        {
            var rm = new RunWithMetadataMessage(BoltProtocolVersion.V4_0, new Query("...", new { x = "y" }));
            rm.Query.Text.Should().Be("...");
            rm.Query.Parameters.Should().BeEquivalentTo(new Dictionary<string, object> { ["x"] = "y" });
            rm.ToString().Should().Be("RUN `...`, [{x, y}] []");
        }

        [Theory]
        [InlineData(4, 4)]
        [InlineData(5, 0)]
        public void ShouldIncludeImpersonatedUserKeyWithBoltVersionGreaterThan44(int major, int minor)
        {
            var rm = new RunWithMetadataMessage(new BoltProtocolVersion(major, minor), null, impersonatedUser: "jeff");
            rm.Query.Should().BeNull();
            rm.Metadata.Should().ContainKey("imp_user").WhichValue.Should().Be("jeff");

            rm.ToString().Should().Be("RUN  [{imp_user, jeff}]");
        }

        [Fact]
        public void ShouldIncludeValues()
        {
            var rm = new RunWithMetadataMessage(
                BoltProtocolVersion.V5_0,
                new Query("..."),
                new InternalBookmarks("bm:a"),
                new TransactionConfig
                {
                    Timeout = TimeSpan.FromSeconds(1),
                    Metadata = new Dictionary<string, object>
                    {
                        ["a"] = "b"
                    }
                },
                AccessMode.Read,
                "neo4j",
                "jeff");

            rm.Metadata.Should().ContainKey("bookmarks").WhichValue.Should().BeEquivalentTo(new[] { "bm:a" });
            rm.Metadata.Should().ContainKey("tx_timeout").WhichValue.Should().Be(1000L);
            rm.Metadata.Should()
                .ContainKey("tx_metadata")
                .WhichValue.Should()
                .BeEquivalentTo(new Dictionary<string, object> { ["a"] = "b" });

            rm.Metadata.Should().ContainKey("mode").WhichValue.Should().BeEquivalentTo("r");
            rm.Metadata.Should().ContainKey("db").WhichValue.Should().BeEquivalentTo("neo4j");
            rm.Metadata.Should().ContainKey("imp_user").WhichValue.Should().BeEquivalentTo("jeff");

            rm.ToString()
                .Should()
                .Be(
                    "RUN `...`, [] [{bookmarks, [bm:a]}, {tx_timeout, 1000}, {tx_metadata, [{a, b}]}, {mode, r}, {db, neo4j}, {imp_user, jeff}]");
        }
    }
}
