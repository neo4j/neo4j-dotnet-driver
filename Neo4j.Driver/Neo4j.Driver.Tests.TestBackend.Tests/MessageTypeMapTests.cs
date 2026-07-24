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
using Neo4j.Driver.Tests.TestBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests;

public class MessageTypeMapTests
{
    [Fact]
    public void GetTypeByName_throws_for_an_unknown_message_name()
    {
        Type[] messageTypes = [typeof(FirstSampleMessage), typeof(SecondSampleMessage)];
        var map = new MessageTypeMap(messageTypes);
        var lookup = () => map.GetTypeByName(nameof(NotAMessage));
        lookup.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void GetTypeByName_finds_type_by_name()
    {
        Type[] messageTypes = [typeof(FirstSampleMessage), typeof(SecondSampleMessage)];
        var map = new MessageTypeMap(messageTypes);
        var result = map.GetTypeByName("FirstSampleMessage");
        result.Should().BeSameAs(typeof(FirstSampleMessage));
    }


    private record FirstSampleMessage : IProtocolMessage;

    private record SecondSampleMessage : IProtocolMessage;

    private class NotAMessage;
}
