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
using Moq;
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class MessageTypeMapTests
{
    private readonly Mock<IProtocolMessageTypesProvider> _protocolTypesProvider = new();

    public MessageTypeMapTests()
    {
        _protocolTypesProvider
            .Setup(p => p.GetTypes())
            .Returns(new[] { typeof(FirstSampleRequest), typeof(SecondSampleRequest) });
    }

    private MessageTypeMap Subject()
    {
        return new MessageTypeMap(_protocolTypesProvider.Object, new WireTypeNameProvider());
    }

    [Fact]
    public void GetTypeByName_throws_for_an_unknown_message_name()
    {
        var lookup = () => Subject().GetTypeByName(nameof(NotAMessage));
        lookup.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void GetTypeByName_finds_types_by_their_inbound_wire_name()
    {
        var result = Subject().GetTypeByName("FirstSample");
        result.Should().BeSameAs(typeof(FirstSampleRequest));
    }

    private record FirstSampleRequest : IProtocolMessage;

    private record SecondSampleRequest : IProtocolMessage;

    private class NotAMessage;
}
