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
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class MessageTypeMapTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<MessageTypeMap>();

    public MessageTypeMapTests()
    {
        _autoMocker.GetMock<IProtocolMessageTypesProvider>()
            .Setup(p => p.GetTypes())
            .Returns(new[] { typeof(FirstSampleRequest), typeof(SecondSampleRequest) });
        _autoMocker.Use<IWireTypeNameProvider>(new WireTypeNameProvider());
    }

    private MessageTypeMap Subject()
    {
        return _autoMocker.CreateInstance<MessageTypeMap>();
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
