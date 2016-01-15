using System;
using System.IO;
using System.Linq;
using Moq;
using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver.Tests
{
    public static class TestHelper
    {


        public static class TcpSocketClientSetup
        {
            public static void SetupClientReadStream(Mock<ITcpSocketClient> mock, byte[] response)
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
                mock.Setup(c => c.WriteStream).Returns(mockedStream.Object);

                return mockedStream;
            }
        }
    }
}