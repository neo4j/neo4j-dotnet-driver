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

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Cypher;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Cypher;

public class CypherFloatTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<CypherValueConverter>();

    private JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { _autoMocker.CreateInstance<CypherValueConverter>() }
        };
    }

    [Theory]
    [InlineData(1.5, "1.5")]
    [InlineData(0.0, "0")]
    [InlineData(double.PositiveInfinity, "\"+Infinity\"")]
    [InlineData(double.NegativeInfinity, "\"-Infinity\"")]
    [InlineData(double.NaN, "\"NaN\"")]
    public void Writes_finite_values_as_numbers_and_non_finite_values_as_wire_strings(
        double value,
        string expectedJsonValue)
    {
        var json = JsonSerializer.Serialize<ICypherValue>(new CypherFloat { Value = value }, Options());

        var expected = """{"name":"CypherFloat","data":{"value":""" + expectedJsonValue + "}}";
        json.Should().Be(expected);
    }

    [Fact]
    public void Reads_a_finite_json_number()
    {
        _autoMocker.GetMock<ICypherValueTypeMap>()
            .Setup(m => m.GetTypeByName("CypherFloat"))
            .Returns(typeof(CypherFloat));

        const string json = """{"name":"CypherFloat","data":{"value":1.5}}""";

        var value = JsonSerializer.Deserialize<ICypherValue>(json, Options());

        value.Should().BeOfType<CypherFloat>().Which.Value.Should().Be(1.5);
    }

    [Theory]
    [InlineData("+Infinity", double.PositiveInfinity)]
    [InlineData("-Infinity", double.NegativeInfinity)]
    [InlineData("NaN", double.NaN)]
    public void Reads_a_non_finite_wire_string(string literal, double expected)
    {
        _autoMocker.GetMock<ICypherValueTypeMap>()
            .Setup(m => m.GetTypeByName("CypherFloat"))
            .Returns(typeof(CypherFloat));

        var json = """{"name":"CypherFloat","data":{"value":""" + "\"" + literal + "\"}}";

        var value = JsonSerializer.Deserialize<ICypherValue>(json, Options());

        value.Should().BeOfType<CypherFloat>().Which.Value.Should().Be(expected);
    }
}
