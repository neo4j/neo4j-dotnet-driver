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
using Moq;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class PayloadEnvelopeConverterTests
{
    [Fact]
    public void Reads_the_nested_authorization_token_envelope_inside_NewDriverRequest()
    {
        var messageTypeMap = new Mock<IMessageTypeMap>();
        messageTypeMap.Setup(m => m.GetTypeByName("NewDriver")).Returns(typeof(NewDriverRequest));

        var optionsProvider = new JsonOptionsProvider(
        [
            new EnvelopeConverter(messageTypeMap.Object),
            new PayloadEnvelopeConverterFactory(new WireTypeNameProvider())
        ]);
        var serializer = new MessageSerializer(optionsProvider);

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
    public void Rejects_a_payload_whose_name_does_not_match_the_declared_type()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new PayloadEnvelopeConverterFactory(new WireTypeNameProvider()) }
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

        var deserialize = () => JsonSerializer.Deserialize<AuthorizationToken>(json, options);

        deserialize.Should().Throw<TestKitProtocolException>();
    }
}
