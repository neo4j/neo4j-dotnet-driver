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
using Neo4j.Driver.TestKitBackend.Cypher;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Cypher;

public class NativeToCypherMapperTests
{
    private readonly NativeToCypherMapper _mapper = new();

    [Fact]
    public void Maps_null_to_cypher_null()
    {
        _mapper.Map(null).Should().Be(new CypherNull());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Maps_bool_to_cypher_bool(bool value)
    {
        _mapper.Map(value).Should().Be(new CypherBool(value));
    }

    [Fact]
    public void Maps_long_to_cypher_int()
    {
        _mapper.Map(42L).Should().Be(new CypherInt(42));
    }

    [Fact]
    public void Maps_double_to_cypher_float()
    {
        _mapper.Map(1.5).Should().Be(new CypherFloat(1.5));
    }

    [Fact]
    public void Maps_string_to_cypher_string()
    {
        _mapper.Map("hello").Should().Be(new CypherString("hello"));
    }

    [Fact]
    public void Maps_an_empty_list_to_an_empty_cypher_list()
    {
        _mapper.Map(new List<object>())
            .Should().BeOfType<CypherList>()
            .Which.Value.Should().BeEmpty();
    }

    [Fact]
    public void Maps_a_list_of_scalars_to_a_cypher_list()
    {
        _mapper.Map(new List<object> { 1L, "two" })
            .Should().BeOfType<CypherList>()
            .Which.Value.Should().Equal(new CypherInt(1), new CypherString("two"));
    }

    [Fact]
    public void Maps_a_nested_list_recursively()
    {
        var inner = _mapper.Map(new List<object> { new List<object> { true } })
            .Should().BeOfType<CypherList>()
            .Which.Value.Should().ContainSingle()
            .Which.Should().BeOfType<CypherList>().Subject;

        inner.Value.Should().Equal(new CypherBool(true));
    }

    [Fact]
    public void Maps_an_empty_dictionary_to_an_empty_cypher_map()
    {
        _mapper.Map(new Dictionary<string, object>())
            .Should().BeOfType<CypherMap>()
            .Which.Value.Should().BeEmpty();
    }

    [Fact]
    public void Maps_a_dictionary_of_scalars_to_a_cypher_map()
    {
        _mapper.Map(new Dictionary<string, object> { ["a"] = 1L, ["b"] = "two" })
            .Should().BeOfType<CypherMap>()
            .Which.Value.Should().Equal(new Dictionary<string, ICypherValue>
            {
                ["a"] = new CypherInt(1),
                ["b"] = new CypherString("two")
            });
    }

    [Fact]
    public void Maps_a_nested_dictionary_recursively()
    {
        var inner = _mapper.Map(new Dictionary<string, object> { ["outer"] = new Dictionary<string, object> { ["inner"] = true } })
            .Should().BeOfType<CypherMap>()
            .Which.Value.Should().ContainSingle()
            .Which.Value.Should().BeOfType<CypherMap>().Subject;

        inner.Value.Should().Equal(new Dictionary<string, ICypherValue> { ["inner"] = new CypherBool(true) });
    }

    [Fact]
    public void Maps_a_guid_to_cypher_uuid()
    {
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        _mapper.Map(guid).Should().Be(new CypherUuid(guid));
    }

    [Fact]
    public void Maps_a_zoned_date_time_with_an_offset_to_cypher_date_time()
    {
        var zonedDateTime = new ZonedDateTime(2022, 6, 7, 11, 52, 5, 0, Zone.Of(7200));

        _mapper.Map(zonedDateTime).Should().Be(new CypherDateTime(2022, 6, 7, 11, 52, 5, 0, 7200));
    }

    [Fact]
    public void Maps_a_zoned_date_time_with_a_named_zone_to_cypher_date_time()
    {
        var zonedDateTime = new ZonedDateTime(2022, 6, 7, 11, 52, 5, 0, Zone.Of("Europe/Stockholm"));

        _mapper.Map(zonedDateTime).Should().Be(new CypherDateTime(2022, 6, 7, 11, 52, 5, 0, 7200, "Europe/Stockholm"));
    }

    [Fact]
    public void Maps_an_integer_vector_to_cypher_vector()
    {
        var vector = Vector.Create<sbyte>([1, -1]);

        _mapper.Map(vector).Should().Be(new CypherVector("i8", "01 ff"));
    }

    [Fact]
    public void Maps_a_float_vector_to_cypher_vector()
    {
        var vector = Vector.Create<float>([1.0f]);

        _mapper.Map(vector).Should().Be(new CypherVector("f32", "3f 80 00 00"));
    }

    [Fact]
    public void Maps_an_empty_vector_to_cypher_vector()
    {
        var vector = Vector.Create<sbyte>([]);

        _mapper.Map(vector).Should().Be(new CypherVector("i8", ""));
    }

    [Fact]
    public void Throws_for_an_unmapped_native_type_naming_the_type()
    {
        var act = () => _mapper.Map(TimeSpan.FromSeconds(1));

        act.Should().Throw<NotSupportedException>().WithMessage("*TimeSpan*");
    }
}
