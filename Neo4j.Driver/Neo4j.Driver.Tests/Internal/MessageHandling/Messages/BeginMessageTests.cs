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

public class BeginMessageTests
{
    [Fact]
    public void ShouldHaveCorrectSerializer()
    {
        var rm = new BeginMessage(null, null, null, null, AccessMode.Read, null, null);

        rm.Serializer.Should().BeOfType<BeginMessageSerializer>();
    }

    [Fact]
    public void ShouldHandleNullValues()
    {
        var rm = new BeginMessage(null, null, null, null, AccessMode.Write, null, null);

        rm.ToString().Should().Be("BEGIN []");
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(5, 0)]
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
            new TransactionConfig { Metadata = txMeta, Timeout = TimeSpan.FromSeconds(1) },
            AccessMode.Read,
            new SessionConfig("Douglas Fir"),
            null);

        message.Metadata.Should().ContainKey("bookmarks").WhoseValue.Should().BeEquivalentTo(new[] { "bm:a" });
        message.Metadata.Should().ContainKey("imp_user").WhoseValue.Should().BeEquivalentTo("Douglas Fir");
        message.Metadata.Should().ContainKey("tx_timeout").WhoseValue.Should().BeEquivalentTo(1000L);
        message.Metadata.Should().ContainKey("tx_metadata").WhoseValue.Should().BeEquivalentTo(txMeta);
        message.Metadata.Should().ContainKey("mode").WhoseValue.Should().BeEquivalentTo("r");
        message.Metadata.Should().ContainKey("db").WhoseValue.Should().BeEquivalentTo("neo4j");

        message.ToString()
            .Should()
            .Be(
                "BEGIN [{bookmarks, [bm:a]}, {tx_timeout, 1000}, {tx_metadata, [{a, b}]}, {mode, r}, {db, neo4j}, {imp_user, Douglas Fir}]");
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(5, 0)]
    [InlineData(5, 1)]
    public void ShouldIgnoreNotificationConfig(int major, int minor)
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
            new TransactionConfig { Metadata = txMeta, Timeout = TimeSpan.FromSeconds(1) },
            AccessMode.Read,
            new SessionConfig("Douglas Fir"),
            new NotificationsDisabledConfig());

        message.Metadata.Should().ContainKey("bookmarks").WhoseValue.Should().BeEquivalentTo(new[] { "bm:a" });
        message.Metadata.Should().ContainKey("imp_user").WhoseValue.Should().BeEquivalentTo("Douglas Fir");
        message.Metadata.Should().ContainKey("tx_timeout").WhoseValue.Should().BeEquivalentTo(1000L);
        message.Metadata.Should().ContainKey("tx_metadata").WhoseValue.Should().BeEquivalentTo(txMeta);
        message.Metadata.Should().ContainKey("mode").WhoseValue.Should().BeEquivalentTo("r");
        message.Metadata.Should().ContainKey("db").WhoseValue.Should().BeEquivalentTo("neo4j");

        message.ToString()
            .Should()
            .Be(
                "BEGIN [{bookmarks, [bm:a]}, {tx_timeout, 1000}, {tx_metadata, [{a, b}]}, {mode, r}, {db, neo4j}, {imp_user, Douglas Fir}]");
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(5, 4)]
    public void ShouldHandleSetValuesWithNotifications(int major, int minor)
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
            new TransactionConfig { Metadata = txMeta, Timeout = TimeSpan.FromSeconds(1) },
            AccessMode.Read,
            new SessionConfig("Douglas Fir"),
            new NotificationsConfig(Severity.Warning, new[] { Category.Generic }));

        message.Metadata.Should().ContainKey("bookmarks").WhoseValue.Should().BeEquivalentTo(new[] { "bm:a" });
        message.Metadata.Should().ContainKey("imp_user").WhoseValue.Should().BeEquivalentTo("Douglas Fir");
        message.Metadata.Should().ContainKey("tx_timeout").WhoseValue.Should().BeEquivalentTo(1000L);
        message.Metadata.Should().ContainKey("tx_metadata").WhoseValue.Should().BeEquivalentTo(txMeta);
        message.Metadata.Should().ContainKey("mode").WhoseValue.Should().BeEquivalentTo("r");
        message.Metadata.Should().ContainKey("db").WhoseValue.Should().BeEquivalentTo("neo4j");
        message.Metadata.Should()
            .ContainKey("notifications_minimum_severity")
            .WhoseValue.Should()
            .BeEquivalentTo("WARNING");

        message.Metadata.Should()
            .ContainKey("notifications_disabled_categories")
            .WhoseValue
            .Should()
            .BeEquivalentTo(new[] { "GENERIC" });

        message.ToString()
            .Should()
            .Be(
                "BEGIN [{bookmarks, [bm:a]}, {tx_timeout, 1000}, {tx_metadata, [{a, b}]}, {mode, r}, {db, neo4j}, {imp_user, Douglas Fir}, {notifications_minimum_severity, WARNING}, {notifications_disabled_categories, [GENERIC]}]");
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(6, 0)]
    public void ShouldHandleSetValuesWithNotificationsClassifications(int major, int minor)
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
            new TransactionConfig { Metadata = txMeta, Timeout = TimeSpan.FromSeconds(1) },
            AccessMode.Read,
            new SessionConfig("Douglas Fir"),
            new NotificationsConfig(Severity.Warning, [Category.Generic]));

        message.Metadata.Should().ContainKey("bookmarks").WhoseValue.Should().BeEquivalentTo(new[] { "bm:a" });
        message.Metadata.Should().ContainKey("imp_user").WhoseValue.Should().BeEquivalentTo("Douglas Fir");
        message.Metadata.Should().ContainKey("tx_timeout").WhoseValue.Should().BeEquivalentTo(1000L);
        message.Metadata.Should().ContainKey("tx_metadata").WhoseValue.Should().BeEquivalentTo(txMeta);
        message.Metadata.Should().ContainKey("mode").WhoseValue.Should().BeEquivalentTo("r");
        message.Metadata.Should().ContainKey("db").WhoseValue.Should().BeEquivalentTo("neo4j");
        message.Metadata.Should()
            .ContainKey("notifications_minimum_severity")
            .WhoseValue
            .Should()
            .BeEquivalentTo("WARNING");

        message.Metadata.Should()
            .ContainKey("notifications_disabled_classifications")
            .WhoseValue
            .Should()
            .BeEquivalentTo(new[] { "GENERIC" });

        message.ToString()
            .Should()
            .Be(
                "BEGIN [{bookmarks, [bm:a]}, {tx_timeout, 1000}, {tx_metadata, [{a, b}]}, {mode, r}, {db, neo4j}, {imp_user, Douglas Fir}, {notifications_minimum_severity, WARNING}, {notifications_disabled_classifications, [GENERIC]}]");
    }

    [Fact]
    public void ShouldThrowIfBoltVersionLessThan44()
    {
        Record.Exception(
                () => new BeginMessage(
                    BoltProtocolVersion.V4_3,
                    "neo4j",
                    new InternalBookmarks("bm:a"),
                    new TransactionConfig
                    {
                        Metadata = new Dictionary<string, object>
                        {
                            ["a"] = "b"
                        },
                        Timeout = TimeSpan.FromSeconds(1)
                    },
                    AccessMode.Read,
                    new SessionConfig("Douglas Fir"),
                    null))
            .Should()
            .BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(5, 3)]
    [InlineData(5, 4)]
    public void ShouldAddNotificationsCategories(int major, int minor)
    {
        var cfg = new NotificationsConfig(Severity.Information, [Category.Hint]);
        var beginMessage = new BeginMessage(
            new BoltProtocolVersion(major, minor),
            null,
            null,
            null,
            AccessMode.Read,
            null,
            cfg);

        beginMessage.Metadata.Should()
            .ContainKey("notifications_disabled_categories")
            .WhoseValue.Should()
            .BeEquivalentTo(new[] { "HINT" });

        beginMessage.Metadata.Should()
            .ContainKey("notifications_minimum_severity")
            .WhoseValue.Should()
            .Be("INFORMATION");
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(5, 6)]
    [InlineData(6, 0)]
    public void ShouldAddNotificationsClassifications(int major, int minor)
    {
        var cfg = new NotificationsConfig(Severity.Information, [Category.Hint]);
        var beginMessage = new BeginMessage(
            new BoltProtocolVersion(major, minor),
            null,
            null,
            null,
            AccessMode.Read,
            null,
            cfg);

        beginMessage.Metadata.Should()
            .ContainKey("notifications_disabled_classifications")
            .WhoseValue
            .Should()
            .BeEquivalentTo(new[] { "HINT" });

        beginMessage.Metadata.Should()
            .ContainKey("notifications_minimum_severity")
            .WhoseValue
            .Should()
            .Be("INFORMATION");
    }
}
