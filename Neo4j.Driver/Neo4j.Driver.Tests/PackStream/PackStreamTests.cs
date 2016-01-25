using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace Neo4j.Driver.Tests
{
    public partial class PackStreamTests
    {
        /*Utilities*/

//        public class ByteArrayInputStream : IInputStream
//        {
//            private 
//            public byte[] Bytes { get; set; }
//            private int index = 0;
//
//            public sbyte ReadSByte()
//            {
//                AssertByteAvailable();
//                return (sbyte)Bytes[index++];
//                
//            }
//
//            private void AssertByteAvailable()
//            {
//                if (index < Bytes.Length)
//                {
//                    throw new ArgumentOutOfRangeException("Wrong!!");
//                }
//            }
//
//            public byte ReadByte()
//            {
//                Mock<IInputStream> i = new Mock<IInputStream>();
//
//                i.Setup(x => x.ReadSByte())
//                    .Returns(3);
//                AssertByteAvailable();
//                return Bytes[index++];
//            }
//
//            public short ReadShort()
//            {
//                throw new NotImplementedException();
//            }
//
//            public int ReadInt()
//            {
//                throw new NotImplementedException();
//            }
//
//            public void ReadBytes(byte[] buffer, int size = 0, int? length = null)
//            {
//                throw new NotImplementedException();
//            }
//
//            public byte PeekByte()
//            {
//                throw new NotImplementedException();
//            }
//
//            public void ReadMessageTail()
//            {
//                throw new NotImplementedException();
//            }
//
//            public long ReadLong()
//            {
//                throw new NotImplementedException();
//            }
//
//            public double ReadDouble()
//            {
//                throw new NotImplementedException();
//            }
//        }
    }
}
