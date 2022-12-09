// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Tests;
using Xunit;

namespace Neo4j.Driver.Internal.Protocol;

public class BoltProtocolFactoryTests
{
    public class CreateMethod
    {
        [Fact]
        public void ShouldCreateLegacyBoltProtocol()
        {
            var boltProtocol = BoltProtocolFactory.Default.ForVersion(BoltProtocolVersion.V30);
            boltProtocol.Should().Be(LegacyBoltProtocol.Instance);
        }

        [Theory]
        [InlineData(4, 1)]
        [InlineData(4, 2)]
        [InlineData(4, 3)]
        [InlineData(4, 4)]
        [InlineData(5, 0)]
        public void ShouldCreateBoltProtocol(int major, int minor)
        {
            var boltProtocol = BoltProtocolFactory.Default.ForVersion(new BoltProtocolVersion(major, minor));
            boltProtocol.Should().Be(BoltProtocol.Instance);
        }

        [Theory]
        // No-matches
        [InlineData(0, 0, "The Neo4j server does not support any of the protocol versions supported by this client. " +
            "Ensure that you are using driver and server versions that are compatible with one another.")]
        // Non-existent
        [InlineData(1, 0, "Protocol error, server suggested unexpected protocol version: 1.0")]
        [InlineData(2, 0, "Protocol error, server suggested unexpected protocol version: 2.0")]
        // Future protocol
        [InlineData(15, 0, "Protocol error, server suggested unexpected protocol version: 15.0")]
        // Deprecated protocol
        [InlineData(4, 0, "Protocol error, server suggested unexpected protocol version: 4.0")]
        public void ShouldThrowExceptionIfVersionIsNotSupported(int majorVersion, int minorVersion, string errorMessage)
        {
            var version = new BoltProtocolVersion(majorVersion, minorVersion);
            var exception = Record.Exception(() => BoltProtocolFactory.Default.ForVersion(version));
            exception.Should().BeOfType<NotSupportedException>().Which.Message.Should().Be(errorMessage);
        }

        [Fact]
        public void ShouldThrowExceptionIfHttpVersionSpecified()
        {
            var version = new BoltProtocolVersion(1213486160);
            var exception = Record.Exception(() => BoltProtocolFactory.Default.ForVersion(version));
            exception.Should().BeOfType<NotSupportedException>().Which.Message.Should().StartWith(
                "Server responded HTTP.");
        }
    }
}
