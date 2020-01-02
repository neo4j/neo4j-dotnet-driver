// Copyright (c) 2002-2020 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class SocketExtensionTests
    {
        public class ReadMethod
        {
            [Fact]
            public void ShouldReadAll()
            {
                var input = new byte[] {1, 2, 3, 4, 5};
                var index = 0;
                var stream = new Mock<Stream>();
                stream.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Callback((byte[] buffer, int offset, int size) =>
                    {
                        Array.Copy(input, index++, buffer, offset, 1);
                    })
                    .Returns((byte[] buffer, int offset, int size) => 1);
                var actual = new byte[5];
                stream.Object.Read(actual);

                actual.Should().Equal(input);
            }

            [Fact]
            public void ShouldThrowExceptionIfNoDataInStream()
            {
                var input = new byte[] {1, 2, 3, 4, 5};
                var index = 0;
                var readSize = -1;
                var stream = new Mock<Stream>();
                stream.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Callback((byte[] buffer, int offset, int size) =>
                    {
                        if (index < input.Length)
                        {
                            readSize = 1;
                            Array.Copy(input, index++, buffer, offset, readSize);
                        }
                        else
                        {
                            readSize = 0;
                        }
                    })
                    .Returns((byte[] buffer, int offset, int size) => readSize);
                var actual = new byte[input.Length + 1];
                var exp = Record.Exception(() => stream.Object.Read(actual));
                exp.Should().BeOfType<IOException>();
                exp.Message.Should().Be("Failed to read more from input stream. Expected 6 bytes, received 5.");
            }
        }
    }
}
