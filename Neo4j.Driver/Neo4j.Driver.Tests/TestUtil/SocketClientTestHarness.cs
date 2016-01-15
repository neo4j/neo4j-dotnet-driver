using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using Sockets.Plugin.Abstractions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class SocketClientTestHarness : IDisposable
    {
        public SocketClient Client { get; }
        public Mock<Stream> MockWriteStream { get; }
        public Mock<ITcpSocketClient> MockTcpSocketClient { get; }
        string _received = String.Empty;

        public SocketClientTestHarness(Uri uri, Config config = null)
        {
            MockTcpSocketClient = new Mock<ITcpSocketClient>();
            MockWriteStream = TestHelper.TcpSocketClientSetup.CreateWriteStreamMock(MockTcpSocketClient);
            Client = new SocketClient(uri, config, MockTcpSocketClient.Object);
               
        }

        public async Task ExpectException<T>(Func<Task> func) where T : Exception
        {
            var exception = await Record.ExceptionAsync(() => func());
            Assert.NotNull(exception);
            Assert.IsType<T>(exception);
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
            MockWriteStream
                .Setup(s => s.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .Callback<byte[], int, int>((buffer, start, size) => _received = $"{buffer.ToHexString(start, size)}");
        }

        public void VerifyWriteStreamUsages(int count)
        {
            MockTcpSocketClient.Verify(c => c.WriteStream, Times.Exactly(2));
        }

        public void VerifyWriteStreamContent(byte[] expectedBytes, int expectedLength)
        {
            MockWriteStream.Verify(c => c.Write(expectedBytes, 0, It.IsAny<int>()), Times.Once,
                $"Received {_received}{Environment.NewLine}Expected {expectedBytes.ToHexString(0, expectedLength)}");
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
            TestHelper.TcpSocketClientSetup.SetupClientReadStream(MockTcpSocketClient, bytes);
        }
    }
}