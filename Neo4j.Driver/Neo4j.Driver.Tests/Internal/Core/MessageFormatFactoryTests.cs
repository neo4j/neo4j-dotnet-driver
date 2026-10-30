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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Core;

public class MessageFormatFactoryTests
{
    private readonly MessageFormatFactory _subject = new(TestDriverContext.MockContext);

    [Theory]
    [InlineData(4, 4)]
    [InlineData(5, 0)]
    [InlineData(6, 0)]
    public void CreateMessageFormat_ProducesFormatForRequestedVersion(int major, int minor)
    {
        var version = new BoltProtocolVersion(major, minor);

        var format = _subject.CreateMessageFormat(version);

        format.Version.Should().Be(version);
    }

    [Fact]
    public void CreateMessageFormat_ReturnsANewInstanceEachCall()
    {
        var first = _subject.CreateMessageFormat(BoltProtocolVersion.V6_0);
        var second = _subject.CreateMessageFormat(BoltProtocolVersion.V6_0);

        second.Should().NotBeSameAs(first);
    }
}
