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
using System.IO;
using System.Linq;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Packstream;
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