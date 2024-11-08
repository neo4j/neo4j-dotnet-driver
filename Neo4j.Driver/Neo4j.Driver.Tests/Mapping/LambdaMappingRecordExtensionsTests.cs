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
using FluentAssertions;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class LambdaMappingRecordExtensionsTests
{
    [Fact]
    public void ShouldMapToAnonymousTypeWithOneProperty()
    {
        var record = TestRecord.Create(("a", 69));
        var result = record.AsObject((int a) => new { a });

        result.a.Should().Be(69);
    }

    [Fact]
    public void ShouldFailWithOnePropertyWhenPropertyMissing()
    {
        var record = TestRecord.Create(("other", 69));
        Action act = () => record.AsObject((int a) => new { a });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithOnePropertyWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(("a", "gettysburg"));
        Action act = () => record.AsObject((int a) => new { a });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithTwoProperties()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"));
        var result = record.AsObject((int a, string b) => new { a, b });

        result.a.Should().Be(69);
        result.b.Should().Be("test");
    }

    [Fact]
    public void ShouldFailWithTwoPropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(("a", 69));
        Action act = () => record.AsObject((int a, string b) => new { a, b });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithTwoPropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(("a", 69), ("b", "six"));
        Action act = () => record.AsObject((int a, int b) => new { a, b });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithThreeProperties()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", true));
        var result = record.AsObject((int a, string b, bool c) => new { a, b, c });

        result.a.Should().Be(69);
        result.b.Should().Be("test");
        result.c.Should().Be(true);
    }

    [Fact]
    public void ShouldFailWithThreePropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"));
        Action act = () => record.AsObject((int a, string b, bool c) => new { a, b, c });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithThreePropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", "not a bool"));
        Action act = () => record.AsObject((int a, string b, bool c) => new { a, b, c });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithFourProperties()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", true), ("d", 1.23));
        var result = record.AsObject((int a, string b, bool c, double d) => new { a, b, c, d });

        result.a.Should().Be(69);
        result.b.Should().Be("test");
        result.c.Should().Be(true);
        result.d.Should().Be(1.23);
    }

    [Fact]
    public void ShouldFailWithFourPropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", true));
        Action act = () => record.AsObject((int a, string b, bool c, double d) => new { a, b, c, d });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithFourPropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", true), ("d", "not a double"));
        Action act = () => record.AsObject((int a, string b, bool c, double d) => new { a, b, c, d });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithFiveProperties()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", true), ("d", 1.23), ("e", 123L));
        var result = record.AsObject((int a, string b, bool c, double d, long e) => new { a, b, c, d, e });

        result.a.Should().Be(69);
        result.b.Should().Be("test");
        result.c.Should().Be(true);
        result.d.Should().Be(1.23);
        result.e.Should().Be(123L);
    }

    [Fact]
    public void ShouldFailWithFivePropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", true), ("d", 1.23));
        Action act = () => record.AsObject((int a, string b, bool c, double d, long e) => new { a, b, c, d, e });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithFivePropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", true), ("d", 1.23), ("e", "not a long"));
        Action act = () => record.AsObject((int a, string b, bool c, double d, long e) => new { a, b, c, d, e });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithSixProperties()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", true), ("d", 1.23), ("e", 123L), ("f", 'x'));
        var result = record.AsObject((int a, string b, bool c, double d, long e, char f) => new { a, b, c, d, e, f });

        result.a.Should().Be(69);
        result.b.Should().Be("test");
        result.c.Should().Be(true);
        result.d.Should().Be(1.23);
        result.e.Should().Be(123L);
        result.f.Should().Be('x');
    }

    [Fact]
    public void ShouldFailWithSixPropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", true), ("d", 1.23), ("e", 123L));
        Action act = () =>
            record.AsObject((int a, string b, bool c, double d, long e, char f) => new { a, b, c, d, e, f });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithSixPropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", "not a char"));

        Action act = () =>
            record.AsObject((int a, string b, bool c, double d, long e, char f) => new { a, b, c, d, e, f });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithSevenProperties()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", 123.45m));

        var result = record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g) => new { a, b, c, d, e, f, g });

        result.a.Should().Be(69);
        result.b.Should().Be("test");
        result.c.Should().Be(true);
        result.d.Should().Be(1.23);
        result.e.Should().Be(123L);
        result.f.Should().Be('x');
        result.g.Should().Be(123.45m);
    }

    [Fact]
    public void ShouldFailWithSevenPropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(("a", 69), ("b", "test"), ("c", true), ("d", 1.23), ("e", 123L), ("f", 'x'));
        Action act = () => record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g) => new { a, b, c, d, e, f, g });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithSevenPropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", "not a decimal"));

        Action act = () => record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g) => new { a, b, c, d, e, f, g });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithEightProperties()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", 123.45m),
            ("h", (byte)123));

        var result = record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g, byte h) => new { a, b, c, d, e, f, g, h });

        result.a.Should().Be(69);
        result.b.Should().Be("test");
        result.c.Should().Be(true);
        result.d.Should().Be(1.23);
        result.e.Should().Be(123L);
        result.f.Should().Be('x');
        result.g.Should().Be(123.45m);
        result.h.Should().Be((byte)123);
    }

    [Fact]
    public void ShouldFailWithEightPropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", 123.45m));

        Action act = () => record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g, byte h) => new { a, b, c, d, e, f, g, h });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithEightPropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", 123.45m),
            ("h", "not a byte"));

        Action act = () => record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g, byte h) => new { a, b, c, d, e, f, g, h });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithNineProperties()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", 123.45m),
            ("h", (byte)123),
            ("i", (short)12345));

        var result = record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g, byte h, short i) =>
                new { a, b, c, d, e, f, g, h, i });

        result.a.Should().Be(69);
        result.b.Should().Be("test");
        result.c.Should().Be(true);
        result.d.Should().Be(1.23);
        result.e.Should().Be(123L);
        result.f.Should().Be('x');
        result.g.Should().Be(123.45m);
        result.h.Should().Be((byte)123);
        result.i.Should().Be((short)12345);
    }

    [Fact]
    public void ShouldFailWithNinePropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", 123.45m),
            ("h", (byte)123));

        Action act = () => record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g, byte h, short i) =>
                new { a, b, c, d, e, f, g, h, i });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithNinePropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", 123.45m),
            ("h", (byte)123),
            ("i", "not a short"));

        Action act = () => record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g, byte h, short i) =>
                new { a, b, c, d, e, f, g, h, i });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithTenProperties()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", 123.45m),
            ("h", (byte)123),
            ("i", (short)12345),
            ("j", (ushort)12345));

        var result = record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g, byte h, short i, ushort j) =>
                new { a, b, c, d, e, f, g, h, i, j });

        result.a.Should().Be(69);
        result.b.Should().Be("test");
        result.c.Should().Be(true);
        result.d.Should().Be(1.23);
        result.e.Should().Be(123L);
        result.f.Should().Be('x');
        result.g.Should().Be(123.45m);
        result.h.Should().Be((byte)123);
        result.i.Should().Be((short)12345);
        result.j.Should().Be((ushort)12345);
    }

    [Fact]
    public void ShouldFailWithTenPropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", 123.45m),
            ("h", (byte)123),
            ("i", (short)12345));

        Action act = () => record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g, byte h, short i, ushort j) =>
                new { a, b, c, d, e, f, g, h, i, j });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithTenPropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(
            ("a", 69),
            ("b", "test"),
            ("c", true),
            ("d", 1.23),
            ("e", 123L),
            ("f", 'x'),
            ("g", 123.45m),
            ("h", (byte)123),
            ("i", (short)12345),
            ("j", "not a ushort"));

        Action act = () => record.AsObject(
            (int a, string b, bool c, double d, long e, char f, decimal g, byte h, short i, ushort j) =>
                new { a, b, c, d, e, f, g, h, i, j });

        act.Should().Throw<MappingFailedException>();
    }
}
