using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Neo4j.Driver.Internal.messaging;
using Neo4j.Driver.Internal.result;
using Sockets.Plugin.Abstractions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class SocketClientTests
    {
        private static void SetupResponse(Mock<ITcpSocketClient> mock, byte[] response = null)
        {
            var memoryStream = new MemoryStream();
            memoryStream.Write(response ?? new byte[] { 0, 0, 0, 1 });
            memoryStream.Flush();
            memoryStream.Position = 0;
            mock.Setup(c => c.ReadStream).Returns(memoryStream);
        }

        private static Mock<Stream> SetupWriteStreamMock(Mock<ITcpSocketClient> mock)
        {
            var mockedStream = new Mock<Stream>();
            mock.Setup(c => c.WriteStream).Returns(mockedStream.Object);

            return mockedStream;
        }

        public class StartMethod
        {
            [Theory]
            [InlineData(new byte[] {0,0,0,0})]
            [InlineData(new byte[] { 0, 0, 0, 2 })]
            public async Task ShouldThrowExceptionIfVersionIsNotSupported(byte[] response)
            {
                var mock = new Mock<ITcpSocketClient>();
                SetupWriteStreamMock(mock);
                SetupResponse(mock, response);
                SocketClient socketClient = new SocketClient(new Uri("bolt://localhost:1234"), null, mock.Object);

                var exception = await Record.ExceptionAsync(() => socketClient.Start());
                Assert.NotNull(exception);
                Assert.IsType<NotSupportedException>(exception);
            }
        }

        public class SendMethod
        {
            [Fact]
            public async Task ShouldSendMessagesAsExpected()
            {
                // Given
                var messages = new IMessage[] {new RunMessage("Run message 1"), new RunMessage("Run message 1") };
                byte[] expectedBytes = { 0x00, 0x22, 0xB2, 0x10, 0x8D, 0x52, 0x75, 0x6E, 0x20, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x20, 0x31, 0xA0, 0xB2, 0x10, 0x8D, 0x52, 0x75, 0x6E, 0x20, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x20, 0x31, 0xA0, 0x00, 0x00 };
                var expectedLength = expectedBytes.Length;
                expectedBytes = expectedBytes.PadRight(PackStreamV1ChunkedOutput.BufferSize);

                var mock = new Mock<ITcpSocketClient>();
                var response =
                    TestHelper.StringToByteArray(
                        "00 00 00 01" 
                        + "00 03 b1 70 a0 00 00" 
                        + "00 0f b1 70  a1 86 66 69  65 6c 64 73  91 83 6e 75 6d 00 00"
                        + "00 0f b1 70  a1 86 66 69  65 6c 64 73  91 83 6e 75 6d 00 00");
                SetupResponse(mock, response);
                var writeStream = SetupWriteStreamMock(mock);

                
                string received = string.Empty;
                
                writeStream
                    .Setup(s => s.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                    .Callback<byte[], int, int>((buffer, start, size) => received = $"{buffer.ToHexString(start, size)}");

                var messageHandler = new MessageResponseHandler();
                messageHandler.Register(new InitMessage("MyClient/1.0"));
                var rb = new ResultBuilder();
                messageHandler.Register(messages[0], rb);
                messageHandler.Register(messages[1], rb);

                SocketClient socketClient = new SocketClient( new Uri("bolt://localhost:1234"), null, mock.Object);
                await socketClient.Start();
                mock.ResetCalls();
                
                // When
                socketClient.Send( messages, messageHandler );

                // Then
                mock.Verify(c => c.WriteStream, Times.Exactly(2) /*write + flush*/);

                

                writeStream.Verify(c => c.Write(expectedBytes, 0, It.IsAny<int>()), Times.Once,
                    $"Received {received}{Environment.NewLine}Expected {expectedBytes.ToHexString(0, expectedLength)}");
            }
        }
    }
}
