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
using Neo4j.Driver.Tests.TestBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.Tests;

public class EnvelopeDeserializationTests
{
    private static readonly MessageRegistry Registry = new(new[] { typeof(SampleRequest) });

    private static JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters = { new EnvelopeConverter(Registry) }
        };
    }

    [Fact]
    public void Dispatches_by_name_to_the_registered_record_type()
    {
        const string json =
            """
            {
                "name": "SampleRequest",
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
                "name": "SampleRequest",
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
                "name": "SampleRequest"
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
                "name": "SampleRequest",
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

        var deserialize = () => JsonSerializer.Deserialize<IProtocolMessage>(json, Options());

        deserialize.Should().Throw<TestKitProtocolException>();
    }

    private record SampleRequest : IProtocolMessage
    {
        public string Uri { get; init; } = "";
        public string? UserAgent { get; init; }
    }
}
