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
using System.IO;
using FluentAssertions;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO.MessageSerializers;

public class HelloMessageSerializerTests
{
    [Fact]
    public void ShouldHaveWritableTypesAsHelloMessage()
    {
        HelloMessageSerializer.Instance.WritableTypes.Should().ContainEquivalentOf(typeof(HelloMessage));
    }

    [Fact]
    public void ShouldThrowIfPassedWrongMessage()
    {
        Record.Exception(() => HelloMessageSerializer.Instance.Serialize(null, RollbackMessage.Instance))
            .Should()
            .BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 2)]
    [InlineData(4, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 0)]
    public void ShouldSerialize(int major, int minor)
    {
        using var memory = new MemoryStream();

        var boltProtocolVersion = new BoltProtocolVersion(major, minor);
        var format = new MessageFormat(boltProtocolVersion, TestDriverContext.MockContext);

        var psw = new PackStreamWriter(format, memory);

        HelloMessageSerializer.Instance.Serialize(
            psw,
            new HelloMessage(boltProtocolVersion, "user", null, null as IDictionary<string, string>));

        memory.Position = 0;

        var reader = new PackStreamReader(format, memory, new ByteBuffers());

        var bytes = reader.ReadBytes(2);
        bytes[0].Should().Be(0xB1);
        bytes[1].Should().Be(0x01);

        var meta = reader.ReadMap();
        meta.Should().ContainKey("user_agent").WhoseValue.Should().Be("user");
    }
}
