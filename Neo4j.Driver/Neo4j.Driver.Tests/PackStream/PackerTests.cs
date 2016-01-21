using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Sockets.Plugin.Abstractions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public partial class PackStreamTests
    {

        public class PackerTests
        {
            private class Mocks
            {
               public Mock<IOutputStream> MockOutputStream { get; }
                public IOutputStream OutputStream => MockOutputStream.Object;

                public string Received { get; set; }
                public string ReceivedByteArray { get; set; }

                public Mocks()
                {
                    MockOutputStream = new Mock<IOutputStream>();

                    MockOutputStream
                        .Setup(s => s.Write(It.IsAny<byte>(), It.IsAny<byte[]>()))
                        .Callback<byte, byte[]>((b, bArray) => Received = $"{b.ToString("X2")} {bArray.ToHexString(0)}")
                        .Returns(OutputStream);

                    MockOutputStream
                        .Setup(s => s.Write(It.IsAny<byte[]>()))
                        .Callback< byte[]>(( bArray) => ReceivedByteArray = $"{bArray.ToHexString(0)}")
                        .Returns(OutputStream);
                }

                public void VerifyWrite(byte b, params byte[] bytes)
                {
                    byte[] expectedBytes = bytes;
                    MockOutputStream.Verify(c => c.Write(b, bytes ?? It.IsAny<byte[]>()), Times.Once,
                        $"Received {Received}{Environment.NewLine}Expected {b.ToString("X2")} {expectedBytes.ToHexString(0)}");
                }

                public void VerifyWrite(byte[] bytes)
                {
                    byte[] expectedBytes = bytes;
                    MockOutputStream.Verify(c => c.Write( bytes ?? It.IsAny<byte[]>()), Times.Once,
                        $"Received {ReceivedByteArray}{Environment.NewLine}Expected {expectedBytes.ToHexString(0)}");
                }
            }

            public class PackNullMethod
            {
                [Fact]
                public void ShouldUnpackNullSuccessfully()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.PackNull();
                    mocks.VerifyWrite(PackStream.NULL);
                }


            }

            public class PackRawMethod
            {
                [Fact]
                public void ShouldUnpacPawBytesSuccessfully()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    var bytes = new byte[] { 1, 2, 3 };
                    u.PackRaw(bytes);
                    mocks.VerifyWrite(bytes);
                }
            }

            public class PackLongMethod
            {
                [Theory]
                [InlineData(PackStream.MINUS_2_TO_THE_4, 0xF0, null)]
                [InlineData(PackStream.PLUS_2_TO_THE_7 - 1, 0x7F, null)]
                [InlineData(PackStream.MINUS_2_TO_THE_7, PackStream.INT_8, "80")]
                [InlineData(PackStream.MINUS_2_TO_THE_4 - 1, PackStream.INT_8, "EF")]
                [InlineData(PackStream.MINUS_2_TO_THE_15, PackStream.INT_16, "80 00")]
                [InlineData(PackStream.PLUS_2_TO_THE_15 - 1, PackStream.INT_16, "7F FF")]
                [InlineData(PackStream.MINUS_2_TO_THE_31, PackStream.INT_32, "80 00 00 00")]
                [InlineData(PackStream.PLUS_2_TO_THE_31 - 1, PackStream.INT_32, "7F FF FF FF")]
                [InlineData(long.MinValue, PackStream.INT_64, "80 00 00 00 00 00 00 00")]
                [InlineData(long.MaxValue, PackStream.INT_64, "7F FF FF FF FF FF FF FF")]
                public void ShouldUnpackNullSuccessfully(long input, byte marker, string expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.Pack(input);
                    mocks.VerifyWrite(marker);
                    if (expected != null)
                    {
                        mocks.VerifyWrite(expected.ToByteArray());
                    }
                }
            }

            public class PackDoubleMethod
            {
                [Theory]
                [InlineData(1.2, "3F F3 33 33 33 33 33 33")]
                public void ShouldUnpackNullSuccessfully(double input, string expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.Pack(input);
                    mocks.VerifyWrite(PackStream.FLOAT_64);
                    mocks.VerifyWrite(expected.ToByteArray());
                }
            }
        }
    }
}
