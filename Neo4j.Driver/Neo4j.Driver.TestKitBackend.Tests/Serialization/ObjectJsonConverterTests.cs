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
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class ObjectJsonConverterTests
{
    private static JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions { Converters = { new ObjectJsonConverter() } };
    }

    [Fact]
    public void Reads_untyped_dictionary_values_as_native_scalars()
    {
        const string json = """{"sky?":"no","my eyes":0.1,"da be dee da be daa?":true}""";

        var values = JsonSerializer.Deserialize<Dictionary<string, object>>(json, Options());

        values.Should().Equal(
            new Dictionary<string, object>
            {
                ["sky?"] = "no",
                ["my eyes"] = 0.1,
                ["da be dee da be daa?"] = true
            });
    }

    [Fact]
    public void Reads_a_null_value_as_null()
    {
        var value = JsonSerializer.Deserialize<object>("null", Options());

        value.Should().BeNull();
    }

    [Fact]
    public void Reads_a_whole_number_as_a_long_and_a_fractional_number_as_a_double()
    {
        JsonSerializer.Deserialize<object>("42", Options()).Should().BeOfType<long>().And.Be(42L);
        JsonSerializer.Deserialize<object>("42.5", Options()).Should().BeOfType<double>().And.Be(42.5);
    }

    [Fact]
    public void Reads_nested_dictionaries_and_lists_round_tripping_what_write_produces()
    {
        var value = new Dictionary<string, object>
        {
            ["EstimatedRows"] = 12L,
            ["Details"] = new List<object> { "n", "m" }
        };

        var json = JsonSerializer.Serialize<object?>(value, Options());
        var roundTripped = JsonSerializer.Deserialize<Dictionary<string, object>>(json, Options());

        roundTripped.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void Writes_native_scalars_as_untyped_json()
    {
        JsonSerializer.Serialize<object?>("no", Options()).Should().Be("\"no\"");
        JsonSerializer.Serialize<object?>(true, Options()).Should().Be("true");
        JsonSerializer.Serialize<object?>((object?)null, Options()).Should().Be("null");
    }

    [Fact]
    public void Writes_nested_lists_and_dictionaries_as_untyped_json()
    {
        // Shape of a real query plan's Args: a map of scalars, some of which are lists.
        var value = new Dictionary<string, object>
        {
            ["EstimatedRows"] = 12L,
            ["Details"] = new List<object> { "n", "m" }
        };

        var json = JsonSerializer.Serialize<object?>(value, Options());

        json.Should().Be("""{"EstimatedRows":12,"Details":["n","m"]}""");
    }
}
