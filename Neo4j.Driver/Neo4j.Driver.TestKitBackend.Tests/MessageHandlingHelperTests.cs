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

using FluentAssertions;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class MessageHandlingHelperTests
{
    [Fact]
    public void MessageTypeFor_returns_the_handled_message_type()
    {
        MessageHandlingHelper.MessageTypeFor(typeof(TestMessageHandler)).Should().Be<TestMessage>();
    }

    [Fact]
    public void MessageTypeFor_throws_when_the_type_is_not_a_message_handler()
    {
        var act = () => MessageHandlingHelper.MessageTypeFor(typeof(string));

        act.Should().Throw<InvalidOperationException>();
    }

    private record TestMessage : IProtocolMessage;
    private class TestMessageHandler : MessageHandler<TestMessage>
    {
        public override Task<IProtocolMessage?> ProcessAsync(TestMessage message)
        {
            return Task.FromResult<IProtocolMessage?>(null);
        }
    }
}
