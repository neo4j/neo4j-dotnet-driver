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

#pragma warning disable CS0618 // Id/StartNodeId/EndNodeId are obsolete but still part of the wire contract.

using FluentAssertions;
using FluentAssertions.Equivalency;
using Moq;
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

        _mapper.Map(guid).Should().Be(new CypherUUID(guid));
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
    public void Maps_a_local_date_time_to_cypher_date_time_without_offset_or_timezone()
    {
        var localDateTime = new LocalDateTime(2022, 6, 7, 11, 52, 5, 0);

        _mapper.Map(localDateTime).Should().Be(new CypherDateTime(2022, 6, 7, 11, 52, 5, 0));
    }

    [Fact]
    public void Maps_a_local_date_to_cypher_date()
    {
        var date = new LocalDate(2022, 6, 7);

        _mapper.Map(date).Should().Be(new CypherDate(2022, 6, 7));
    }

    [Fact]
    public void Maps_a_local_time_to_cypher_time_without_offset()
    {
        var time = new LocalTime(11, 52, 5, 0);

        _mapper.Map(time).Should().Be(new CypherTime(11, 52, 5, 0));
    }

    [Fact]
    public void Maps_an_offset_time_to_cypher_time_with_offset()
    {
        var time = new OffsetTime(11, 52, 5, 0, 7200);

        _mapper.Map(time).Should().Be(new CypherTime(11, 52, 5, 0, 7200));
    }

    [Fact]
    public void Maps_a_duration_to_cypher_duration()
    {
        var duration = new Duration(1, 2, 3, 4);

        _mapper.Map(duration).Should().Be(new CypherDuration(1, 2, 3, 4));
    }

    [Fact]
    public void Maps_bytes_to_cypher_bytes()
    {
        _mapper.Map(new byte[] { 0x01, 0xff }).Should().Be(new CypherBytes("01 ff"));
    }

    [Fact]
    public void Maps_a_2d_cartesian_point_to_cypher_point()
    {
        var point = new Point(7203, 1.0, 2.0);

        _mapper.Map(point).Should().Be(new CypherPoint("cartesian", 1.0, 2.0, null));
    }

    [Fact]
    public void Maps_a_3d_wgs84_point_to_cypher_point()
    {
        var point = new Point(4979, 1.0, 2.0, 3.0);

        _mapper.Map(point).Should().Be(new CypherPoint("wgs84", 1.0, 2.0, 3.0));
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
    public void Maps_an_unsupported_type_to_cypher_unsupported_type()
    {
        var unsupported = new UnsupportedType("encrypted_value", 6, 10, "test message");

        _mapper.Map(unsupported).Should().Be(new CypherUnsupportedType("encrypted_value", "6.10", "test message"));
    }

    [Fact]
    public void Maps_an_unsupported_type_with_no_message_to_cypher_unsupported_type()
    {
        var unsupported = new UnsupportedType("encrypted_value", 6, 10, null);

        _mapper.Map(unsupported).Should().Be(new CypherUnsupportedType("encrypted_value", "6.10", null));
    }

    [Fact]
    public void Maps_a_node_to_cypher_node()
    {
        var node = new Mock<INode>();
        node.SetupGet(n => n.Id).Returns(1L);
        node.SetupGet(n => n.Labels).Returns(new List<string> { "Thing" });
        node.SetupGet(n => n.Properties).Returns(new Dictionary<string, object> { ["uid"] = "abc" });
        node.SetupGet(n => n.ElementId).Returns("element-id-1");

        _mapper.Map(node.Object).Should().BeEquivalentTo(
            new CypherNode(
                1L,
                new CypherList([new CypherString("Thing")]),
                new CypherMap(new Dictionary<string, ICypherValue> { ["uid"] = new CypherString("abc") }),
                "element-id-1"),
            ComparingCypherRecordsByMembers);
    }

    [Fact]
    public void Maps_a_relationship_to_cypher_relationship()
    {
        var relationship = new Mock<IRelationship>();
        relationship.SetupGet(r => r.Id).Returns(1L);
        relationship.SetupGet(r => r.StartNodeId).Returns(2L);
        relationship.SetupGet(r => r.EndNodeId).Returns(3L);
        relationship.SetupGet(r => r.Type).Returns("KNOWS");
        relationship.SetupGet(r => r.Properties).Returns(new Dictionary<string, object> { ["since"] = 2020L });
        relationship.SetupGet(r => r.ElementId).Returns("rel-1");
        relationship.SetupGet(r => r.StartNodeElementId).Returns("node-2");
        relationship.SetupGet(r => r.EndNodeElementId).Returns("node-3");

        _mapper.Map(relationship.Object).Should().BeEquivalentTo(
            new CypherRelationship(
                1L,
                2L,
                3L,
                "KNOWS",
                new CypherMap(new Dictionary<string, ICypherValue> { ["since"] = new CypherInt(2020) }),
                "rel-1",
                "node-2",
                "node-3"),
            ComparingCypherRecordsByMembers);
    }

    [Fact]
    public void Maps_a_path_to_cypher_path()
    {
        var node = new Mock<INode>();
        node.SetupGet(n => n.Id).Returns(1L);
        node.SetupGet(n => n.Labels).Returns(new List<string>());
        node.SetupGet(n => n.Properties).Returns(new Dictionary<string, object>());
        node.SetupGet(n => n.ElementId).Returns("node-1");

        var relationship = new Mock<IRelationship>();
        relationship.SetupGet(r => r.Id).Returns(2L);
        relationship.SetupGet(r => r.StartNodeId).Returns(1L);
        relationship.SetupGet(r => r.EndNodeId).Returns(1L);
        relationship.SetupGet(r => r.Type).Returns("SELF");
        relationship.SetupGet(r => r.Properties).Returns(new Dictionary<string, object>());
        relationship.SetupGet(r => r.ElementId).Returns("rel-2");
        relationship.SetupGet(r => r.StartNodeElementId).Returns("node-1");
        relationship.SetupGet(r => r.EndNodeElementId).Returns("node-1");

        var path = new Mock<IPath>();
        path.SetupGet(p => p.Nodes).Returns(new List<INode> { node.Object });
        path.SetupGet(p => p.Relationships).Returns(new List<IRelationship> { relationship.Object });

        var expectedNode = new CypherNode(
            1L,
            new CypherList([]),
            new CypherMap(new Dictionary<string, ICypherValue>()),
            "node-1");

        var expectedRelationship = new CypherRelationship(
            2L,
            1L,
            1L,
            "SELF",
            new CypherMap(new Dictionary<string, ICypherValue>()),
            "rel-2",
            "node-1",
            "node-1");

        _mapper.Map(path.Object).Should().BeEquivalentTo(
            new CypherPath(new CypherList([expectedNode]), new CypherList([expectedRelationship])),
            ComparingCypherRecordsByMembers);
    }

    private static EquivalencyAssertionOptions<T> ComparingCypherRecordsByMembers<T>(EquivalencyAssertionOptions<T> options)
    {
        return options
            .ComparingByMembers<CypherNode>()
            .ComparingByMembers<CypherRelationship>()
            .ComparingByMembers<CypherPath>()
            .ComparingByMembers<CypherMap>()
            .ComparingByMembers<CypherList>();
    }

    [Fact]
    public void Throws_for_an_unmapped_native_type_naming_the_type()
    {
        var act = () => _mapper.Map(TimeSpan.FromSeconds(1));

        act.Should().Throw<NotSupportedException>().WithMessage("*TimeSpan*");
    }
}
