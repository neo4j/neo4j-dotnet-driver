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

using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Messages;

public class SuccessMessageTests
{
    [Fact]
    public void ShouldHaveCorrectSerializer()
    {
        var message = new SuccessMessage(null);
        message.Serializer.Should().BeOfType<SuccessMessageSerializer>();
    }

    [Fact]
    public void ShouldDispatchToPipelineOnSuccess()
    {
        var pipeline = new Mock<IResponsePipeline>();
        var meta = new Dictionary<string, object>
        {
            ["a"] = "b"
        };

        var message = new SuccessMessage(meta);

        message.Dispatch(pipeline.Object);

        pipeline.Verify(x => x.OnSuccess(meta));
    }

    [Fact]
    public void ShouldHaveIgnoredMessage()
    {
        var meta = new Dictionary<string, object>
        {
            ["a"] = "b"
        };

        new SuccessMessage(meta).ToString().Should().Be("SUCCESS [{a, b}]");
    }
}