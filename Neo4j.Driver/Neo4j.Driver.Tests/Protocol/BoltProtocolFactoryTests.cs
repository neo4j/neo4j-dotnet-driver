// Copyright (c) 2002-2018 "Neo4j,"
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
using System.IO;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class BoltProtocolFactoryTests
    {
        public class CreateMethod
        {
            [Fact]
            public void ShouldCreateBoltProtocolV1()
            {
                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock);
                var boltProtocol = BoltProtocolFactory.Create(1, connMock.Object, new BufferSettings(Config.DefaultConfig));
                boltProtocol.Should().BeOfType<BoltProtocolV1>();
            }

            [Fact]
            public void ShouldCreateBoltProtocolV2()
            {
                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock);
                var boltProtocol = BoltProtocolFactory.Create(2, connMock.Object, new BufferSettings(Config.DefaultConfig));
                boltProtocol.Should().BeOfType<BoltProtocolV2>();
            }

            [Theory]
            [InlineData(0, "The Neo4j server does not support any of the protocol versions supported by this client")]
            [InlineData(1024, "Protocol error, server suggested unexpected protocol version: 1024")]
            [InlineData(1213486160 /*HTTP*/, "Server responded HTTP.")]
            public void ShouldThrowExceptionIfVersionIsNotSupported(int version, string errorMessage)
            {
                var exception = Record.Exception(() => BoltProtocolFactory.Create(version, null, null));
                exception.Should().BeOfType<NotSupportedException>();
                exception.Message.Should().StartWith(errorMessage);
            }
        }
    }
}
