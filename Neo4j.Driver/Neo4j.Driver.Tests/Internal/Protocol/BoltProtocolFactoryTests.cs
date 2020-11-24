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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Tests;
using Xunit;

namespace Neo4j.Driver.Internal.Protocol
{
    public class BoltProtocolFactoryTests
    {
        public class CreateMethod
        {
            [Fact]
            public void ShouldCreateBoltProtocolV3()
            {
                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock);
                var boltProtocol = BoltProtocolFactory.ForVersion(new BoltProtocolVersion(3, 0));
                boltProtocol.Should().BeOfType<BoltProtocolV3>();
            }

            [Fact]
            public void ShouldCreateBoltProtocolV4()
            {
                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock);
                var boltProtocol = BoltProtocolFactory.ForVersion(new BoltProtocolVersion(4, 0));
                boltProtocol.Should().BeOfType<BoltProtocolV4_0>();
            }

            [Fact]
            public void ShouldCreateBoltProtocolV4_1()
            {
                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock);
                var boltProtocol = BoltProtocolFactory.ForVersion(new BoltProtocolVersion(4, 1), new Dictionary<string, string>());
                boltProtocol.Should().BeOfType<BoltProtocolV4_1>();
            }

            [Fact]
            public void ShouldCreateBoltProtocolV4_2()
            {
                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock);
                var boltProtocol = BoltProtocolFactory.ForVersion(new BoltProtocolVersion(4, 2), new Dictionary<string, string>());
                boltProtocol.Should().BeOfType<BoltProtocolV4_2>();
            }

            [Fact]
            public void ShouldCreateBoltProtocolV4_3()
            {
                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock);
                var boltProtocol = BoltProtocolFactory.ForVersion(new BoltProtocolVersion(4, 3), new Dictionary<string, string>());
                boltProtocol.Should().BeOfType<BoltProtocolV4_3>();
            }


            [Theory]
            [InlineData(0, 0, "The Neo4j server does not support any of the protocol versions supported by this client")]
            [InlineData(1, 0, "Protocol error, server suggested unexpected protocol version: 1.0")]
            [InlineData(2, 0, "Protocol error, server suggested unexpected protocol version: 2.0")]            
            [InlineData(15, 0, "Protocol error, server suggested unexpected protocol version: 15.0")]            
            public void ShouldThrowExceptionIfVersionIsNotSupported(int majorVersion, int minorVersion, string errorMessage)
            {
                var version = new BoltProtocolVersion(majorVersion, minorVersion);
                var exception = Record.Exception(() => BoltProtocolFactory.ForVersion(version));
                exception.Should().BeOfType<NotSupportedException>();
                exception.Message.Should().StartWith(errorMessage);
            }

            [Theory]
            [InlineData(1213486160 /*HTTP*/, "Server responded HTTP.")]
            public void ShouldThrowExceptionIfSpecialVersionIsNotSupported(int largeVersion, string errorMessage)
            {
                var version = new BoltProtocolVersion(largeVersion);
                var exception = Record.Exception(() => BoltProtocolFactory.ForVersion(version));
                exception.Should().BeOfType<NotSupportedException>();
                exception.Message.Should().StartWith(errorMessage);
            }
        }
    }
}