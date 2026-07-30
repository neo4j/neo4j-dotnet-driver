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
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ResultConsumeHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ResultConsumeHandler>();

    [Fact]
    public async Task Lets_a_driver_exception_from_consume_propagate()
    {
        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        var exception = new ClientException("boom");
        cursorMock.Setup(c => c.ConsumeAsync()).ThrowsAsync(exception);

        var handler = _autoMocker.CreateInstance<ResultConsumeHandler>();
        var request = new ResultConsumeRequest
        {
            Result = new RegistryObject<IResultCursor>("result-1", cursorMock.Object)
        };

        var act = () => handler.ProcessAsync(request);

        await act.Should().ThrowAsync<ClientException>();
    }

    [Fact]
    public async Task Throws_not_implemented_when_consume_succeeds()
    {
        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.ConsumeAsync()).ReturnsAsync(Mock.Of<IResultSummary>());

        var handler = _autoMocker.CreateInstance<ResultConsumeHandler>();
        var request = new ResultConsumeRequest
        {
            Result = new RegistryObject<IResultCursor>("result-1", cursorMock.Object)
        };

        var act = () => handler.ProcessAsync(request);

        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
