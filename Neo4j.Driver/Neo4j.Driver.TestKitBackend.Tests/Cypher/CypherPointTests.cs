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

public class CypherPointTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<CypherValueConverter>();

    public CypherPointTests()
    {
        _autoMocker.GetMock<ICypherValueTypeMap>()
            .Setup(m => m.GetTypeByName("CypherPoint"))
            .Returns(typeof(CypherPoint));
    }

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

    [Fact]
    public void Reads_non_finite_coordinates_from_wire_strings()
    {
        const string json =
            """
            {
                "name": "CypherPoint",
                "data": { "system": "cartesian", "x": "-Infinity", "y": "+Infinity", "z": "NaN" }
            }
            """;

        var value = JsonSerializer.Deserialize<ICypherValue>(json, Options());

        value.Should().Be(
            new CypherPoint
            {
                System = "cartesian",
                X = double.NegativeInfinity,
                Y = double.PositiveInfinity,
                Z = double.NaN
            });
    }

    [Fact]
    public void Reads_a_two_dimensional_point_with_a_null_z()
    {
        const string json =
            """
            {
                "name": "CypherPoint",
                "data": { "system": "wgs84", "x": 1.5, "y": -2.5, "z": null }
            }
            """;

        var value = JsonSerializer.Deserialize<ICypherValue>(json, Options());

        value.Should().Be(new CypherPoint { System = "wgs84", X = 1.5, Y = -2.5, Z = null });
    }

    [Fact]
    public void Writes_non_finite_coordinates_as_wire_strings()
    {
        var point = new CypherPoint
        {
            System = "wgs84",
            X = double.NegativeInfinity,
            Y = double.PositiveInfinity,
            Z = double.NaN
        };

        var json = JsonSerializer.Serialize<ICypherValue>(point, Options());

        json.Should().Be(
            """{"name":"CypherPoint","data":{"system":"wgs84","x":"-Infinity","y":"+Infinity","z":"NaN"}}""");
    }
}
