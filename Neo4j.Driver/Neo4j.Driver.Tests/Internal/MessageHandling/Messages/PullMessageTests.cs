// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using FluentAssertions;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.MessageHandling.Messages;

public class PullMessageTests
{
    [Fact]
    public void ShouldHaveCorrectSerializer()
    {
        var message = new PullMessage(10);
        message.Serializer.Should().BeOfType<PullMessageSerializer>();
    }

    [Fact]
    public void ShouldHandleNullValue()
    {
        var message = new PullMessage(10);

        message.Metadata.Should().ContainKey("n").WhoseValue.Should().Be(10);
        message.Metadata.Should().NotContainKey("qid");
        message.ToString().Should().Be("PULL [{n, 10}]");
    }

    [Fact]
    public void ShouldHandleQueryId()
    {
        var message = new PullMessage(42, 10);
        message.Metadata.Should().ContainKey("n").WhoseValue.Should().Be(10);
        message.Metadata.Should().ContainKey("qid").WhoseValue.Should().Be(42);
        message.ToString().Should().Be("PULL [{n, 10}, {qid, 42}]");
    }
}
