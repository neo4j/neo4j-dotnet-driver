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
            [Theory]
            [InlineData(new byte[] {0x00, 0x01, 0x80, 0x00, 0x00}, sbyte.MinValue)]
            [InlineData(new byte[] {0x00, 0x01, 0x7F, 0x00, 0x00}, sbyte.MaxValue)]
            [InlineData(new byte[] {0x00, 0x01, 0x00, 0x00, 0x00}, 0)]
            [InlineData(new byte[] {0x00, 0x01, 0xFF, 0x00, 0x00}, -1)]
            public void ShouldReturnTheCorrectValue(byte[] response, sbyte correctValue)
            {
                var clientMock = new Mock<ITcpSocketClient>();
                TestHelper.TcpSocketClientSetup.SetupClientReadStream(clientMock, response);

                var chunkedInput = new PackStreamV1ChunkedInput(clientMock.Object, new BigEndianTargetBitConverter());
                var actual = chunkedInput.ReadSByte();
                actual.Should().Be(correctValue); //, $"Got: {actual}, expected: {correctValue}");
            }
        }

        public class ReadBytesMethod
        {
            [Theory]
            //-----------------------|---head1--|----|---head2---|-----------|--msg end--|
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02})]
            public void ShouldReadMessageAcrossChunks(byte[] input, byte[] correctValue)
            {
                var clientMock = new Mock<ITcpSocketClient>();
                TestHelper.TcpSocketClientSetup.SetupClientReadStream(clientMock, input);

                var chunkedInput = new PackStreamV1ChunkedInput(clientMock.Object, new BigEndianTargetBitConverter());
                byte[] actual = new byte[3];
                chunkedInput.ReadBytes( actual );
                actual.Should().Equal(correctValue);
            }
        }
    }
}