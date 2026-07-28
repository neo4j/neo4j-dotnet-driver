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

public class MessageSerializerTests
{
    private readonly Mock<IMessageTypeMap> _messageTypeMap = new();
    private readonly Mock<IResponseWireNameProvider> _wireNameProvider = new();

    private IMessageSerializer Subject()
    {
        _messageTypeMap.Setup(m => m.GetTypeByName("SampleMessage")).Returns(typeof(Sample));
        _wireNameProvider.Setup(p => p.GetResponseWireName(typeof(Sample))).Returns("SampleMessage");
        var optionsProvider = new JsonOptionsProvider(
            [new EnvelopeConverter(_messageTypeMap.Object, _wireNameProvider.Object)]);
        return new MessageSerializer(optionsProvider);
    }

    [Fact]
    public void Serialize_wraps_the_message_in_its_envelope()
    {
        var json = Subject().Serialize(new Sample { Value = "x" });

        json.Should().Be("""{"name":"SampleMessage","data":{"value":"x"}}""");
    }

    [Fact]
    public void Deserialize_reads_an_envelope_into_its_message_type()
    {
        var message = Subject().Deserialize("""{"name":"SampleMessage","data":{"value":"x"}}""");

        message.Should().BeOfType<Sample>().Which.Value.Should().Be("x");
    }

    private record Sample : IProtocolMessage
    {
        public string Value { get; init; } = "";
    }
}
