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
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class MessageSerializerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<MessageSerializer>();

    private IMessageSerializer Subject()
    {
        _autoMocker.GetMock<IMessageTypeMap>()
            .Setup(m => m.GetTypeByName("Sample"))
            .Returns(typeof(Sample));
        _autoMocker.Use<IJsonOptionsProvider>(
            new JsonOptionsProvider([new EnvelopeConverter(_autoMocker.Get<IMessageTypeMap>())]));
        return _autoMocker.CreateInstance<MessageSerializer>();
    }

    [Fact]
    public void Serialize_wraps_the_message_in_its_envelope()
    {
        var json = Subject().Serialize(new Sample { Value = "x" });

        json.Should().Be("""{"name":"Sample","data":{"value":"x"}}""");
    }

    [Fact]
    public void Deserialize_reads_an_envelope_into_its_message_type()
    {
        var message = Subject().Deserialize("""{"name":"Sample","data":{"value":"x"}}""");

        message.Should().BeOfType<Sample>().Which.Value.Should().Be("x");
    }

    private record Sample : IProtocolMessage
    {
        public string Value { get; init; } = "";
    }
}
