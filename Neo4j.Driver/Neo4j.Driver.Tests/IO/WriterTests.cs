using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Tests.IO
{
    public class WriterTests
    {
        public class Mocks
        {
            private readonly Mock<Stream> _mockOutputStream;
            private readonly Queue<string> _receviedBytes = new Queue<string>();
            private readonly Queue<string> _receivedByteArrays = new Queue<string>();

            public Mocks()
            {
                _mockOutputStream = new Mock<Stream>();
                _mockOutputStream.Setup(s => s.CanWrite).Returns(true);
                _mockOutputStream
                    .Setup(s => s.WriteByte(It.IsAny<byte>()))
                    .Callback<byte>(b => _receviedBytes.Enqueue($"{b:X2}"));
                _mockOutputStream
                    .Setup(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Callback<byte[], int, int>((bArray, offset, count) => _receivedByteArrays.Enqueue($"{bArray.ToHexString(offset, count)}"));
            }

            public Stream OutputStream => _mockOutputStream.Object;

            public void VerifyWrite(byte expectedByte)
            {
                _mockOutputStream.Verify(c => c.WriteByte(expectedByte), Times.Once,
                    $"Received {_receviedBytes.Dequeue()}{Environment.NewLine}Expected {expectedByte:X2}");
            }

            public void VerifyWrite(byte[] expectedBytes)
            {
                _mockOutputStream.Verify(c => c.Write(expectedBytes, It.IsAny<int>(), It.IsAny<int>()), Times.Once,
                    $"Received {_receivedByteArrays.Dequeue()}{Environment.NewLine}Expected {expectedBytes.ToHexString()}");
            }
        }

    }
}
