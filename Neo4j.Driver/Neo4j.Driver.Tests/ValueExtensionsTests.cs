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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Xunit;

#pragma warning disable CS0618

namespace Neo4j.Driver.Tests;

public class ValueExtensionsTests
{
    public class AsWithDefaultPrimaryTypeMethod
    {
        [Fact]
        public void ShouldConvertNullable()
        {
            object value = null;
            var actual = value.As<bool?>(false);
            actual.Value.Should().BeFalse();
        }

        [Fact]
        public void ShouldConvertToDefault()
        {
            object value = null;
            var actual = value.As(false);
            actual.Should().BeFalse();
        }

        [Fact]
        public void ShouldThrowExceptionWhenCastFromIntToList()
        {
            object value = 10;
            var ex = Record.Exception(() => value.As(new List<int>()));
            ex.Should().BeOfType<InvalidCastException>();
            ex.Message.Should()
                .StartWith("Invalid cast from 'System.Int32' to 'System.Collections.Generic.List`1[[System.Int32");
        }
    }

    public class AsPrimaryTypeMethod
    {
        [Fact]
        public void ShouldHandleLists()
        {
            object value = new List<object> { "a", "b" };
            var actual = value.As<List<string>>();
            actual.ToList().Should().HaveCount(2);
        }

        [Fact]
        public void ShouldConvertNullToNullable()
        {
            object value = null;
            var actual = value.As<bool?>();
            actual.HasValue.Should().BeFalse();
        }

        [Fact]
        public void ShouldConvertNullToString()
        {
            object value = null;
            var actual = value.As<string>();
            actual.Should().BeNull();
        }

        [Fact]
        public void ShouldConvertNullToClass()
        {
            object value = null;
            var actual = value.As<INode>();
            actual.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowExceptionWhenConvertingFromNullToNotNullable()
        {
            object value = null;
            var ex = Record.Exception(() => value.As<bool>());
            ex.Should().BeOfType<InvalidCastException>();
            ex.Message.Should().Be("Unable to cast `null` to `System.Boolean`.");
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1234L, true)]
        [InlineData(1.2f, true)]
        [InlineData(1.2, true)]
        [InlineData((short)1, true)]
        [InlineData((sbyte)1, true)]
        [InlineData((ulong)1234, true)]
        [InlineData((uint)1234, true)]
        [InlineData((ushort)1, true)]
        [InlineData((byte)1, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void ShouldConvertToNullable(object input, bool expected)
        {
            var actual = input.As<bool?>();
            actual.HasValue.Should().BeTrue();
            actual.Value.Should().Be(expected);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1234L, true)]
        [InlineData(1.2f, true)]
        [InlineData(1.2, true)]
        [InlineData((short)1, true)]
        [InlineData((sbyte)1, true)]
        [InlineData((ulong)1234, true)]
        [InlineData((uint)1234, true)]
        [InlineData((ushort)1, true)]
        [InlineData((byte)1, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void ShouldConvertToBool(object input, bool expected)
        {
            var actual = input.As<bool>();
            actual.Should().Be(expected);
        }

        [Theory]
        [InlineData(1234, 1234)]
        [InlineData(1234L, 1234)]
        [InlineData(1.2f, 1)]
        [InlineData(1.2, 1)]
        [InlineData((short)1, 1)]
        [InlineData((sbyte)1, 1)]
        [InlineData((ulong)1234, 1234)]
        [InlineData((uint)1234, 1234)]
        [InlineData((ushort)1, 1)]
        [InlineData((byte)1, 1)]
        [InlineData("1", 1)]
        [InlineData('a', 97)]
        [InlineData(true, 1)]
        public void ShouldConvertToInt(object input, int expected)
        {
            var actual = input.As<int>();
            actual.Should().Be(expected);
        }

        [Theory]
        [InlineData(1234, 1234)]
        [InlineData(1234L, 1234)]
        [InlineData(1.2f, 1)]
        [InlineData(1.2, 1)]
        [InlineData((short)1, 1)]
        [InlineData((sbyte)1, 1)]
        [InlineData((ulong)1234, 1234)]
        [InlineData((uint)1234, 1234)]
        [InlineData((ushort)1, 1)]
        [InlineData((byte)1, 1)]
        [InlineData("1", 1)]
        [InlineData('a', 97)]
        [InlineData(true, 1)]
        public void ShouldConvertToLong(object input, long expected)
        {
            var actual = input.As<long>();
            actual.Should().Be(expected);
        }

        [Theory]
        [InlineData(1234, 1234)]
        [InlineData(1234L, 1234)]
        [InlineData(1.2f, 1.2)]
        [InlineData(1.2, 1.2)]
        [InlineData((short)1, 1)]
        [InlineData((sbyte)1, 1)]
        [InlineData((ulong)1234, 1234)]
        [InlineData((uint)1234, 1234)]
        [InlineData((ushort)1, 1)]
        [InlineData((byte)1, 1)]
        [InlineData("1", 1)]
        [InlineData(true, 1)]
        public void ShouldConvertToDouble(object input, double expected)
        {
            var actual = input.As<double>();
            (actual - expected).Should().BeLessThan(0.01);
        }

        [Theory]
        [InlineData(12, 12)]
        [InlineData(12L, 12)]
        [InlineData(1.2f, 1)]
        [InlineData(1.2, 1)]
        [InlineData((short)1, 1)]
        [InlineData((sbyte)1, 1)]
        [InlineData((ulong)12, 12)]
        [InlineData((uint)12, 12)]
        [InlineData((ushort)1, 1)]
        [InlineData((byte)1, 1)]
        [InlineData("1", 1)]
        [InlineData('a', 97)]
        [InlineData(true, 1)]
        public void ShouldConvertToByte(object input, byte expected)
        {
            var actual = input.As<byte>();
            actual.Should().Be(expected);
        }
    }

    public class AsMethodWithError
    {
        [Fact]
        public void ShouldThrowExceptionWhenCastFromIntToList()
        {
            object value = 10;
            var ex = Record.Exception(() => value.As<List<int>>());
            ex.Should().BeOfType<InvalidCastException>();
            ex.Message.Should()
                .StartWith("Invalid cast from 'System.Int32' to 'System.Collections.Generic.List`1[[System.Int32");
        }

        [Fact]
        public void ShouldThrowExceptionWhenCastFromListToInt()
        {
            object value = new List<object> { "string", 2, true, "lala" };
            var ex = Record.Exception(() => value.As<int>());
            ex.Should().BeOfType<InvalidCastException>();
        }
    }

    public class AsCollectionTypeMethod
    {
        [Fact]
        public void ShouldCovertToListOfStrings()
        {
            IReadOnlyList<object> list = new List<object> { "string", 2, true, "lala" };
            object obj = list;
            var actual = obj.As<List<string>>();
            actual.Count.Should().Be(4);
            actual[0].Should().Be("string");
            actual[1].Should().Be("2");
            actual[2].Should().Be("True");
            actual[3].Should().Be("lala");
        }

        [Fact]
        public void ShouldCovertToListOfListOfInts()
        {
            IReadOnlyList<object> list = new List<object>
            {
                new List<object> { 1, 2, 3 },
                new List<object> { 11, 12, 13 },
                new List<object> { 21, 22, 23 },
                new List<object> { 31, 32, 33 }
            };

            object obj = list;
            var actual = obj.As<List<List<int>>>();
            actual.Count.Should().Be(4);
            actual[0][0].Should().Be(1);
            actual[1][1].Should().Be(12);
            actual[2][2].Should().Be(23);
            actual[3][2].Should().Be(33);
        }

        [Fact]
        public void ShouldCovertToDictionaryOfStrings()
        {
            IReadOnlyDictionary<string, object> dict =
                new Dictionary<string, object>
                    { { "key1", "string" }, { "key2", 2 }, { "key3", true }, { "key4", "lala" } };

            object obj = dict;
            var actual = obj.As<Dictionary<string, string>>();
            actual.Count.Should().Be(4);
            actual["key1"].As<string>().Should().Be("string");
            actual["key2"].As<string>().Should().Be("2");
            actual["key3"].As<string>().Should().Be("True");
            actual["key4"].As<string>().Should().Be("lala");
        }
    }

    public class AsBoltStructTypeMethod
    {
        [Fact]
        public void ShouldConvertToNode()
        {
            object obj = new Node(
                1L,
                new List<string> { "l1", "l2" },
                new Dictionary<string, object> { { "key1", "value1" }, { "key2", 2 } });

            var actual = obj.As<INode>();
            actual.Id.Should().Be(1);
            actual.Labels.Count.Should().Be(2);
            actual.Labels[0].Should().Be("l1");
            actual.Labels[1].Should().Be("l2");
            actual.Properties["key1"].As<string>().Should().Be("value1");
            actual.Properties["key2"].As<string>().Should().Be("2");

            obj.As<string>().Should().Be($"{obj.GetType()}");
        }

        [Fact]
        public void ShouldConvertToRel()
        {
            object obj = new Relationship(
                1,
                -2,
                -3,
                "Type",
                new Dictionary<string, object> { { "key1", "value1" }, { "key2", 2 } });

            var actual = obj.As<IRelationship>();
            actual.Id.Should().Be(1);
            actual.Type.Should().Be("Type");
            actual.Properties["key1"].As<string>().Should().Be("value1");
            actual.Properties["key2"].As<string>().Should().Be("2");

            obj.As<string>().Should().Be($"{obj.GetType()}");
        }

        [Fact]
        public void ShouldConvertToPath()
        {
            object obj = new Path(
                new List<ISegment>(),
                new List<INode>
                {
                    new Node(
                        1L,
                        new List<string> { "l1", "l2" },
                        new Dictionary<string, object> { { "key1", "value1" }, { "key2", 2 } })
                },
                new List<IRelationship>());

            var actual = obj.As<IPath>();
            actual.Start.Id.Should().Be(1);
            actual.End.Id.Should().Be(1);
            var node = actual.Nodes[0];
            node.Properties["key1"].As<string>().Should().Be("value1");
            node.Properties["key2"].As<string>().Should().Be("2");

            obj.As<string>().Should().Be("Path with 0 segments");
        }
    }
}
