// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using static Neo4j.Driver.Tests.TcpSocketClientTestSetup;

namespace Neo4j.Driver.Tests
{
    internal class SocketClientTestHarness : IDisposable
    {
        public SocketClient Client { get; }
        public Mock<Stream> MockWriteStream { get; private set; }
        public Mock<ITcpSocketClient> MockTcpSocketClient { get; }
        string _received = string.Empty;

        public SocketClientTestHarness(Uri uri=null)
        {
            MockTcpSocketClient = new Mock<ITcpSocketClient>();
            var socketSettings = new SocketSettings
            {
                EncryptionManager = new Mock<EncryptionManager>().Object,
                Ipv6Enabled = true,
                SocketKeepAliveEnabled = true,
                ConnectionTimeout = Config.InfiniteInterval
            };
            var bufferSettings = new BufferSettings(Config.DefaultConfig);
            Client = new SocketClient(uri, socketSettings, bufferSettings, new Mock<ILogger>().Object, MockTcpSocketClient.Object);
        }

        public async Task ExpectException<T>(Func<Task> func, string errorMessage=null) where T : Exception
        {
            var exception = await Record.ExceptionAsync(() => func());
            Assert.NotNull(exception);
            Assert.IsType<T>(exception);
            if (errorMessage != null)
            {
                exception.Message.Should().Contain(errorMessage);
            }
        }

        public void SetupReadStream(string hexBytes)
        {
            SetupReadStream(hexBytes.ToByteArray());
        }

        public void ResetCalls()
        {
            MockTcpSocketClient.ResetCalls();
            MockWriteStream.ResetCalls();
        }

        public void SetupWriteStream()
        {
            MockWriteStream = CreateWriteStreamMock(MockTcpSocketClient);

            MockWriteStream
                .Setup(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<byte[], int, int>((buffer, start, size) => _received += $"{buffer.ToHexString(start, size)}");
        }

        public void VerifyWriteStreamUsages(int count)
        {
            MockTcpSocketClient.Verify(c => c.WriteStream, Times.Exactly(count));
        }

        public void VerifyWriteStreamContent(byte[] expectedBytes, int expectedLength)
        {
            Assert.Equal(expectedBytes.ToHexString(0, expectedLength), _received);

            MockWriteStream.Verify(c => c.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Client.Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void SetupReadStream(byte[] bytes)
        {
            CreateReadStreamMock(MockTcpSocketClient, bytes);
        }
    }

    internal static class TcpSocketClientTestSetup
    {
        public static void SetupClientWithReadStream(Mock<ITcpSocketClient> mock, byte[] response)
        {
            var memoryStream = new MemoryStream();
            memoryStream.Write(response);
            memoryStream.Flush();
            memoryStream.Position = 0;
            mock.Setup(c => c.ReadStream).Returns(memoryStream);

            var writeStream = new MemoryStream();
            mock.Setup(c => c.WriteStream).Returns(writeStream);
        }

        public static void CreateReadStreamMock(Mock<ITcpSocketClient> mock, byte[] response)
        {
            var memoryStream = new MemoryStream();
            memoryStream.Write(response);
            memoryStream.Flush();
            memoryStream.Position = 0;

            mock.Setup(c => c.ReadStream).Returns(memoryStream);
        }

        public static Mock<Stream> CreateWriteStreamMock(Mock<ITcpSocketClient> mock)
        {
            var mockedStream = new Mock<Stream>();

            //mockedStream.Setup(x => x.Seek(It.IsAny<long>(), It.IsAny<SeekOrigin>())).CallBase();
            mockedStream.Setup(x => x.CanWrite).Returns(true);
            //mockedStream.Setup(x => x.CanWrite).CallBase();
            //mockedStream.Setup(x => x.Length).CallBase();
            //mockedStream.Setup(x => x.Position).CallBase();
            //mockedStream.Setup(x => x.NextByte()).CallBase();
            //mockedStream.Setup(x => x.ToArray()).CallBase();
            //mockedStream.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).CallBase();

            mock.Setup(c => c.WriteStream).Returns(mockedStream.Object);

            return mockedStream;
        }
    }
}
