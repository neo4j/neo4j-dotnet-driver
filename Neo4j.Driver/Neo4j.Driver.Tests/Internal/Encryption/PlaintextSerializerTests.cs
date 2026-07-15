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

#nullable enable

using System;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class PlaintextSerializerTests
{
    private readonly Mock<IMessageFormatFactory> _messageFormatFactory = new();
    private readonly Mock<IPackStreamSerializationHelper> _packStreamHelper = new();
    private readonly MessageFormat _format = new MessageFormatFactory(TestDriverContext.MockContext)
        .CreateMessageFormat(BoltProtocolVersion.V6_0);

    private PlaintextSerializer CreateSubject()
    {
        _messageFormatFactory.Setup(f => f.CreateMessageFormat(It.IsAny<BoltProtocolVersion>())).Returns(_format);
        return new PlaintextSerializer(_messageFormatFactory.Object, _packStreamHelper.Object);
    }

    [Fact]
    public void Serialize_WritesTheValueThroughTheHelperAndReturnsItsBytes()
    {
        const long value = 42L;
        var expectedBytes = new byte[] { 0xAA };
        var writer = new Mock<IPackStreamWriter>();

        _packStreamHelper
            .Setup(h => h.Write(_format, It.IsAny<Action<IPackStreamWriter>>()))
            .Returns((MessageFormat _, Action<IPackStreamWriter> write) =>
            {
                write(writer.Object);
                return expectedBytes;
            });

        var result = CreateSubject().Serialize(value);

        result.Should().BeSameAs(expectedBytes);
        writer.Verify(w => w.Write(value), Times.Once);
    }

    [Fact]
    public void Deserialize_ReadsThroughTheHelperAndReturnsItsResult()
    {
        const long expectedValue = 42L;
        var plaintext = new byte[] { 0x2A };
        var reader = new Mock<IPackStreamReader>();
        reader.Setup(r => r.Read()).Returns(expectedValue);

        _packStreamHelper
            .Setup(h => h.Read(_format, plaintext, It.IsAny<Func<IPackStreamReader, object>>()))
            .Returns((MessageFormat _, byte[] _, Func<IPackStreamReader, object> read) => read(reader.Object));

        var result = CreateSubject().Deserialize(plaintext);

        result.Should().Be(expectedValue);
    }
}
