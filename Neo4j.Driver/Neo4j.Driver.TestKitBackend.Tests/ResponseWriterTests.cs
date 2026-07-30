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

using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class ResponseWriterTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ResponseWriter>();

    [Fact]
    public async Task Writes_the_serialized_message_wrapped_in_response_sentinels()
    {
        var message = Mock.Of<IProtocolMessage>();
        _autoMocker.GetMock<IMessageSerializer>()
            .Setup(s => s.Serialize(message))
            .Returns("""{"name":"Sample","data":{}}""");
        var writer = _autoMocker.CreateInstance<ResponseWriter>();

        await writer.WriteAsync(message);

        var output = _autoMocker.GetMock<IConnectionOutput>();
        output.Verify(o => o.WriteAsync(
            "#response begin\n" +
            """{"name":"Sample","data":{}}""" + "\n" +
            "#response end\n"), Times.Once);
        output.Verify(o => o.FlushAsync(), Times.Once);
    }
}
