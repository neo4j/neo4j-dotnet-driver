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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Messages
{
    public class HelloMessageTests
    {
        [Fact]
        public void ShouldHaveCorrectSerializer()
        {
            var helloMessage = new HelloMessage(BoltProtocolVersion.V3_0, null, null, null);
            helloMessage.Serializer.Should().BeOfType<HelloMessageSerializer>();
        }

        [Theory]
        [InlineData(3, 0)]
        [InlineData(4, 0)]
        public void ShouldHandleNullValues(int major, int minor)
        {
            var helloMessage = new HelloMessage(new BoltProtocolVersion(major, minor), null, null, null);
            helloMessage.Serializer.Should().BeOfType<HelloMessageSerializer>();
            helloMessage.ToString().Should().Be("HELLO [{user_agent, NULL}]");
        }

        [Theory]
        [InlineData(3, 0)]
        [InlineData(4, 0)]
        public void ShouldHandleValues(int major, int minor)
        {
            var helloMessage = new HelloMessage(new BoltProtocolVersion(major, minor), "jeff", null, null);
            helloMessage.Serializer.Should().BeOfType<HelloMessageSerializer>();
            helloMessage.Metadata.Should().ContainKey("user_agent").WhichValue.Should().Be("jeff");
            helloMessage.ToString().Should().Be("HELLO [{user_agent, jeff}]");
        }

        [Theory]
        [InlineData(4, 1)]
        [InlineData(4, 2)]
        public void ShouldIncludeRoutingKeyAboveV40(int major, int minor)
        {
            var helloMessage = new HelloMessage(new BoltProtocolVersion(major, minor), null, null, null);
            helloMessage.Serializer.Should().BeOfType<HelloMessageSerializer>();

            helloMessage.ToString().Should().Be("HELLO [{user_agent, NULL}, {routing, NULL}]");
        }

        [Theory]
        [InlineData(4, 1)]
        [InlineData(4, 2)]
        [InlineData(5, 0)]
        public void ShouldIncludeRoutingDetailsAboveV40(int major, int minor)
        {
            var meta = new Dictionary<string, string>
            {
                ["a"] = "b"
            };

            var helloMessage = new HelloMessage(new BoltProtocolVersion(major, minor), null, null, meta);
            helloMessage.Serializer.Should().BeOfType<HelloMessageSerializer>();

            helloMessage.ToString().Should().Be("HELLO [{user_agent, NULL}, {routing, [{a, b}]}]");
        }

        [Theory]
        [InlineData(4, 3)]
        [InlineData(4, 4)]
        public void ShouldIncludePatchBolt(int major, int minor)
        {
            var helloMessage = new HelloMessage(new BoltProtocolVersion(major, minor), null, null, null);
            helloMessage.Serializer.Should().BeOfType<HelloMessageSerializer>();

            helloMessage.ToString().Should().Be("HELLO [{user_agent, NULL}, {routing, NULL}, {patch_bolt, [utc]}]");
        }

        [Theory]
        [InlineData(5, 0)]
        [InlineData(6, 0)]
        public void ShouldIncludeAuth(int major, int minor)
        {
            var helloMessage = new HelloMessage(
                new BoltProtocolVersion(major, minor),
                null,
                AuthTokens.Basic("jeff", "hidden").AsDictionary(),
                null);

            helloMessage.Serializer.Should().BeOfType<HelloMessageSerializer>();
            helloMessage.Metadata.Should().ContainKey("scheme").WhichValue.Should().Be("basic");
            helloMessage.Metadata.Should().ContainKey("principal").WhichValue.Should().Be("jeff");
            helloMessage.Metadata.Should().ContainKey("credentials").WhichValue.Should().Be("hidden");
            helloMessage.ToString()
                .Should()
                .Be(
                    "HELLO [{scheme, basic}, {principal, jeff}, {credentials, ******}, {user_agent, NULL}, {routing, NULL}]");
        }
    }
}
