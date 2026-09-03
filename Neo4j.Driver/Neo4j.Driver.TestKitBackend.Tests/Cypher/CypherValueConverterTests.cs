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
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Cypher;

public class CypherValueConverterTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<CypherValueConverter>();

    public CypherValueConverterTests()
    {
        _autoMocker.GetMock<ICypherValueTypeMap>()
            .Setup(m => m.GetTypeByName("CypherInt"))
            .Returns(typeof(CypherInt));
    }

    private JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters = { _autoMocker.CreateInstance<CypherValueConverter>() }
        };
    }

    [Fact]
    public void Dispatches_by_name_to_the_registered_record_type()
    {
        const string json =
            """
            {
                "name": "CypherInt",
                "data": {
                    "value": 42
                }
            }
            """;

        var value = JsonSerializer.Deserialize<ICypherValue>(json, Options());

        value.Should().BeOfType<CypherInt>().Which.Value.Should().Be(42);
    }

    [Fact]
    public void Treats_missing_data_as_empty_object()
    {
        _autoMocker.GetMock<ICypherValueTypeMap>()
            .Setup(m => m.GetTypeByName("CypherNull"))
            .Returns(typeof(CypherNull));

        const string json =
            """
            {
                "name": "CypherNull"
            }
            """;

        var value = JsonSerializer.Deserialize<ICypherValue>(json, Options());

        value.Should().BeOfType<CypherNull>();
    }

    [Fact]
    public void Rejects_unknown_member_in_data()
    {
        const string json =
            """
            {
                "name": "CypherInt",
                "data": {
                    "value": 42,
                    "bogus": true
                }
            }
            """;

        var deserialize = () => JsonSerializer.Deserialize<ICypherValue>(json, Options());

        deserialize.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void Rejects_unknown_cypher_type_name()
    {
        const string json =
            """
            {
                "name": "NoSuchCypherType",
                "data": {}
            }
            """;

        _autoMocker.GetMock<ICypherValueTypeMap>()
            .Setup(m => m.GetTypeByName("NoSuchCypherType"))
            .Throws(() => new TestKitProtocolException("Test"));

        var deserialize = () => JsonSerializer.Deserialize<ICypherValue>(json, Options());

        deserialize.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void Writes_the_outbound_wire_name_and_camelCase_data()
    {
        var json = JsonSerializer.Serialize<ICypherValue>(new CypherString("hi"), Options());

        json.Should().Be("""{"name":"CypherString","data":{"value":"hi"}}""");
    }

    [Fact]
    public void Reads_a_cypher_list_of_scalar_values()
    {
        _autoMocker.GetMock<ICypherValueTypeMap>()
            .Setup(m => m.GetTypeByName("CypherList"))
            .Returns(typeof(CypherList));

        const string json =
            """
            {
                "name": "CypherList",
                "data": {
                    "value": [
                        { "name": "CypherInt", "data": { "value": 1 } },
                        { "name": "CypherInt", "data": { "value": 2 } }
                    ]
                }
            }
            """;

        var value = JsonSerializer.Deserialize<ICypherValue>(json, Options());

        value.Should().BeOfType<CypherList>()
            .Which.Value.Should().Equal(new CypherInt(1), new CypherInt(2));
    }
}
