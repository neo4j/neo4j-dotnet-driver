﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Messaging.V5_1;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Tests;
using Xunit;

namespace Neo4j.Driver.Internal.IO.MessageSerializers.V5_1;

public class RunWithMetadataMessageSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new RunWithMetadataMessageSerializer();

    [Fact]
    public void ShouldSerialize()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();

        var query = new Query("RETURN $x", new Dictionary<string, object>
        {
            {"x", 1L}
        });

        writer.Write(new RunWithMetadataMessage(query, "my-database",
            Bookmarks.From(AsyncSessionTests.FakeABookmark(123)), TimeSpan.FromMinutes(1),
            new Dictionary<string, object>
            {
                {"username", "MollyMostlyWhite"}
            }, AccessMode.Write, null, null));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(3);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgRun);
        reader.ReadString().Should().Be("RETURN $x");
        reader.ReadMap().Should().HaveCount(1).And.Contain(new KeyValuePair<string, object>("x", 1L));
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                {"bookmarks", new[] {"bookmark-123"}},
                {"tx_timeout", 60_000L},
                {"tx_metadata", new Dictionary<string, object> {{"username", "MollyMostlyWhite"}}},
                {"db", "my-database"}
            });
    }

    [Fact]
    public void ShouldSerializeWithNotifications()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();

        var query = new Query("RETURN $x", new Dictionary<string, object>
        {
            {"x", 1L}
        });

        writer.Write(new RunWithMetadataMessage(query, "my-database",
            Bookmarks.From(AsyncSessionTests.FakeABookmark(123)), TimeSpan.FromMinutes(1),
            new Dictionary<string, object>
            {
                {"username", "MollyMostlyWhite"}
            }, AccessMode.Write, null, new[]{"test.filter.A", "test.filter.B"}));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(3);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgRun);
        reader.ReadString().Should().Be("RETURN $x");
        reader.ReadMap().Should().BeEquivalentTo(new Dictionary<string, object> { ["x"] = 1L});
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                ["bookmarks"] = new[] {"bookmark-123"},
                ["tx_timeout"] = 60_000L,
                ["tx_metadata"] = new Dictionary<string, object> { ["username"] = "MollyMostlyWhite"},
                ["db"]= "my-database",
                ["notifications"] = new[] {"test.filter.A", "test.filter.B"}
            });
    }
    [Fact]
    public void ShouldSerializeWithEmptyNotifications()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();

        var query = new Query("RETURN $x", new Dictionary<string, object>
        {
            {"x", 1L}
        });

        writer.Write(new RunWithMetadataMessage(query, "my-database",
            Bookmarks.From(AsyncSessionTests.FakeABookmark(123)), TimeSpan.FromMinutes(1),
            new Dictionary<string, object>
            {
                {"username", "MollyMostlyWhite"}
            }, AccessMode.Write, null, Array.Empty<string>()));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(3);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgRun);
        reader.ReadString().Should().Be("RETURN $x");
        reader.ReadMap().Should().BeEquivalentTo(new Dictionary<string, object> { ["x"] = 1L });
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                ["bookmarks"] = new[] { "bookmark-123" },
                ["tx_timeout"] = 60_000L,
                ["tx_metadata"] = new Dictionary<string, object> { ["username"] = "MollyMostlyWhite" },
                ["db"] = "my-database",
                ["notifications"] = Array.Empty<string>()
            });
    }
}