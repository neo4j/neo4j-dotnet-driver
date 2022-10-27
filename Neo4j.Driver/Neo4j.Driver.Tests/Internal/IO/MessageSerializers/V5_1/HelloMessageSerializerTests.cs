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
using Xunit;

namespace Neo4j.Driver.Internal.IO.MessageSerializers.V5_1;

public class HelloMessageSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new HelloMessageSerializer();

    [Fact]
    public void ShouldThrowOnDeserialize()
    {
        var handler = SerializerUnderTest;

        var ex = Record.Exception(() =>
            handler.Deserialize(Mock.Of<IPackStreamReader>(), BoltProtocolV3MessageFormat.MsgBegin, 2));

        ex.Should().NotBeNull();
        ex.Should().BeOfType<ProtocolException>();
    }

    [Fact]
    public void ShouldSerializeAndIgnoreNullNotifications()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();
        var authToken = new Dictionary<string, object> { {"scheme", "basic"},
            {"principal", "username"},
            {"credentials", "password"} };
        var routingContext = new Dictionary<string, string> { { "contextKey", "contextValue" } };

        writer.Write(new HelloMessage("Client-Version/1.0", authToken, routingContext, null));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgHello);
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                ["user_agent"] = "Client-Version/1.0",
                ["scheme"] = "basic",
                ["principal"] = "username",
                ["credentials"] = "password",
                ["routing"] = new Dictionary<string, object> { ["contextKey"] = "contextValue" }
            });
    }

    [Fact]
    public void ShouldIncludeEmptyNotificationsArray()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();
        var authToken = new Dictionary<string, object> { {"scheme", "basic"},
            {"principal", "username"},
            {"credentials", "password"} };
        var routingContext = new Dictionary<string, string> { { "contextKey", "contextValue" } };

        writer.Write(new HelloMessage("Client-Version/1.0", authToken, routingContext, Array.Empty<string>()));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgHello);
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                ["user_agent"] = "Client-Version/1.0",
                ["scheme"] = "basic",
                ["principal"] = "username",
                ["credentials"] = "password",
                ["routing"] = new Dictionary<string, object> {["contextKey"] = "contextValue" },
                ["notifications"] = Array.Empty<string>()
            });
    }

    [Fact]
    public void ShouldIncludeNotificationsArrayWithItems()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();
        var authToken = new Dictionary<string, object> { {"scheme", "basic"},
            {"principal", "username"},
            {"credentials", "password"} };
        var routingContext = new Dictionary<string, string> { { "contextKey", "contextValue" } };

        writer.Write(new HelloMessage("Client-Version/1.0", authToken, routingContext, new[] {"WARNING.*"}));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgHello);
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                ["user_agent"] = "Client-Version/1.0",
                ["scheme"] = "basic",
                ["principal"] = "username",
                ["credentials"]= "password",
                ["routing"] = new Dictionary<string, object>{["contextKey"] = "contextValue"},
                ["notifications"] = new [] { "WARNING.*"}
            });
    }

    [Fact]
    public void ShouldIncludeNotificationsArrayWithManyItems()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();
        var authToken = new Dictionary<string, object> { {"scheme", "basic"},
            {"principal", "username"},
            {"credentials", "password"} };

        writer.Write(new HelloMessage("Client-Version/1.0", authToken, null, new[] { "WARNING.*", "INFORMATION.*" }));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgHello);
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                ["user_agent"] = "Client-Version/1.0",
                ["scheme"] = "basic",
                ["principal"] = "username",
                ["credentials"] = "password",
                ["routing"] = null,
                ["notifications"] = new[] { "WARNING.*", "INFORMATION.*" }
            });
    }

    [Fact]
    public void ShouldSerializeOnlyNotifications()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();

        writer.Write(new HelloMessage("Client-Version/1.0", null, null, new[] { "WARNING.*" }));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgHello);
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                ["user_agent"] = "Client-Version/1.0",
                ["routing"] = null,
                ["notifications"] = new[] { "WARNING.*" }
            });
    }

    [Fact]
    public void ShouldSerializeEmptyMapWhenAuthTokenIsNull()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();
        var routingContext = new Dictionary<string, string> { { "contextKey", "contextValue" } };

        writer.Write(new HelloMessage("Client-Version/1.0", null, routingContext, null));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgHello);
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                ["user_agent"] = "Client-Version/1.0",
                ["routing"] = new Dictionary<string, object> { ["contextKey"] = "contextValue" },
            });
    }

    [Fact]
    public void ShouldSerializeWithNullRoutingContext()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();

        writer.Write(new HelloMessage("Client-Version/1.0", null, null, null));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgHello);
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                ["user_agent"] = "Client-Version/1.0",
                ["routing"] = null
            });
    }

    [Fact]
    public void ShouldSerializeWithEmptyRoutingContext()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer();

        writer.Write(new HelloMessage("Client-Version/1.0", null, new Dictionary<string, string>(), null));

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgHello);
        reader.ReadMap().Should().BeEquivalentTo(
            new Dictionary<string, object>
            {
                ["user_agent"] = "Client-Version/1.0",
                ["routing"] = new Dictionary<string, object>()
            });
    }
}