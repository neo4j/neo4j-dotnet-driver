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

using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Messages
{
    public class IgnoredMessageTests
    {
        [Fact]
        public void ShouldHaveCorrectSerializer()
        {
            IgnoredMessage.Instance.Serializer.Should().BeOfType<IgnoredMessageSerializer>();
        }

        [Fact]
        public void ShouldCallPipelineOnIgnored()
        {
            var pipeline = new Mock<IResponsePipeline>();

            IgnoredMessage.Instance.Dispatch(pipeline.Object);

            pipeline.Verify(x => x.OnIgnored());
        }

        [Fact]
        public void ShouldHaveIgnoredMessage()
        {
            IgnoredMessage.Instance.ToString().Should().Be("IGNORED");
        }
    }
}
