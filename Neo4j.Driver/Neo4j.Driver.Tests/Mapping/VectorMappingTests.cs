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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

// ReSharper disable once ClassNeverInstantiated.Global
public class ClassWithVector
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public Vector<double> DoubleVector { get; set; } = null!;
}

public class VectorMappingTests
{
    [Fact]
    public void Should_Map_Vector_Property()
    {
        var record = TestRecord.Create(
            ("Id", 1),
            ("Name", "Alice"),
            ("DoubleVector", new Vector<double>([1.0, 2.0, 3.0])));

        var poco = record.AsObject<ClassWithVector>();

        poco.Id.Should().Be(1);
        poco.Name.Should().Be("Alice");
        poco.DoubleVector.Values.Should().Equal(1.0, 2.0, 3.0);
    }

    [Fact]
    public void Should_Throw_When_Vector_Element_Type_Does_Not_Match_Property_Type()
    {
        var record = TestRecord.Create(
            ("Id", 1),
            ("Name", "Alice"),
            ("DoubleVector", new Vector<int>([1, 2, 3])));

        var act = () => record.AsObject<ClassWithVector>();

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void Should_Map_Vector_Nested_In_Anon_Object()
    {
        var node = new Node(
            1,
            ["Label"],
            new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "Alice" },
                { "DoubleVector", new Vector<double>([1.0, 2.0, 3.0]) }
            });

        var record = TestRecord.Create(("Nested", node));
        var poco = record.AsObjectFromBlueprint(
            new
            {
                Nested = new
                {
                    Id = 0, Name = string.Empty, DoubleVector = (Vector<double>)null!
                }
            });

        poco.Nested.Id.Should().Be(1);
        poco.Nested.Name.Should().Be("Alice");
        poco.Nested.DoubleVector.Values.Should().Equal(1.0, 2.0, 3.0);
    }

    private IRecord GetTestRecord()
    {
        return TestRecord.Create(
            ("Ints", new Vector<int>([5, 6, 7])),
            ("Longs", new Vector<long>([3456L, 4567L, 5678L])),
            ("Floats", new Vector<float>([394857.5f, 48576.4f, 5768.3f])),
            ("Doubles", new Vector<double>([854765.45, 94765.34, 8765.23])),
            ("Shorts", new Vector<short>([43, 54, 65])),
            ("Bytes", new Vector<sbyte>([94, 85, 76])));
    }

    [Fact]
    public void Should_Convert_Vector_To_Array()
    {
        var record = GetTestRecord();
        var obj = record.AsObjectFromBlueprint(
            new
            {
                Ints = System.Array.Empty<int>(),
                Longs = System.Array.Empty<long>(),
                Floats = System.Array.Empty<float>(),
                Doubles = System.Array.Empty<double>(),
                Shorts = System.Array.Empty<short>(),
                Bytes = System.Array.Empty<sbyte>()
            });

        obj.Ints.Should().Equal(5, 6, 7);
        obj.Longs.Should().Equal(3456L, 4567L, 5678L);
        obj.Floats.Should().BeApproximately([394857.5f, 48576.4f, 5768.3f]);
        obj.Doubles.Should().BeApproximately([854765.45, 94765.34, 8765.23]);
        obj.Shorts.Should().Equal(43, 54, 65);
        obj.Bytes.Should().Equal(94, 85, 76);
    }

    [Fact]
    public void Should_Convert_Vector_To_List()
    {
        var record = GetTestRecord();
        var obj = record.AsObjectFromBlueprint(
            new
            {
                Ints = new List<int>(),
                Longs = new List<long>(),
                Floats = new List<float>(),
                Doubles = new List<double>(),
                Shorts = new List<short>(),
                Bytes = new List<sbyte>()
            });

        obj.Ints.Should().Equal(5, 6, 7);
        obj.Longs.Should().Equal(3456L, 4567L, 5678L);
        obj.Floats.Should().BeApproximately([394857.5f, 48576.4f, 5768.3f]);
        obj.Doubles.Should().BeApproximately([854765.45, 94765.34, 8765.23]);
        obj.Shorts.Should().Equal(43, 54, 65);
        obj.Bytes.Should().Equal(94, 85, 76);
    }

    [Fact]
    public void Should_Convert_Vector_To_IList()
    {
        var record = GetTestRecord();
        var obj = record.AsObjectFromBlueprint(
            new
            {
                Ints = (IList<int>)null!,
                Longs = (IList<long>)null!,
                Floats = (IList<float>)null!,
                Doubles = (IList<double>)null!,
                Shorts = (IList<short>)null!,
                Bytes = (IList<sbyte>)null!
            });

        obj.Ints.Should().Equal(5, 6, 7);
        obj.Longs.Should().Equal(3456L, 4567L, 5678L);
        obj.Floats.Should().BeApproximately([394857.5f, 48576.4f, 5768.3f]);
        obj.Doubles.Should().BeApproximately([854765.45, 94765.34, 8765.23]);
        obj.Shorts.Should().Equal(43, 54, 65);
        obj.Bytes.Should().Equal(94, 85, 76);
    }

    [Fact]
    public void Should_Convert_Vector_To_IEnumerable()
    {
        var record = GetTestRecord();
        var obj = record.AsObjectFromBlueprint(
            new
            {
                Ints = (IEnumerable<int>)null!,
                Longs = (IEnumerable<long>)null!,
                Floats = (IEnumerable<float>)null!,
                Doubles = (IEnumerable<double>)null!,
                Shorts = (IEnumerable<short>)null!,
                Bytes = (IEnumerable<sbyte>)null!
            });

        obj.Ints.Should().Equal(5, 6, 7);
        obj.Longs.Should().Equal(3456L, 4567L, 5678L);
        obj.Floats.Should().BeApproximately([394857.5f, 48576.4f, 5768.3f]);
        obj.Doubles.Should().BeApproximately([854765.45, 94765.34, 8765.23]);
        obj.Shorts.Should().Equal(43, 54, 65);
        obj.Bytes.Should().Equal(94, 85, 76);
    }

    [Fact]
    public void Should_Convert_Vector_To_IReadOnlyList()
    {
        var record = GetTestRecord();
        var obj = record.AsObjectFromBlueprint(
            new
            {
                Ints = (IReadOnlyList<int>)null!,
                Longs = (IReadOnlyList<long>)null!,
                Floats = (IReadOnlyList<float>)null!,
                Doubles = (IReadOnlyList<double>)null!,
                Shorts = (IReadOnlyList<short>)null!,
                Bytes = (IReadOnlyList<sbyte>)null!
            });

        obj.Ints.Should().Equal(5, 6, 7);
        obj.Longs.Should().Equal(3456L, 4567L, 5678L);
        obj.Floats.Should().BeApproximately([394857.5f, 48576.4f, 5768.3f]);
        obj.Doubles.Should().BeApproximately([854765.45, 94765.34, 8765.23]);
        obj.Shorts.Should().Equal(43, 54, 65);
        obj.Bytes.Should().Equal(94, 85, 76);
    }

    [Fact]
    public void Should_Convert_Vector_To_IReadOnlyCollection()
    {
        var record = GetTestRecord();
        var obj = record.AsObjectFromBlueprint(
            new
            {
                Ints = (IReadOnlyCollection<int>)null!,
                Longs = (IReadOnlyCollection<long>)null!,
                Floats = (IReadOnlyCollection<float>)null!,
                Doubles = (IReadOnlyCollection<double>)null!,
                Shorts = (IReadOnlyCollection<short>)null!,
                Bytes = (IReadOnlyCollection<sbyte>)null!
            });

        obj.Ints.Should().Equal(5, 6, 7);
        obj.Longs.Should().Equal(3456L, 4567L, 5678L);
        obj.Floats.Should().BeApproximately([394857.5f, 48576.4f, 5768.3f]);
        obj.Doubles.Should().BeApproximately([854765.45, 94765.34, 8765.23]);
        obj.Shorts.Should().Equal(43, 54, 65);
        obj.Bytes.Should().Equal(94, 85, 76);
    }

    [Fact]
    public void Should_Convert_Vector_To_ICollection()
    {
        var record = GetTestRecord();
        var obj = record.AsObjectFromBlueprint(
            new
            {
                Ints = (ICollection<int>)null!,
                Longs = (ICollection<long>)null!,
                Floats = (ICollection<float>)null!,
                Doubles = (ICollection<double>)null!,
                Shorts = (ICollection<short>)null!,
                Bytes = (ICollection<sbyte>)null!
            });

        obj.Ints.Should().Equal(5, 6, 7);
        obj.Longs.Should().Equal(3456L, 4567L, 5678L);
        obj.Floats.Should().BeApproximately([394857.5f, 48576.4f, 5768.3f]);
        obj.Doubles.Should().BeApproximately([854765.45, 94765.34, 8765.23]);
        obj.Shorts.Should().Equal(43, 54, 65);
        obj.Bytes.Should().Equal(94, 85, 76);
    }
}
