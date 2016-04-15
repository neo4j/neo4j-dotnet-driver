// Copyright (c) 2002-2016 "Neo Technology,"
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
