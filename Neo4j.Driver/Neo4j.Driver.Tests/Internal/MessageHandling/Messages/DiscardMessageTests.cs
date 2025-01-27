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

public class DiscardMessageTests
{
    [Fact]
    public void ShouldHaveCorrectSerializer()
    {
        var message = new DiscardMessage(10);
        message.Serializer.Should().BeOfType<DiscardMessageSerializer>();
    }

    [Fact]
    public void ShouldSkipNullQueryId()
    {
        var message = new DiscardMessage(10);
        message.ToString().Should().Be("DISCARD [{n, 10}]");
    }

    [Fact]
    public void ShouldHandleValues()
    {
        var message = new DiscardMessage(42, 10);
        message.ToString().Should().Be("DISCARD [{n, 10}, {qid, 42}]");
    }
}
