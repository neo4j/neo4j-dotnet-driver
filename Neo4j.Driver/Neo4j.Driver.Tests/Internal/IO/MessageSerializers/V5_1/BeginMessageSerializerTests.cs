// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Moq;
using Neo4j.Driver.Internal.Messaging.V5_1;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Tests;
using Xunit;

namespace Neo4j.Driver.Internal.IO.MessageSerializers.V5_1;

public class BeginMessageSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new BeginMessageSerializer();

    [Fact]
    public void ShouldThrowOnDeserialize()
    {
        var handler = SerializerUnderTest;

        var ex = Record.Exception(() =>
            handler.Deserialize(Mock.Of<IPackStreamReader>(), BoltProtocolV3MessageFormat.MsgBegin, 1));

        ex.Should().NotBeNull();
        ex.Should().BeOfType<ProtocolException>();
    }

    [Fact]
    public void ShouldSerialize()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();

        writer.Write(new BeginMessage(null, Bookmarks.From(AsyncSessionTests.FakeABookmark(123)), TimeSpan.FromMinutes(1),
            new Dictionary<string, object>
            {
                {"username", "MollyMostlyWhite"}
            }, AccessMode.Write, null,null));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgBegin);

        reader.ReadMap().Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["bookmarks"] = new List<object> {"bookmark-123"},
            ["tx_timeout"] = 60000L,
            ["tx_metadata"] = new Dictionary<string, object> { ["username"] = "MollyMostlyWhite"}
        });
    }

    [Fact]
    public void ShouldSerializeWithNotificationFilters()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();

        writer.Write(new BeginMessage(null, Bookmarks.From(AsyncSessionTests.FakeABookmark(123)), TimeSpan.FromMinutes(1),
            new Dictionary<string, object>
            {
                {"username", "MollyMostlyWhite"}
            }, AccessMode.Write, null, new []{ "test.filter.A", "test.filter.B"}));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgBegin);
        var map = reader.ReadMap();
        map.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["bookmarks"] = new List<object> { "bookmark-123" },
            ["tx_timeout"] = 60000L,
            ["tx_metadata"] = new Dictionary<string, object> { ["username"] = "MollyMostlyWhite" },
            ["notifications"] = new[] { "test.filter.A", "test.filter.B" }
        });
    }

    [Fact]
    public void ShouldSerializeWithEmptyNotificationFilters()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();

        writer.Write(new BeginMessage(null, Bookmarks.From(AsyncSessionTests.FakeABookmark(123)), TimeSpan.FromMinutes(1),
            new Dictionary<string, object>
            {
                {"username", "MollyMostlyWhite"}
            }, AccessMode.Write, null, Array.Empty<string>()));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgBegin);
        var map = reader.ReadMap();
        map.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["bookmarks"] = new List<object> { "bookmark-123" },
            ["tx_timeout"] = 60000L,
            ["tx_metadata"] = new Dictionary<string, object> { ["username"] = "MollyMostlyWhite" },
            ["notifications"] = Array.Empty<string>()
        });
    }

    [Fact]
    public void ShouldSerializeEmptyMapWhenMetadataIsNull()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();

        writer.Write(new BeginMessage(null, null, null, null, AccessMode.Write, null, null));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgBegin);
        reader.ReadMap().Should().NotBeNull().And.HaveCount(0);
    }
}