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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ResultPeekHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ResultPeekHandler>();

    private ResultPeekRequest RequestFor(IResultCursor cursor)
    {
        return new ResultPeekRequest(new RegistryObject<IResultCursor>("result-1", cursor));
    }

    [Fact]
    public async Task Responds_with_a_record_of_mapped_values_without_advancing_the_cursor()
    {
        var record = new Mock<IRecord>();
        record.Setup(r => r.Keys).Returns(["n"]);
        record.Setup(r => r["n"]).Returns(1L);

        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.PeekAsync()).ReturnsAsync(record.Object);

        var mapperMock = _autoMocker.GetMock<INativeToCypherMapper>();
        mapperMock.Setup(m => m.Map(1L)).Returns(new CypherInt(1));

        var handler = _autoMocker.CreateInstance<ResultPeekHandler>();

        await handler.ProcessAsync(RequestFor(cursorMock.Object));

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(
                w => w.WriteAsync(It.Is<RecordResponse>(r => r.Values.SequenceEqual(new ICypherValue[] { new CypherInt(1) }))),
                Times.Once);
        cursorMock.Verify(c => c.FetchAsync(), Times.Never);
    }

    [Fact]
    public async Task Responds_with_null_record_when_there_is_no_next_record()
    {
        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.PeekAsync()).ReturnsAsync((IRecord)null!);

        var handler = _autoMocker.CreateInstance<ResultPeekHandler>();

        await handler.ProcessAsync(RequestFor(cursorMock.Object));

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new NullRecordResponse()), Times.Once);
    }
}
