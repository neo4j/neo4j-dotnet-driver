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
using Neo4j.Driver.Preview.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class LambdaMappingRecordExtensionsTests
{
    [Fact]
    public void ShouldMapToAnonymousTypeWithOneProperty()
    {
        var record = TestRecord.Create(["a"], [69]);
        var result = record.AsObject(
            a => new
            {
                A = a.As<int>()
            });

        result.A.Should().Be(69);
    }

    [Fact]
    public void ShouldFailWithOnePropertyWhenPropertyMissing()
    {
        var record = TestRecord.Create(["other"], [69]);
        var act = () => record.AsObject(
            a => new
            {
                A = a.As<int>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithOnePropertyWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(["a"], ["gettysburg"]);
        var act = () => record.AsObject(
            a => new
            {
                A = a.As<int>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithTwoProperties()
    {
        var record = TestRecord.Create(["a", "b"], [69, "test"]);
        var result = record.AsObject(
            (a, b) => new
            {
                A = a.As<int>(),
                B = b.As<string>()
            });

        result.A.Should().Be(69);
        result.B.Should().Be("test");
    }

    [Fact]
    public void ShouldFailWithTwoPropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(["other", "b"], [69, "test"]);
        var act = () => record.AsObject(
            (a, b) => new
            {
                A = a.As<int>(),
                B = b.As<string>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithTwoPropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(["a", "b"], ["gettysburg", "test"]);
        var act = () => record.AsObject(
            (a, b) => new
            {
                A = a.As<int>(),
                B = b.As<string>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithThreeProperties()
    {
        var record = TestRecord.Create(["a", "b", "c"], [69, "test", true]);
        var result = record.AsObject(
            (a, b, c) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>()
            });

        result.A.Should().Be(69);
        result.B.Should().Be("test");
        result.C.Should().Be(true);
    }

    [Fact]
    public void ShouldFailWithThreePropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(["other", "b", "c"], [69, "test", true]);
        var act = () => record.AsObject(
            (a, b, c) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithThreePropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(["a", "b", "c"], ["gettysburg", "test", true]);
        var act = () => record.AsObject(
            (a, b, c) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithFourProperties()
    {
        var record = TestRecord.Create(["a", "b", "c", "d"], [69, "test", true, 3.14]);
        var result = record.AsObject(
            (a, b, c, d) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>()
            });

        result.A.Should().Be(69);
        result.B.Should().Be("test");
        result.C.Should().Be(true);
        result.D.Should().Be(3.14);
    }

    [Fact]
    public void ShouldFailWithFourPropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(
            ["other", "b", "c", "d"],
            [69, "test", true, 3.14]);

        var act = () => record.AsObject(
            (a, b, c, d) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithFourPropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(
            ["a", "b", "c", "d"],
            ["gettysburg", "test", true, "pi"]);

        var act = () => record.AsObject(
            (a, b, c, d) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithFiveProperties()
    {
        var record = TestRecord.Create(
            ["a", "b", "c", "d", "e"],
            [69, "test", true, 3.14, new DateTime(1955, 11, 5, 6, 15, 0)]);

        var result = record.AsObject(
            (a, b, c, d, e) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>()
            });

        result.A.Should().Be(69);
        result.B.Should().Be("test");
        result.C.Should().Be(true);
        result.D.Should().Be(3.14);
        result.E.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
    }

    [Fact]
    public void ShouldFailWithFivePropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(
            ["other", "b", "c", "d", "e"],
            [69, "test", true, 3.14, new DateTime(1955, 11, 5, 6, 15, 0)]);

        var act = () => record.AsObject(
            (a, b, c, d, e) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithFivePropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(
            ["a", "b", "c", "d", "e"],
            ["gettysburg", "test", true, "pi", "not a date"]);

        var act = () => record.AsObject(
            (a, b, c, d, e) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithSixProperties()
    {
        var record = TestRecord.Create(
            ["a", "b", "c", "d", "e", "f"],
            [69, "test", true, 3.14, new DateTime(1955, 11, 5, 6, 15, 0), 42L]);

        var result = record.AsObject(
            (a, b, c, d, e, f) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>(),
                F = f.As<long>()
            });

        result.A.Should().Be(69);
        result.B.Should().Be("test");
        result.C.Should().Be(true);
        result.D.Should().Be(3.14);
        result.E.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
        result.F.Should().Be(42L);
    }

    [Fact]
    public void ShouldFailWithSixPropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(
            ["other", "b", "c", "d", "e", "f"],
            [69, "test", true, 3.14, new DateTime(1955, 11, 5, 6, 15, 0), 42L]);

        var act = () => record.AsObject(
            (a, b, c, d, e, f) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>(),
                F = f.As<long>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithSixPropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(
            ["a", "b", "c", "d", "e", "f"],
            ["gettysburg", "test", true, "pi", "not a date", "not a long"]);

        var act = () => record.AsObject(
            (a, b, c, d, e, f) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>(),
                F = f.As<long>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithSevenProperties()
    {
        var record = TestRecord.Create(
            ["a", "b", "c", "d", "e", "f", "g"],
            [69, "test", true, 3.14, new DateTime(1955, 11, 5, 6, 15, 0), 42L, 1.23f]);

        var result = record.AsObject(
            (a, b, c, d, e, f, g) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>(),
                F = f.As<long>(),
                G = g.As<float>()
            });

        result.A.Should().Be(69);
        result.B.Should().Be("test");
        result.C.Should().Be(true);
        result.D.Should().Be(3.14);
        result.E.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
        result.F.Should().Be(42L);
        result.G.Should().Be(1.23f);
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithEightProperties()
    {
        var record = TestRecord.Create(
            ["a", "b", "c", "d", "e", "f", "g", "h"],
            [69, "test", true, 3.14, new DateTime(1955, 11, 5, 6, 15, 0), 42L, 1.23f, 123.45m]);

        var result = record.AsObject(
            (a, b, c, d, e, f, g, h) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>(),
                F = f.As<long>(),
                G = g.As<float>(),
                H = h.As<decimal>()
            });

        result.A.Should().Be(69);
        result.B.Should().Be("test");
        result.C.Should().Be(true);
        result.D.Should().Be(3.14);
        result.E.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
        result.F.Should().Be(42L);
        result.G.Should().Be(1.23f);
        result.H.Should().Be(123.45m);
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithNineProperties()
    {
        var record = TestRecord.Create(
            ["a", "b", "c", "d", "e", "f", "g", "h", "i"],
            [69, "test", true, 3.14, new DateTime(1955, 11, 5, 6, 15, 0), 42L, 1.23f, 123.45m, 'x']);

        var result = record.AsObject(
            (a, b, c, d, e, f, g, h, i) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>(),
                F = f.As<long>(),
                G = g.As<float>(),
                H = h.As<decimal>(),
                I = i.As<char>()
            });

        result.A.Should().Be(69);
        result.B.Should().Be("test");
        result.C.Should().Be(true);
        result.D.Should().Be(3.14);
        result.E.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
        result.F.Should().Be(42L);
        result.G.Should().Be(1.23f);
        result.H.Should().Be(123.45m);
        result.I.Should().Be('x');
    }

    [Fact]
    public void ShouldMapToAnonymousTypeWithTenProperties()
    {
        var record = TestRecord.Create(
            ["a", "b", "c", "d", "e", "f", "g", "h", "i", "j"],
            [69, "test", true, 3.14, new DateTime(1955, 11, 5, 6, 15, 0), 42L, 1.23f, 123.45m, 'x', (byte)7]);

        var result = record.AsObject(
            (a, b, c, d, e, f, g, h, i, j) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>(),
                F = f.As<long>(),
                G = g.As<float>(),
                H = h.As<decimal>(),
                I = i.As<char>(),
                J = j.As<byte>()
            });

        result.A.Should().Be(69);
        result.B.Should().Be("test");
        result.C.Should().Be(true);
        result.D.Should().Be(3.14);
        result.E.Should().Be(new DateTime(1955, 11, 5, 6, 15, 0));
        result.F.Should().Be(42L);
        result.G.Should().Be(1.23f);
        result.H.Should().Be(123.45m);
        result.I.Should().Be('x');
        result.J.Should().Be(7);
    }

    [Fact]
    public void ShouldFailWithTenPropertiesWhenPropertyMissing()
    {
        var record = TestRecord.Create(
            ["other", "b", "c", "d", "e", "f", "g", "h", "i", "j"],
            [69, "test", true, 3.14, new DateTime(1955, 11, 5, 6, 15, 0), 42L, 1.23f, 123.45m, 'x', (byte)7]);

        var act = () => record.AsObject(
            (a, b, c, d, e, f, g, h, i, j) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>(),
                F = f.As<long>(),
                G = g.As<float>(),
                H = h.As<decimal>(),
                I = i.As<char>(),
                J = j.As<byte>()
            });

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldFailWithTenPropertiesWhenPropertyTypeMismatch()
    {
        var record = TestRecord.Create(
            ["a", "b", "c", "d", "e", "f", "g", "h", "i", "j"],
            [
                "gettysburg", "test", true, 3.14, new DateTime(1955, 11, 5, 6, 15, 0), 42L, 1.23f, 123.45m, 'x', (byte)7
            ]);

        var act = () => record.AsObject(
            (a, b, c, d, e, f, g, h, i, j) => new
            {
                A = a.As<int>(),
                B = b.As<string>(),
                C = c.As<bool>(),
                D = d.As<double>(),
                E = e.As<DateTime>(),
                F = f.As<long>(),
                G = g.As<float>(),
                H = h.As<decimal>(),
                I = i.As<char>(),
                J = j.As<byte>()
            });

        act.Should().Throw<MappingFailedException>();
    }
}
