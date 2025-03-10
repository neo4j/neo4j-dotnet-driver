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
using System.IO;
using FluentAssertions;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO.MessageSerializers;

public class RecordMessageSerializerTests
{
    [Fact]
    public void StructTagsAreSuccess()
    {
        RecordMessageSerializer.Instance.ReadableStructs.Should().ContainEquivalentOf(MessageFormat.MsgRecord);
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 2)]
    [InlineData(4, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 0)]
    [InlineData(6, 0)]
    public void ShouldDeserialize(int major, int minor)
    {
        using var memory = new MemoryStream();

        var boltProtocolVersion = new BoltProtocolVersion(major, minor);
        var format = new MessageFormat(boltProtocolVersion, TestDriverContext.MockContext);

        var psw = new PackStreamWriter(format, memory);
        psw.WriteList(new List<object> { 0, "a" });
        memory.Position = 0;

        var reader = new PackStreamReader(format, memory, new ByteBuffers());

        var message = RecordMessageSerializer.Instance.Deserialize(reader);

        message.Should().BeOfType<RecordMessage>().Which.Fields.Should().BeEquivalentTo(new object[] { 0L, "a" });
    }
}
