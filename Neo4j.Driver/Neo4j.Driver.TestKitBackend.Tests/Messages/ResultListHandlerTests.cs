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
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ResultListHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ResultListHandler>();

    private ResultListRequest RequestFor(IResultCursor cursor)
    {
        return new ResultListRequest(new Stored<IResultCursor>("result-1", cursor));
    }

    [Fact]
    public async Task Responds_with_an_empty_record_list_when_the_stream_has_no_records()
    {
        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.FetchAsync()).ReturnsAsync(false);

        var handler = _autoMocker.CreateInstance<ResultListHandler>();

        await handler.ProcessAsync(RequestFor(cursorMock.Object));

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(It.Is<RecordListResponse>(r => r.Records.Count == 0)), Times.Once);
    }

    [Fact]
    public async Task Responds_with_every_remaining_record_mapped_in_order()
    {
        var first = new Mock<IRecord>();
        first.Setup(r => r.Keys).Returns(["n"]);
        first.Setup(r => r["n"]).Returns(1L);

        var second = new Mock<IRecord>();
        second.Setup(r => r.Keys).Returns(["n"]);
        second.Setup(r => r["n"]).Returns(2L);

        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.SetupSequence(c => c.FetchAsync())
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        cursorMock.SetupSequence(c => c.Current)
            .Returns(first.Object)
            .Returns(second.Object);

        var mapperMock = _autoMocker.GetMock<INativeToCypherMapper>();
        mapperMock.Setup(m => m.Map(1L)).Returns(new CypherInt(1));
        mapperMock.Setup(m => m.Map(2L)).Returns(new CypherInt(2));

        var handler = _autoMocker.CreateInstance<ResultListHandler>();

        await handler.ProcessAsync(RequestFor(cursorMock.Object));

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(
                w => w.WriteAsync(
                    It.Is<RecordListResponse>(
                        r => r.Records.Select(entry => entry.Values.Single())
                            .SequenceEqual(new ICypherValue[] { new CypherInt(1), new CypherInt(2) }))),
                Times.Once);
    }
}
