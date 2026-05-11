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
using FluentAssertions;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Tests.Internal.IO.Utils;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO;

/// <summary>
/// Tests for Guid values passed through PackStreamWriter.Write(object) — the path exercised when a
/// Guid is used as a query parameter.
/// </summary>
public class UuidQueryParameterTests
{
    // Bolt version constants used in version-gate tests.
    // V6_1 inline because the constant may not yet exist in BoltProtocolVersion.
    private static readonly BoltProtocolVersion V6_1 = new(6, 1);
    private static readonly BoltProtocolVersion V6_0 = new(6, 0);
    private static readonly BoltProtocolVersion V5_4 = new(5, 4);

    private static PackStreamWriterMachine WriterFor(BoltProtocolVersion version)
    {
        var format = new MessageFormat(version, TestDriverContext.MockContext);
        return new PackStreamWriterMachine(s => new PackStreamWriter(format, s));
    }

    private static PackStreamReaderMachine ReaderFor(byte[] bytes, BoltProtocolVersion version)
    {
        var format = new MessageFormat(version, TestDriverContext.MockContext);
        return new PackStreamReaderMachine(
            bytes,
            s => new PackStreamReader(format, s, new ByteBuffers()));
    }

    // ── Write(object) dispatch ────────────────────────────────────────────────

    [Fact]
    public void Write_Guid_ShouldNotThrowProtocolException()
    {
        // Currently Write(Guid) falls through to the default case and throws
        // "Cannot understand value with type System.Guid".
        var guid = Guid.NewGuid();
        var machine = WriterFor(V6_1);

        var act = () => machine.Writer.Write(guid);

        act.Should().NotThrow<ProtocolException>();
    }

    [Fact]
    public void Write_Guid_ShouldProduceSameBytesAsWriteUuid()
    {
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        var viaDirect = WriterFor(V6_1);
        viaDirect.Writer.WriteUuid(guid);

        var viaDispatch = WriterFor(V6_1);
        viaDispatch.Writer.Write((object)guid);

        viaDispatch.GetOutput().Should().Equal(viaDirect.GetOutput());
    }

    [Fact]
    public void Write_Guid_ThenRead_RoundTrips()
    {
        var guid = Guid.NewGuid();
        var machine = WriterFor(V6_1);

        machine.Writer.Write((object)guid);

        var reader = ReaderFor(machine.GetOutput(), V6_1).Reader();
        reader.Read().Should().Be(guid);
    }

    // ── Query parameter dictionary ────────────────────────────────────────────

    [Fact]
    public void Write_DictionaryContainingGuid_ShouldNotThrow()
    {
        var guid = Guid.NewGuid();
        var parameters = new Dictionary<string, object> { ["id"] = guid };
        var machine = WriterFor(V6_1);

        var act = () => machine.Writer.Write(parameters);

        act.Should().NotThrow<ProtocolException>();
    }

    [Fact]
    public void Write_DictionaryContainingGuid_ThenRead_RoundTrips()
    {
        var guid = Guid.NewGuid();
        var parameters = new Dictionary<string, object> { ["id"] = guid };
        var machine = WriterFor(V6_1);

        machine.Writer.Write(parameters);

        var reader = ReaderFor(machine.GetOutput(), V6_1).Reader();
        var map = reader.ReadMap();
        map["id"].Should().Be(guid);
    }

    [Fact]
    public void Write_DictionaryWithMultipleGuids_ThenRead_RoundTrips()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var parameters = new Dictionary<string, object>
        {
            ["a"] = guid1,
            ["b"] = guid2,
            ["name"] = "Alice"
        };
        var machine = WriterFor(V6_1);

        machine.Writer.Write(parameters);

        var reader = ReaderFor(machine.GetOutput(), V6_1).Reader();
        var map = reader.ReadMap();
        map["a"].Should().Be(guid1);
        map["b"].Should().Be(guid2);
        map["name"].Should().Be("Alice");
    }

    // ── List containing Guid ──────────────────────────────────────────────────

    [Fact]
    public void Write_ListContainingGuid_ShouldNotThrow()
    {
        var list = new List<object> { Guid.NewGuid(), Guid.NewGuid() };
        var machine = WriterFor(V6_1);

        var act = () => machine.Writer.Write(list);

        act.Should().NotThrow<ProtocolException>();
    }

    [Fact]
    public void Write_ListContainingGuid_ThenRead_RoundTrips()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var list = new List<object> { guid1, guid2 };
        var machine = WriterFor(V6_1);

        machine.Writer.Write(list);

        var reader = ReaderFor(machine.GetOutput(), V6_1).Reader();
        var result = reader.ReadList();
        result.Should().BeEquivalentTo(new[] { guid1, guid2 });
    }

    // ── Nullable Guid ─────────────────────────────────────────────────────────

    [Fact]
    public void Write_NullableGuidWithValue_ShouldNotThrow()
    {
        Guid? guid = Guid.NewGuid();
        var machine = WriterFor(V6_1);

        var act = () => machine.Writer.Write((object)guid);

        act.Should().NotThrow();
    }

    [Fact]
    public void Write_NullableGuidWithValue_ThenRead_RoundTrips()
    {
        Guid? guid = Guid.NewGuid();
        var machine = WriterFor(V6_1);

        machine.Writer.Write((object)guid);

        var reader = ReaderFor(machine.GetOutput(), V6_1).Reader();
        reader.Read().Should().Be(guid.Value);
    }

    [Fact]
    public void Write_NullableGuidWithNull_WritesNull()
    {
        Guid? guid = null;
        var machine = WriterFor(V6_1);

        machine.Writer.Write((object)guid);

        var reader = ReaderFor(machine.GetOutput(), V6_1).Reader();
        reader.PeekNextType().Should().Be(PackStreamType.Null);
    }

    // ── Version gating ────────────────────────────────────────────────────────

    [Fact]
    public void Write_Guid_OnBoltBelow6_1_ShouldThrowWithVersionMessage()
    {
        var guid = Guid.NewGuid();
        var machine = WriterFor(V6_0);

        var act = () => machine.Writer.Write((object)guid);

        act.Should().Throw<ProtocolException>()
            .WithMessage("*6.1*");
    }

    [Fact]
    public void Write_Guid_On5x_ShouldThrowWithVersionMessage()
    {
        var guid = Guid.NewGuid();
        var machine = WriterFor(V5_4);

        var act = () => machine.Writer.Write((object)guid);

        act.Should().Throw<ProtocolException>()
            .WithMessage("*6.1*");
    }

    [Fact]
    public void WriteUuid_DirectCall_OnBoltBelow6_1_ShouldThrow()
    {
        var guid = Guid.NewGuid();
        var machine = WriterFor(V6_0);

        var act = () => machine.Writer.WriteUuid(guid);

        act.Should().Throw<ProtocolException>()
            .WithMessage("*6.1*");
    }
}
