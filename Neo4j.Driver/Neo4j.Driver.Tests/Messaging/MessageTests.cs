// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Messaging;
using V3 = Neo4j.Driver.Internal.Messaging.V3;
using V4 = Neo4j.Driver.Internal.Messaging.V4;
using V4_1 = Neo4j.Driver.Internal.Messaging.V4_1;
using V4_2 = Neo4j.Driver.Internal.Messaging.V4_2;
using V4_3 = Neo4j.Driver.Internal.Messaging.V4_3;
using Xunit;
using static Neo4j.Driver.Internal.Messaging.DiscardAllMessage;
using static Neo4j.Driver.Internal.Messaging.IgnoredMessage;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;

namespace Neo4j.Driver.Tests
{
    public class MessageTests
    {
        public class MessageToStringMethod
        {
            public static IEnumerable<object[]> MessageData => new[]
            {
				new object[] {new V4_3.RouteMessage(new Dictionary<string, string> { { "RoutingKey", "RoutingValue" }, { "Bob", "Empty" } }, ""), "ROUTE { \'RoutingKey\':\'RoutingValue\' \'Bob\':\'Empty\' } None" },
				new object[] {new V4_3.RouteMessage(new Dictionary<string, string> { { "RoutingKey", "RoutingValue" }, { "Bob", "Null" } }, null), "ROUTE { \'RoutingKey\':\'RoutingValue\' \'Bob\':\'Null\' } None" },
				new object[] {new V4_3.RouteMessage(new Dictionary<string, string> { { "RoutingKey", "RoutingValue" }, { "Bob", "adb" } }, "adb"), "ROUTE { \'RoutingKey\':\'RoutingValue\' \'Bob\':\'adb\' } \'adb\'" },
                new object[] {new FailureMessage("CODE", "MESSAGE"), "FAILURE code=CODE, message=MESSAGE"},
                new object[] {new V4_3.HelloMessage("mydriver", null, new Dictionary<string, string> {{ "RoutingKey", "RoutingValue" }}), "HELLO [{user_agent, mydriver}, {routing, [{RoutingKey, RoutingValue}]}]"},
                new object[] {new V4_3.HelloMessage("mydriver", null, new Dictionary<string, string>()), "HELLO [{user_agent, mydriver}, {routing, []}]"},
                new object[] {new V4_3.HelloMessage("mydriver", null, null), "HELLO [{user_agent, mydriver}, {routing, NULL}]"},
                new object[] {new V4_2.HelloMessage("mydriver", null, new Dictionary<string, string> {{ "RoutingKey", "RoutingValue" }}), "HELLO [{user_agent, mydriver}, {routing, [{RoutingKey, RoutingValue}]}]"},
                new object[] {new V4_2.HelloMessage("mydriver", null, new Dictionary<string, string>()), "HELLO [{user_agent, mydriver}, {routing, []}]"},
                new object[] {new V4_2.HelloMessage("mydriver", null, null), "HELLO [{user_agent, mydriver}, {routing, NULL}]"},
                new object[] {new V4_1.HelloMessage("mydriver", null, new Dictionary<string, string> {{ "RoutingKey", "RoutingValue" }}), "HELLO [{user_agent, mydriver}, {routing, [{RoutingKey, RoutingValue}]}]"},
                new object[] {new V4_1.HelloMessage("mydriver", null, new Dictionary<string, string>()), "HELLO [{user_agent, mydriver}, {routing, []}]"},
                new object[] {new V4_1.HelloMessage("mydriver", null, null), "HELLO [{user_agent, mydriver}, {routing, NULL}]"},
                new object[] {new V3.HelloMessage("mydriver", null), "HELLO [{user_agent, mydriver}]"},
                new object[] {new SuccessMessage(new Dictionary<string, object>()), "SUCCESS []"},
                new object[] {DiscardAll, "DISCARDALL"},
                new object[] {Ignored, "IGNORED"},
                new object[] {PullAll, "PULLALL"},
                new object[]
                {
                    new RecordMessage(new object[] {1, "a string", new[] {3, 4}}),
                    "RECORD [1, a string, [3, 4]]"
                },
                new object[] {ResetMessage.Reset, "RESET"},
                new object[]
                {
                    new V3.RunWithMetadataMessage(new Query("A query", new Dictionary<string, object>
                    {
                        {"key1", 1},
                        {"key2", new[] {2, 4}}
                    }), "my-database", Bookmark.From("bookmark-1"), TransactionConfig.Default, AccessMode.Read),
                    "RUN `A query`, [{key1, 1}, {key2, [2, 4]}] [{bookmarks, [bookmark-1]}, {mode, r}, {db, my-database}]"
                },
                new object[] {new V4.PullMessage(1, 2), "PULL [{n, 2}, {qid, 1}]"},
                new object[] {new V4.PullMessage(2), "PULL [{n, 2}]"},
                new object[] {new V4.DiscardMessage(1, 2), "DISCARD [{n, 2}, {qid, 1}]"},
                new object[] {new V4.DiscardMessage(2), "DISCARD [{n, 2}]"},
            };

            [Theory, MemberData(nameof(MessageData))]
            internal void ShouldPrintTheMessageAsExpected(IMessage message, string expected)
            {
                message.ToString().Should().Be(expected);
            }
        }
    }
}