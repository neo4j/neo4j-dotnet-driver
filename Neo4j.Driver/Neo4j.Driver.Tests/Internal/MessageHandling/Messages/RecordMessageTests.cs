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
    public class RecordMessageTests
    {
        [Fact]
        public void ShouldHaveCorrectSerializer()
        {
            var message = new RecordMessage(new object[]{});
            message.Serializer.Should().BeOfType<RecordMessageSerializer>();
        }

        [Fact]
        public void ShouldDispatchToPipelineOnRecord()
        {
            var pipeline = new Mock<IResponsePipeline>();
            var fields = new object[] {1, "hello"};
            var message = new RecordMessage(fields);

            message.Dispatch(pipeline.Object);

            pipeline.Verify(x => x.OnRecord(fields));
        }

        [Fact]
        public void ShouldHaveRecordMessage()
        {
            new RecordMessage(new object[] { 1, "hello" }).ToString().Should().Be("RECORD [1, hello]");
        }
    }
}
