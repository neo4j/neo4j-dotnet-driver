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
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class WireTypeConverterTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<WireTypeConverterFactory>();

    public WireTypeConverterTests()
    {
        _autoMocker.Use<IWireTypeNameProvider>(new WireTypeNameProvider());
    }

    [Fact]
    public void Reads_the_nested_authorization_token_envelope_inside_NewDriverRequest()
    {
        _autoMocker.GetMock<IMessageTypeMap>()
            .Setup(m => m.GetTypeByName("NewDriver"))
            .Returns(typeof(NewDriverRequest));

        _autoMocker.Use<IJsonOptionsProvider>(new JsonOptionsProvider(
        [
            new EnvelopeConverter(_autoMocker.Get<IMessageTypeMap>()),
            _autoMocker.CreateInstance<WireTypeConverterFactory>()
        ]));
        var serializer = _autoMocker.CreateInstance<MessageSerializer>();

        const string json =
            """
            {
                "name": "NewDriver",
                "data": {
                    "uri": "neo4j://x",
                    "authorizationToken": {
                        "name": "AuthorizationToken",
                        "data": {
                            "scheme": "basic",
                            "principal": "neo4j",
                            "credentials": "secret"
                        }
                    }
                }
            }
            """;

        var message = serializer.Deserialize(json);

        message.Should().BeOfType<NewDriverRequest>()
            .Which.AuthorizationToken.Should().Be(new AuthorizationToken("basic", "neo4j", "secret"));
    }

    [Fact]
    public void Reads_a_wire_type_nested_inside_another_wire_type()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { _autoMocker.CreateInstance<WireTypeConverterFactory>() }
        };

        const string json =
            """
            {
                "name": "OuterTestWireType",
                "data": {
                    "token": {
                        "name": "AuthorizationToken",
                        "data": {
                            "scheme": "basic",
                            "principal": "neo4j",
                            "credentials": "secret"
                        }
                    }
                }
            }
            """;

        var outer = JsonSerializer.Deserialize<IWireType<OuterTestWireType>>(json, options);

        outer!.Value.Token.Should().Be(new AuthorizationToken("basic", "neo4j", "secret"));
    }

    [Fact]
    public void Writes_a_wire_type_as_a_named_envelope()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { _autoMocker.CreateInstance<WireTypeConverterFactory>() }
        };

        IWireType<AuthorizationToken> token = new AuthorizationToken("basic", "neo4j", "secret", "myrealm");
        var json = JsonSerializer.Serialize(token, options);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("name").GetString().Should().Be("AuthorizationToken");
        var data = document.RootElement.GetProperty("data");
        data.GetProperty("scheme").GetString().Should().Be("basic");
        data.GetProperty("principal").GetString().Should().Be("neo4j");
        data.GetProperty("credentials").GetString().Should().Be("secret");
        data.GetProperty("realm").GetString().Should().Be("myrealm");
    }

    [Fact]
    public void Omits_a_null_realm_when_writing_an_authorization_token()
    {
        // Testkit compares the token attribute-by-attribute against what its get_auth returned,
        // so a realm it never sent must stay absent, not become null.
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { _autoMocker.CreateInstance<WireTypeConverterFactory>() }
        };

        IWireType<AuthorizationToken> token = new AuthorizationToken("basic", "neo4j", "secret");
        var json = JsonSerializer.Serialize(token, options);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("data").TryGetProperty("realm", out _).Should().BeFalse();
    }

    [Fact]
    public void Rejects_a_wire_type_whose_name_does_not_match_the_declared_type()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { _autoMocker.CreateInstance<WireTypeConverterFactory>() }
        };

        const string json =
            """
            {
                "name": "WrongName",
                "data": {
                    "scheme": "basic",
                    "principal": "neo4j",
                    "credentials": "secret"
                }
            }
            """;

        var deserialize = () => JsonSerializer.Deserialize<IWireType<AuthorizationToken>>(json, options);

        deserialize.Should().Throw<TestKitProtocolException>();
    }
}

internal record OuterTestWireType : IWireType<OuterTestWireType>
{
    public IWireType<AuthorizationToken>? Token { get; init; }
}
