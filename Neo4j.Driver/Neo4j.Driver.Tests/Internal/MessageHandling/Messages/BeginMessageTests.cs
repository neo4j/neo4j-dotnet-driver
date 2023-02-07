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
    public class BeginMessageTests
    {
        [Fact]
        public void ShouldHaveCorrectSerializer()
        {
            var rm = new BeginMessage(null, null, null, null, null, AccessMode.Read, null);

            rm.Serializer.Should().BeOfType<BeginMessageSerializer>();
        }

        [Fact]
        public void ShouldHandleNullValues()
        {
            var rm = new BeginMessage(null, null, null, null, null, AccessMode.Write, null);

            rm.ToString().Should().Be("BEGIN []");
        }

        [Theory]
        [InlineData(4, 4)]
        [InlineData(5, 0)]
        [InlineData(6, 0)]
        public void ShouldHandleSetValues(int major, int minor)
        {
            var txMeta = new Dictionary<string, object>
            {
                ["a"] = "b"
            };

            var bookmarks = new InternalBookmarks("bm:a");
            var message = new BeginMessage(
                new BoltProtocolVersion(major, minor),
                "neo4j",
                bookmarks,
                TimeSpan.FromSeconds(1),
                txMeta,
                AccessMode.Read,
                new("Douglas Fir"));

            message.Metadata.Should().ContainKey("bookmarks").WhichValue.Should().BeEquivalentTo(new[] { "bm:a" });
            message.Metadata.Should().ContainKey("imp_user").WhichValue.Should().BeEquivalentTo("Douglas Fir");
            message.Metadata.Should().ContainKey("tx_timeout").WhichValue.Should().BeEquivalentTo(1000L);
            message.Metadata.Should().ContainKey("tx_metadata").WhichValue.Should().BeEquivalentTo(txMeta);
            message.Metadata.Should().ContainKey("mode").WhichValue.Should().BeEquivalentTo("r");
            message.Metadata.Should().ContainKey("db").WhichValue.Should().BeEquivalentTo("neo4j");

            message.ToString()
                .Should()
                .Be(
                    "BEGIN [{bookmarks, [bm:a]}, {tx_timeout, 1000}, {tx_metadata, [{a, b}]}, {mode, r}, {db, neo4j}, {imp_user, Douglas Fir}]");
        }

        [Fact]
        public void ShouldValidateTimeout()
        {
            var message = new BeginMessage(
                BoltProtocolVersion.V3_0,
                null,
                null,
                TimeSpan.FromSeconds(-1),
                null,
                AccessMode.Write,
                null);

            message.Metadata.Should().ContainKey("tx_timeout").WhichValue.Should().BeEquivalentTo(0L);
        }

        [Fact]
        public void ShouldThrowIfBoltVersionLessThan44()
        {
            Record.Exception(
                    () => new BeginMessage(
                        BoltProtocolVersion.V4_3,
                        "neo4j",
                        new InternalBookmarks("bm:a"),
                        TimeSpan.FromSeconds(1),
                        new Dictionary<string, object>
                        {
                            ["a"] = "b"
                        },
                        AccessMode.Read,
                        new("Douglas Fir")))
                .Should()
                .BeOfType<ArgumentOutOfRangeException>();
        }
    }
}
