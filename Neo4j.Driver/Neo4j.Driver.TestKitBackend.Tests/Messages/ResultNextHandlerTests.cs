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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ResultNextHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ResultNextHandler>();

    private ResultNextRequest RequestFor(IResultCursor cursor)
    {
        return new ResultNextRequest { Result = new RegistryObject<IResultCursor>("result-1", cursor) };
    }

    [Fact]
    public async Task Responds_with_a_record_of_mapped_values_when_a_record_is_available()
    {
        var record = new Mock<IRecord>();
        record.Setup(r => r.Keys).Returns(["n", "s"]);
        record.Setup(r => r["n"]).Returns(1L);
        record.Setup(r => r["s"]).Returns("hi");

        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.FetchAsync()).ReturnsAsync(true);
        cursorMock.Setup(c => c.Current).Returns(record.Object);

        var mapperMock = _autoMocker.GetMock<INativeToCypherMapper>();
        mapperMock.Setup(m => m.Map(1L)).Returns(new CypherInt(1));
        mapperMock.Setup(m => m.Map("hi")).Returns(new CypherString("hi"));

        var handler = _autoMocker.CreateInstance<ResultNextHandler>();

        var response = await handler.ProcessAsync(RequestFor(cursorMock.Object));

        var recordResponse = response.Should().BeOfType<RecordResponse>().Subject;
        recordResponse.Values.Should().Equal(new CypherInt(1), new CypherString("hi"));
    }

    [Fact]
    public async Task Responds_with_null_record_when_there_is_no_next_record()
    {
        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.FetchAsync()).ReturnsAsync(false);

        var handler = _autoMocker.CreateInstance<ResultNextHandler>();

        var response = await handler.ProcessAsync(RequestFor(cursorMock.Object));

        response.Should().BeOfType<NullRecordResponse>();
    }
}
