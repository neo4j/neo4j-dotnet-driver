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

using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO;

// The helper's only job is to build a stream, ask the factory for a reader/writer bound
// to it, invoke the caller's callback, and hand back the result - so these tests mock the
// factory entirely and never touch a real PackStreamWriter/PackStreamReader. Real
// serialization behaviour is covered end to end by PlaintextSerializerTests and
// EncryptedStructureCodecTests, which wire up the real factory.
public class PackStreamSerializationHelperTests
{
    private static readonly MessageFormat Format = new MessageFormatFactory(TestDriverContext.MockContext)
        .CreateMessageFormat(BoltProtocolVersion.V6_0);

    private readonly Mock<IPackStreamReaderWriterFactory> _factory = new();

    private PackStreamSerializationHelper CreateSubject() => new(_factory.Object);

    [Fact]
    public void Write_InvokesTheGivenActionExactlyOnceWithTheWriterFromTheFactory()
    {
        var writerFromFactory = Mock.Of<IPackStreamWriter>();
        _factory.Setup(f => f.CreateWriter(Format, It.IsAny<Stream>())).Returns(writerFromFactory);

        var invocations = new List<IPackStreamWriter>();
        CreateSubject().Write(Format, invocations.Add);

        invocations.Should().Equal(writerFromFactory);
    }

    [Fact]
    public void Write_ReturnsWhateverBytesEndUpInTheStreamGivenToTheFactory()
    {
        _factory.Setup(f => f.CreateWriter(Format, It.IsAny<Stream>()))
            .Returns((MessageFormat _, Stream stream) =>
            {
                stream.Write([0x2A]);
                return Mock.Of<IPackStreamWriter>();
            });

        var bytes = CreateSubject().Write(Format, _ => { });

        bytes.Should().Equal(0x2A);
    }

    [Fact]
    public void Read_PassesAStreamContainingTheGivenBytesToTheFactory()
    {
        byte[]? streamContents = null;
        _factory.Setup(f => f.CreateReader(Format, It.IsAny<MemoryStream>()))
            .Returns((MessageFormat _, MemoryStream stream) =>
            {
                streamContents = stream.ToArray();
                return Mock.Of<IPackStreamReader>();
            });

        var inputBytes = new byte[] { 0x2A, 0x2B };
        CreateSubject().Read(Format, inputBytes, _ => 0);

        streamContents.Should().Equal(inputBytes);
    }

    [Fact]
    public void Read_InvokesTheGivenFuncWithTheReaderFromTheFactoryAndReturnsItsResult()
    {
        var readerFromFactory = Mock.Of<IPackStreamReader>();
        _factory.Setup(f => f.CreateReader(Format, It.IsAny<MemoryStream>())).Returns(readerFromFactory);

        IPackStreamReader? receivedReader = null;
        var result = CreateSubject().Read(Format, [], r =>
        {
            receivedReader = r;
            return 42;
        });

        receivedReader.Should().BeSameAs(readerFromFactory);
        result.Should().Be(42);
    }
}
