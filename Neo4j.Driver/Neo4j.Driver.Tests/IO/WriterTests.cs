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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using TaskExtensions = Neo4j.Driver.Internal.TaskExtensions;

namespace Neo4j.Driver.Tests.IO
{
    public class WriterTests
    {
        public class Mocks
        {
            private readonly Mock<Stream> _mockOutputStream;
            private readonly Queue<string> _receviedBytes = new Queue<string>();
            private readonly Queue<string> _receivedByteArrays = new Queue<string>();
            private readonly StringBuilder _receivedBytesAccumulated = new StringBuilder(); 

            public Mocks()
            {
                _mockOutputStream = new Mock<Stream>();
                _mockOutputStream.Setup(s => s.CanWrite).Returns(true);
                _mockOutputStream
                    .Setup(s => s.WriteByte(It.IsAny<byte>()))
                    .Callback<byte>(b =>
                    {
                        var hex = $"{b:X2}";
                        _receviedBytes.Enqueue(hex);
                        _receivedBytesAccumulated.Append(hex);
                    });
                _mockOutputStream
                    .Setup(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Callback<byte[], int, int>((bArray, offset, count) =>
                    {
                        var hex = bArray.ToHexString(offset, count);
                        _receivedByteArrays.Enqueue(hex);
                        _receivedBytesAccumulated.Append(hex);
                    });
                _mockOutputStream
                    .Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Callback<byte[], int, int, CancellationToken>((bArray, offset, count, token) =>
                    {
                        var hex = bArray.ToHexString(offset, count);
                        _receivedByteArrays.Enqueue(hex);
                        _receivedBytesAccumulated.Append(hex);
                    })
                    .Returns(TaskExtensions.GetCompletedTask());
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

            public void VerifyResult(string expectedBytesAsHexString)
            {
                string actual = _receivedBytesAccumulated.ToString().Replace(" ", "").ToLowerInvariant();
                string expected = expectedBytesAsHexString.Replace(" ", "").ToLowerInvariant();

                actual.Should().Be(expected);
            }

            public void VerifyResult(params byte[] expectedBytes)
            {
                VerifyResult(expectedBytes.ToHexString());
            }
        }

    }
}
