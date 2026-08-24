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

using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class EnvelopeConverterTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<EnvelopeConverter>();

    public EnvelopeConverterTests()
    {
        _autoMocker.Use<IStoredObjectFieldTransformer>(new StoredObjectFieldTransformer());
        _autoMocker.GetMock<IMessageTypeMap>()
            .Setup(m => m.GetTypeByName("Sample"))
            .Returns(typeof(SampleRequest));
    }

    private JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters = { _autoMocker.CreateInstance<EnvelopeConverter>() }
        };
    }

    [Fact]
    public void Dispatches_by_name_to_the_registered_record_type()
    {
        const string json =
            """
            {
                "name": "Sample",
                "data": {
                    "uri": "neo4j://x"
                }
            }
            """;

        var message = JsonSerializer.Deserialize<IProtocolMessage>(json, Options());

        message.Should().BeOfType<SampleRequest>();
    }

    [Fact]
    public void Binds_camelCase_wire_members_to_pascal_case_properties()
    {
        const string json =
            """
            {
                "name": "Sample",
                "data": {
                    "uri": "neo4j://x",
                    "userAgent": "ua/1"
                }
            }
            """;

        var message = JsonSerializer.Deserialize<IProtocolMessage>(json, Options());

        message.Should().BeOfType<SampleRequest>()
            .Which.UserAgent.Should().Be("ua/1");
    }

    [Fact]
    public void Treats_missing_data_as_empty_object()
    {
        const string json =
            """
            {
                "name": "Sample"
            }
            """;

        var message = JsonSerializer.Deserialize<IProtocolMessage>(json, Options());

        message.Should().BeOfType<SampleRequest>();
    }

    [Fact]
    public void Rejects_unknown_member_in_data()
    {
        const string json =
            """
            {
                "name": "Sample",
                "data": {
                    "uri": "neo4j://x",
                    "bogus": true
                }
            }
            """;

        var deserialize = () => JsonSerializer.Deserialize<IProtocolMessage>(json, Options());

        deserialize.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void Rejects_unknown_message_name()
    {
        const string json =
            """
            {
                "name": "NoSuchMessage",
                "data": {}
            }
            """;

        _autoMocker.GetMock<IMessageTypeMap>()
            .Setup(m => m.GetTypeByName("NoSuchMessage"))
            .Throws(() => new TestKitProtocolException("Test"));

        var deserialize = () => JsonSerializer.Deserialize<IProtocolMessage>(json, Options());

        deserialize.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void Rejects_a_null_data_object()
    {
        const string json =
            """
            {
                "name": "Sample",
                "data": null
            }
            """;

        var deserialize = () => JsonSerializer.Deserialize<IProtocolMessage>(json, Options());

        deserialize.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void Rejects_a_non_object_envelope()
    {
        const string json = "\"just a string\"";

        var deserialize = () => JsonSerializer.Deserialize<IProtocolMessage>(json, Options());

        deserialize.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void Wraps_a_non_JsonException_deserialization_failure_with_the_wire_type_name()
    {
        _autoMocker.GetMock<IMessageTypeMap>()
            .Setup(m => m.GetTypeByName("Malformed"))
            .Returns(typeof(MalformedRequest));

        var options = new JsonOptionsProvider(
            [_autoMocker.CreateInstance<EnvelopeConverter>()],
            Mock.Of<IObjectStore>()).GetOptions();

        const string json = """{"name":"Malformed","data":{"value":"x"}}""";

        var deserialize = () => JsonSerializer.Deserialize<IProtocolMessage>(json, options);

        deserialize.Should().Throw<TestKitProtocolException>()
            .WithMessage("*Malformed*")
            .Which.InnerException.Should().NotBeNull();
    }

    [Fact]
    public void Writes_the_outbound_wire_name_and_camelCase_data()
    {
        var json = JsonSerializer.Serialize<IProtocolMessage>(new SampleResponse { Value = "x" }, Options());

        json.Should().Be("""{"name":"Sample","data":{"value":"x"}}""");
    }

    [Fact]
    public void Serializes_BackendError_with_a_msg_field()
    {
        var json = JsonSerializer.Serialize<IProtocolMessage>(new BackendErrorResponse { Msg = "boom" }, Options());

        json.Should().Be("""{"name":"BackendError","data":{"msg":"boom"}}""");
    }

    [Fact]
    public void Writes_nested_protocol_message_properties_as_envelopes()
    {
        var message = new SampleEnvelope { Inner = new SampleResponse { Value = "y" } };

        var json = JsonSerializer.Serialize<IProtocolMessage>(message, Options());

        json.Should().Be(
            """{"name":"SampleEnvelope","data":{"inner":{"name":"Sample","data":{"value":"y"}}}}""");
    }

    [Fact]
    public void Concrete_protocol_message_types_nested_in_a_list_do_not_get_their_own_envelope()
    {
        var message = new SampleListEnvelope { Items = [new SampleResponse { Value = "y" }] };

        var json = JsonSerializer.Serialize<IProtocolMessage>(message, Options());

        json.Should().Be("""{"name":"SampleListEnvelope","data":{"items":[{"value":"y"}]}}""");
    }

    [Fact]
    public void Reads_nested_envelopes_into_protocol_message_properties()
    {
        _autoMocker.GetMock<IMessageTypeMap>()
            .Setup(m => m.GetTypeByName("SampleEnvelope"))
            .Returns(typeof(SampleEnvelope));

        const string json =
            """
            {
                "name": "SampleEnvelope",
                "data": {
                    "inner": {
                        "name": "Sample",
                        "data": { "uri": "neo4j://y" }
                    }
                }
            }
            """;

        var message = JsonSerializer.Deserialize<IProtocolMessage>(json, Options());

        message.Should().BeOfType<SampleEnvelope>()
            .Which.Inner.Should().BeOfType<SampleRequest>()
            .Which.Uri.Should().Be("neo4j://y");
    }

    private record SampleRequest : IProtocolMessage
    {
        public string Uri { get; init; } = "";
        public string? UserAgent { get; init; }
    }

    private record SampleResponse : IProtocolMessage
    {
        public string Value { get; init; } = "";
    }

    private record SampleEnvelope : IProtocolMessage
    {
        public IProtocolMessage Inner { get; init; } = null!;
    }

    private record SampleListEnvelope : IProtocolMessage
    {
        public IReadOnlyList<SampleResponse> Items { get; init; } = [];
    }

    private record MalformedRequest : IProtocolMessage
    {
        public string Value { get; }

        public MalformedRequest(string value)
        {
            Value = value;
        }

        public MalformedRequest(string value, string extra)
        {
            Value = value;
        }
    }
}
