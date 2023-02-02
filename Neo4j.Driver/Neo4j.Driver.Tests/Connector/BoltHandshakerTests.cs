// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class BoltHandshakerTests
    {
        [Fact]
        public async Task DoHandshakeAsyncShouldReturnBoltVersion()
        {
            var socket = new Mock<ITcpSocketClient>();
            var writerStream = new MemoryStream();
            socket.SetupGet(x => x.WriterStream).Returns(writerStream);
            var readerStream = new MemoryStream(new byte[] { 0x00, 0x00, 4, 4 });
            socket.SetupGet(x => x.ReaderStream).Returns(readerStream);

            var boltProtocolVersion = await BoltHandshaker.Default.DoHandshakeAsync(
                socket.Object,
                new Mock<ILogger>().Object,
                CancellationToken.None);

            boltProtocolVersion.Should().Equals(new BoltProtocolVersion(4, 4));
        }

        [Fact]
        public async Task DoHandshakeAsyncShouldThrowIfNotCorrectLengthResult()
        {
            var socket = new Mock<ITcpSocketClient>();
            var writerStream = new MemoryStream();
            socket.SetupGet(x => x.WriterStream).Returns(writerStream);
            var readerStream = new MemoryStream(new byte[] { 0x00, 0x00, 4 });
            socket.SetupGet(x => x.ReaderStream).Returns(readerStream);

            var exception = await Record.ExceptionAsync(
                () => BoltHandshaker.Default.DoHandshakeAsync(
                    socket.Object,
                    new Mock<ILogger>().Object,
                    CancellationToken.None));

            exception.Should().BeOfType<IOException>();
        }
    }
}
