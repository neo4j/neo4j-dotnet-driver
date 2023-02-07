﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class MessageTests
    {
        public class MessageToStringMethod
        {
            public static IEnumerable<object[]> MessageData => new[]
            {
                new object[]
                {
                    new BeginMessage(
                        BoltProtocolVersion.V4_4,
                        null,
                        Bookmarks.From("bookmark1", "bookmark2"),
                        TransactionConfigGenerator(),
                        AccessMode.Read,
                        "Impersonated User",
                        null),
                    "BEGIN [{bookmarks, [bookmark1, bookmark2]}, {tx_timeout, 1000}, {mode, r}, {imp_user, Impersonated User}]"
                },
                new object[]
                {
                    new RouteMessage(
                        new Dictionary<string, string> { { "RoutingKey", "RoutingValue" }, { "Bob", "Empty" } },
                        Bookmarks.From("bookmark-1"),
                        "myDB",
                        "Impersonated User"),
                    "ROUTE { \'RoutingKey\':\'RoutingValue\' \'Bob\':\'Empty\' } { bookmarks, [bookmark-1] } { \'db\':\'myDB\' \'imp_user\':\'Impersonated User\' }"
                },
                new object[]
                {
                    new RouteMessageV43(
                        new Dictionary<string, string> { { "RoutingKey", "RoutingValue" }, { "Bob", "Empty" } },
                        Bookmarks.From("bookmark-1"),
                        ""),
                    "ROUTE { \'RoutingKey\':\'RoutingValue\' \'Bob\':\'Empty\' } { bookmarks, [bookmark-1] } None"
                },
                new object[]
                {
                    new RouteMessageV43(
                        new Dictionary<string, string> { { "RoutingKey", "RoutingValue" }, { "Bob", "Null" } },
                        Bookmarks.From("bookmark-1"),
                        null),
                    "ROUTE { \'RoutingKey\':\'RoutingValue\' \'Bob\':\'Null\' } { bookmarks, [bookmark-1] } None"
                },
                new object[]
                {
                    new RouteMessageV43(
                        new Dictionary<string, string> { { "RoutingKey", "RoutingValue" }, { "Bob", "adb" } },
                        Bookmarks.From("bookmark-1"),
                        "adb"),
                    "ROUTE { \'RoutingKey\':\'RoutingValue\' \'Bob\':\'adb\' } { bookmarks, [bookmark-1] } \'adb\'"
                },
                new object[]
                {
                    new RouteMessageV43(
                        new Dictionary<string, string> { { "RoutingKey", "RoutingValue" }, { "Bob", "adb" } },
                        null,
                        "adb"),
                    "ROUTE { \'RoutingKey\':\'RoutingValue\' \'Bob\':\'adb\' } [] \'adb\'"
                },
                new object[] { new FailureMessage("CODE", "MESSAGE"), "FAILURE code=CODE, message=MESSAGE" },
                new object[]
                {
                    new HelloMessage(
                        BoltProtocolVersion.V4_4,
                        "mydriver",
                        null,
                        new Dictionary<string, string> { { "RoutingKey", "RoutingValue" } }),
                    "HELLO [{user_agent, mydriver}, {routing, [{RoutingKey, RoutingValue}]}, {patch_bolt, [utc]}]"
                },
                new object[]
                {
                    new HelloMessage(BoltProtocolVersion.V4_4, "mydriver", null, new Dictionary<string, string>()),
                    "HELLO [{user_agent, mydriver}, {routing, []}, {patch_bolt, [utc]}]"
                },
                new object[]
                {
                    new HelloMessage(BoltProtocolVersion.V4_4, "mydriver", null, null as IDictionary<string, string>),
                    "HELLO [{user_agent, mydriver}, {routing, NULL}, {patch_bolt, [utc]}]"
                },
                new object[]
                {
                    new HelloMessage(
                        BoltProtocolVersion.V4_4,
                        "mydriver",
                        null,
                        new Dictionary<string, string> { { "RoutingKey", "RoutingValue" } }),
                    "HELLO [{user_agent, mydriver}, {routing, [{RoutingKey, RoutingValue}]}, {patch_bolt, [utc]}]"
                },
                new object[]
                {
                    new HelloMessage(BoltProtocolVersion.V4_4, "mydriver", null, new Dictionary<string, string>()),
                    "HELLO [{user_agent, mydriver}, {routing, []}, {patch_bolt, [utc]}]"
                },
                new object[]
                {
                    new HelloMessage(BoltProtocolVersion.V4_4, "mydriver", null, null as IDictionary<string, string>),
                    "HELLO [{user_agent, mydriver}, {routing, NULL}, {patch_bolt, [utc]}]"
                },
                new object[]
                {
                    new HelloMessage(
                        BoltProtocolVersion.V4_2,
                        "mydriver",
                        null,
                        new Dictionary<string, string> { { "RoutingKey", "RoutingValue" } }),
                    "HELLO [{user_agent, mydriver}, {routing, [{RoutingKey, RoutingValue}]}]"
                },
                new object[]
                {
                    new HelloMessage(BoltProtocolVersion.V4_2, "mydriver", null, new Dictionary<string, string>()),
                    "HELLO [{user_agent, mydriver}, {routing, []}]"
                },
                new object[]
                {
                    new HelloMessage(BoltProtocolVersion.V4_2, "mydriver", null, null as IDictionary<string, string>),
                    "HELLO [{user_agent, mydriver}, {routing, NULL}]"
                },
                new object[]
                {
                    new HelloMessage(
                        BoltProtocolVersion.V4_1,
                        "mydriver",
                        null,
                        new Dictionary<string, string> { { "RoutingKey", "RoutingValue" } }),
                    "HELLO [{user_agent, mydriver}, {routing, [{RoutingKey, RoutingValue}]}]"
                },
                new object[]
                {
                    new HelloMessage(BoltProtocolVersion.V4_1, "mydriver", null, new Dictionary<string, string>()),
                    "HELLO [{user_agent, mydriver}, {routing, []}]"
                },
                new object[]
                {
                    new HelloMessage(BoltProtocolVersion.V4_1, "mydriver", null, null as IDictionary<string, string>),
                    "HELLO [{user_agent, mydriver}, {routing, NULL}]"
                },
                new object[]
                {
                    new HelloMessage(BoltProtocolVersion.V3_0, "mydriver", null, null as IDictionary<string, string>),
                    "HELLO [{user_agent, mydriver}]"
                },
                new object[] { new SuccessMessage(new Dictionary<string, object>()), "SUCCESS []" },
                new object[] { IgnoredMessage.Instance, "IGNORED" },
                new object[] { PullAllMessage.Instance, "PULLALL" },
                new object[]
                {
                    new RecordMessage(new object[] { 1, "a string", new[] { 3, 4 } }),
                    "RECORD [1, a string, [3, 4]]"
                },
                new object[] { ResetMessage.Instance, "RESET" },
                new object[]
                {
                    new RunWithMetadataMessage(
                        BoltProtocolVersion.V3_0,
                        new Query(
                            "A query",
                            new Dictionary<string, object>
                            {
                                { "key1", 1 },
                                { "key2", new[] { 2, 4 } }
                            }),
                        database: "my-database",
                        bookmarks: Bookmarks.From("bookmark-1"),
                        config: TransactionConfig.Default,
                        mode: AccessMode.Read),
                    "RUN `A query`, [{key1, 1}, {key2, [2, 4]}] [{bookmarks, [bookmark-1]}, {mode, r}, {db, my-database}]"
                },
                new object[] { new PullMessage(1, 2), "PULL [{n, 2}, {qid, 1}]" },
                new object[] { new PullMessage(2), "PULL [{n, 2}]" },
                new object[] { new DiscardMessage(1, 2), "DISCARD [{n, 2}, {qid, 1}]" },
                new object[] { new DiscardMessage(2), "DISCARD [{n, 2}]" }
            };

            private static TransactionConfig TransactionConfigGenerator()
            {
                var config = new TransactionConfig();
                config.Timeout = TimeSpan.FromMilliseconds(1000);
                return config;
            }

            [Theory]
            [MemberData(nameof(MessageData))]
            internal void ShouldPrintTheMessageAsExpected(IMessage message, string expected)
            {
                var m = message.ToString();
                m.Should().Be(expected);
            }
        }
    }
}
