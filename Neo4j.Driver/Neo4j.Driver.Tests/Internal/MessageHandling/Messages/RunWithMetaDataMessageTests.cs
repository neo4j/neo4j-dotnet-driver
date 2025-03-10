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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Types;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.MessageHandling.Messages;

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
        var rm = new RunWithMetadataMessage(
            new BoltProtocolVersion(major, minor),
            null,
            sessionConfig: new SessionConfig("jeff"));

        rm.Query.Should().BeNull();
        rm.Metadata.Should().ContainKey("imp_user").WhoseValue.Should().Be("jeff");

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
            new SessionConfig("jeff"));

        rm.Metadata.Should().ContainKey("bookmarks").WhoseValue.Should().BeEquivalentTo(new[] { "bm:a" });
        rm.Metadata.Should().ContainKey("tx_timeout").WhoseValue.Should().Be(1000L);
        rm.Metadata.Should()
            .ContainKey("tx_metadata")
            .WhoseValue
            .Should()
            .BeEquivalentTo(new Dictionary<string, object> { ["a"] = "b" });

        rm.Metadata.Should().ContainKey("mode").WhoseValue.Should().BeEquivalentTo("r");
        rm.Metadata.Should().ContainKey("db").WhoseValue.Should().BeEquivalentTo("neo4j");
        rm.Metadata.Should().ContainKey("imp_user").WhoseValue.Should().BeEquivalentTo("jeff");

        rm.ToString()
            .Should()
            .Be(
                "RUN `...`, [] [{bookmarks, [bm:a]}, {tx_timeout, 1000}, {tx_metadata, [{a, b}]}, {mode, r}, {db, neo4j}, {imp_user, jeff}]");
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(5, 3)]
    [InlineData(5, 4)]
    public void ShouldAddNotificationsCategories(int major, int minor)
    {
        var cfg = new NotificationsConfig(Severity.Information, [Category.Hint]);
        var runMessage = new RunWithMetadataMessage(
            new BoltProtocolVersion(major, minor),
            new Query(""),
            notificationsConfig: cfg);

        runMessage.Metadata.Should()
            .ContainKey("notifications_disabled_categories")
            .WhoseValue
            .Should()
            .BeEquivalentTo(new[] { "HINT" });

        runMessage.Metadata.Should()
            .ContainKey("notifications_minimum_severity")
            .WhoseValue
            .Should()
            .Be("INFORMATION");
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(5, 6)]
    [InlineData(6, 0)]
    public void ShouldAddNotificationsClassifications(int major, int minor)
    {
        var cfg = new NotificationsConfig(Severity.Information, [Category.Hint]);
        var runMessage = new RunWithMetadataMessage(
            new BoltProtocolVersion(major, minor),
            new Query(""),
            notificationsConfig: cfg);

        runMessage.Metadata.Should()
            .ContainKey("notifications_disabled_classifications")
            .WhoseValue
            .Should()
            .BeEquivalentTo(new[] { "HINT" });

        runMessage.Metadata.Should()
            .ContainKey("notifications_minimum_severity")
            .WhoseValue
            .Should()
            .Be("INFORMATION");
    }
}
