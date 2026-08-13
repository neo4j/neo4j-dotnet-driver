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
using FluentAssertions;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class ProtocolEnvelopeTests
{
    private record InitPropertyContainer
    {
        public AuthorizationToken? Auth { get; init; }
    }

    private record PositionalContainer(AuthorizationToken Auth, string Tag);

    [ProtocolEnvelope("Token")]
    private record RenamedToken(string Flavour);

    private record RenamedContainer
    {
        public RenamedToken? Auth { get; init; }
    }

    [ProtocolEnvelope]
    private record InnerBundle(AuthorizationToken Auth, string Label);

    private record NestedContainer
    {
        public InnerBundle? Bundle { get; init; }
    }

    private static JsonSerializerOptions RealOptions()
    {
        return new JsonOptionsProvider([]).GetOptions();
    }

    private const string AuthEnvelope =
        """{"name":"AuthorizationToken","data":{"scheme":"basic","principal":"neo4j","credentials":"pass"}}""";

    [Fact]
    public void A_property_of_an_enveloped_type_deserializes_to_the_bare_payload_type()
    {
        var container = JsonSerializer.Deserialize<InitPropertyContainer>(
            $$"""{"auth":{{AuthEnvelope}}}""",
            RealOptions())!;

        container.Auth.Should().Be(new AuthorizationToken("basic", "neo4j", "pass"));
    }

    [Fact]
    public void A_positional_parameter_of_an_enveloped_type_deserializes_through_constructor_binding()
    {
        var container = JsonSerializer.Deserialize<PositionalContainer>(
            $$"""{"auth":{{AuthEnvelope}},"tag":"t1"}""",
            RealOptions())!;

        container.Auth.Should().Be(new AuthorizationToken("basic", "neo4j", "pass"));
        container.Tag.Should().Be("t1");
    }

    [Fact]
    public void An_absent_property_of_an_enveloped_type_is_null()
    {
        var container = JsonSerializer.Deserialize<InitPropertyContainer>("{}", RealOptions())!;

        container.Auth.Should().BeNull();
    }

    [Fact]
    public void The_attribute_name_argument_overrides_the_expected_envelope_name()
    {
        var container = JsonSerializer.Deserialize<RenamedContainer>(
            """{"auth":{"name":"Token","data":{"flavour":"salty"}}}""",
            RealOptions())!;

        container.Auth.Should().Be(new RenamedToken("salty"));
    }

    [Fact]
    public void A_mismatched_envelope_name_is_rejected()
    {
        var act = () => JsonSerializer.Deserialize<InitPropertyContainer>(
            """{"auth":{"name":"SomethingElse","data":{"scheme":"basic"}}}""",
            RealOptions());

        act.Should().Throw<TestKitProtocolException>().WithMessage("*AuthorizationToken*SomethingElse*");
    }

    [Fact]
    public void A_property_of_an_enveloped_type_serializes_with_the_envelope_shape()
    {
        var container = new InitPropertyContainer { Auth = new AuthorizationToken("basic", "neo4j", "pass") };

        var json = JsonSerializer.Serialize(container, RealOptions());

        json.Should().Be($$"""{"auth":{{AuthEnvelope}}}""");
    }

    [Fact]
    public void An_enveloped_type_nested_inside_another_enveloped_type_round_trips()
    {
        var container = JsonSerializer.Deserialize<NestedContainer>(
            """
            {"bundle":{"name":"InnerBundle","data":{
                "auth":{"name":"AuthorizationToken","data":{"scheme":"basic","principal":"neo4j","credentials":"pass"}},
                "label":"L"}}}
            """,
            RealOptions())!;

        container.Bundle.Should().NotBeNull();
        container.Bundle!.Auth.Should().Be(new AuthorizationToken("basic", "neo4j", "pass"));
        container.Bundle.Label.Should().Be("L");
    }
}
