// Copyright (c) 2002-2018 "Neo Technology,"
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
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using static Neo4j.Driver.Tests.TcpSocketClientTestSetup;

namespace Neo4j.Driver.Tests
{
    internal static class TcpSocketClientTestSetup
    {
        public static void CreateReadStreamMock(Mock<ITcpSocketClient> mock, byte[] response)
        {
            var memoryStream = new MemoryStream();
            memoryStream.Write(response);
            memoryStream.Flush();
            memoryStream.Position = 0;

            mock.Setup(c => c.ReadStream).Returns(memoryStream);
        }

        public static Mock<Stream> CreateReadStreamMock(Mock<ITcpSocketClient> mock)
        {
            var mockedStream = new Mock<Stream>();
            mockedStream.Setup(x => x.CanRead).Returns(true);
            mock.Setup(c => c.ReadStream).Returns(mockedStream.Object);
            return mockedStream;
        }

        public static Mock<Stream> CreateWriteStreamMock(Mock<ITcpSocketClient> mock)
        {
            var mockedStream = new Mock<Stream>();
            mockedStream.Setup(x => x.CanWrite).Returns(true);
            mock.Setup(c => c.WriteStream).Returns(mockedStream.Object);

            return mockedStream;
        }
    }
}
