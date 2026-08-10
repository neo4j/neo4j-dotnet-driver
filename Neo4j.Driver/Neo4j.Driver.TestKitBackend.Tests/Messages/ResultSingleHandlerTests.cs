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
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ResultSingleHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ResultSingleHandler>();

    private ResultSingleRequest RequestFor(IResultCursor cursor)
    {
        return new ResultSingleRequest(new RegistryObject<IResultCursor>("result-1", cursor));
    }

    [Fact]
    public async Task Responds_with_the_only_record_mapped_when_exactly_one_remains()
    {
        var record = new Mock<IRecord>();
        record.Setup(r => r.Keys).Returns(["n"]);
        record.Setup(r => r["n"]).Returns(1L);

        var enumeratorMock = new Mock<IAsyncEnumerator<IRecord>>();
        enumeratorMock.SetupSequence(e => e.MoveNextAsync()).ReturnsAsync(true).ReturnsAsync(false);
        enumeratorMock.Setup(e => e.Current).Returns(record.Object);

        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(enumeratorMock.Object);

        var mapperMock = _autoMocker.GetMock<INativeToCypherMapper>();
        mapperMock.Setup(m => m.Map(1L)).Returns(new CypherInt(1));

        var handler = _autoMocker.CreateInstance<ResultSingleHandler>();

        await handler.ProcessAsync(RequestFor(cursorMock.Object));

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(
                w => w.WriteAsync(It.Is<RecordResponse>(r => r.Values.SequenceEqual(new ICypherValue[] { new CypherInt(1) }))),
                Times.Once);
    }

    [Fact]
    public async Task Throws_when_the_stream_is_empty()
    {
        var enumeratorMock = new Mock<IAsyncEnumerator<IRecord>>();
        enumeratorMock.Setup(e => e.MoveNextAsync()).ReturnsAsync(false);

        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(enumeratorMock.Object);

        var handler = _autoMocker.CreateInstance<ResultSingleHandler>();

        var act = () => handler.ProcessAsync(RequestFor(cursorMock.Object));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Throws_when_more_than_one_record_remains()
    {
        var record = new Mock<IRecord>();
        record.Setup(r => r.Keys).Returns(["n"]);
        record.Setup(r => r["n"]).Returns(1L);

        var enumeratorMock = new Mock<IAsyncEnumerator<IRecord>>();
        enumeratorMock.SetupSequence(e => e.MoveNextAsync()).ReturnsAsync(true).ReturnsAsync(true);
        enumeratorMock.Setup(e => e.Current).Returns(record.Object);

        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(enumeratorMock.Object);

        var handler = _autoMocker.CreateInstance<ResultSingleHandler>();

        var act = () => handler.ProcessAsync(RequestFor(cursorMock.Object));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
