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

using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class ResponseWriterTests
{
    [Fact]
    public async Task Writes_the_serialized_message_wrapped_in_response_sentinels()
    {
        var message = Mock.Of<IProtocolMessage>();
        var serializer = new Mock<IMessageSerializer>();
        serializer.Setup(s => s.Serialize(message)).Returns("""{"name":"Sample","data":{}}""");
        var output = new Mock<IConnectionOutput>();
        var writer = new ResponseWriter(output.Object, serializer.Object, Mock.Of<ILogger>());

        await writer.WriteAsync(message);

        output.Verify(o => o.WriteAsync(
            "#response begin\n" +
            """{"name":"Sample","data":{}}""" + "\n" +
            "#response end\n"), Times.Once);
        output.Verify(o => o.FlushAsync(), Times.Once);
    }
}
