using System.IO;
using FluentAssertions;
using Moq;
using Sockets.Plugin.Abstractions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class ChunkedInputTests
    {
        public class ReadBSyteMethod
        {
            private static void SetupReadStreamResponse(Mock<ITcpSocketClient> mock, byte[] response)
            {
                var memoryStream = new MemoryStream();
                memoryStream.Write(response);
                memoryStream.Flush();
                memoryStream.Position = 0;
                mock.Setup(c => c.ReadStream).Returns(memoryStream);
            }

            [Theory]
            [InlineData(new byte[] {0x00, 0x01, 0x80, 0x00, 0x00}, sbyte.MinValue)]
            [InlineData(new byte[] {0x00, 0x01, 0x7F, 0x00, 0x00}, sbyte.MaxValue)]
            [InlineData(new byte[] {0x00, 0x01, 0x00, 0x00, 0x00}, 0)]
            [InlineData(new byte[] {0x00, 0x01, 0xFF, 0x00, 0x00}, -1)]
            public void ShouldReturnTheCorrectValue(byte[] response, sbyte correctValue)
            {
                var clientMock = new Mock<ITcpSocketClient>();
                SetupReadStreamResponse(clientMock, response);

                var chunkedInput = new PackStreamV1ChunkedInput(clientMock.Object, new BigEndianTargetBitConverter());
                var actual = chunkedInput.ReadSByte();
                actual.Should().Be(correctValue); //, $"Got: {actual}, expected: {correctValue}");
            }
        }
    }
}