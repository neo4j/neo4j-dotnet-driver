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
using Moq;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Messages
{
    public class FailureMessageTests
    {
        [Fact]
        public void ShouldHaveCorrectSerializer()
        {
            var message = new FailureMessage(null, null);
            message.Serializer.Should().BeOfType<FailureMessageSerializer>();
        }

        [Fact]
        public void ShouldIncludeValuesInToString()
        {
            var message = new FailureMessage("e.g.Code", "e.g.Message");
            message.ToString().Should().Be("FAILURE code=e.g.Code, message=e.g.Message");
        }

        [Fact]
        public void ShouldInvokeOnFailure()
        {
            var mockPipeline = new Mock<IResponsePipeline>();
            var message = new FailureMessage("e.g.Code", "e.g.Message");
            message.Dispatch(mockPipeline.Object);

            mockPipeline.Verify(x => x.OnFailure("e.g.Code", "e.g.Message"));
        }
    }
}
