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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Summary;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ExecuteQueryHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ExecuteQueryHandler>();

    private static readonly SummaryResponse StubSummaryResponse =
        new(null!, null, null, null, [], null, null!, null!, null, null, []);

    private Mock<IExecutableQuery<IRecord, IRecord>> SetUpExecutableQuery(
        string cypher,
        QueryConfig queryConfig,
        EagerResult<IReadOnlyList<IRecord>> eagerResult)
    {
        var queryMock = new Mock<IExecutableQuery<IRecord, IRecord>>();
        queryMock.Setup(q => q.WithParameters(It.IsAny<Dictionary<string, object>>())).Returns(queryMock.Object);
        queryMock.Setup(q => q.WithConfig(queryConfig)).Returns(queryMock.Object);
        queryMock.Setup(q => q.ExecuteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(eagerResult);

        _autoMocker.GetMock<IDriver>().Setup(d => d.ExecutableQuery(cypher)).Returns(queryMock.Object);
        return queryMock;
    }

    [Fact]
    public async Task Responds_with_the_keys_mapped_records_and_the_mapped_summary()
    {
        var config = new ExecuteQueryConfig();
        var queryConfig = new QueryConfig();
        _autoMocker.GetMock<IExecuteQueryConfigMapper>().Setup(m => m.Map(config)).Returns(queryConfig);

        _autoMocker.GetMock<ICypherToNativeMapper>()
            .Setup(m => m.Map((Dictionary<string, ICypherValue>?)null))
            .Returns(new Dictionary<string, object>());

        var recordMock = new Mock<IRecord>();
        recordMock.Setup(r => r.Keys).Returns(["n"]);
        recordMock.Setup(r => r["n"]).Returns(1L);
        _autoMocker.GetMock<INativeToCypherMapper>().Setup(m => m.Map(1L)).Returns(new CypherInt(1));

        var summaryMock = Mock.Of<IResultSummary>();
        _autoMocker.GetMock<ISummaryMapper>().Setup(m => m.Map(summaryMock)).Returns(StubSummaryResponse);

        var eagerResult = new EagerResult<IReadOnlyList<IRecord>>([recordMock.Object], summaryMock, ["n"]);
        SetUpExecutableQuery("RETURN 1 AS n", queryConfig, eagerResult);

        var handler = _autoMocker.CreateInstance<ExecuteQueryHandler>();
        var request = new ExecuteQueryRequest
        {
            Driver = new RegistryObject<IDriver>("driver-1", _autoMocker.Get<IDriver>()),
            Cypher = "RETURN 1 AS n",
            Config = config
        };

        EagerResultResponse? response = null;
        _autoMocker.GetMock<IResponseWriter>()
            .Setup(w => w.WriteAsync(It.IsAny<EagerResultResponse>()))
            .Callback<IProtocolMessage>(m => response = m as EagerResultResponse)
            .Returns(Task.CompletedTask);

        await handler.ProcessAsync(request);

        response.Should().NotBeNull();
        response!.Keys.Should().Equal("n");
        response.Records.Should().ContainSingle();
        response.Records[0].Values.Single().Should().BeEquivalentTo(new CypherInt(1));
        response.Summary.Should().BeSameAs(StubSummaryResponse);
    }

    [Fact]
    public async Task Maps_params_and_the_config_through_to_the_query()
    {
        var config = new ExecuteQueryConfig();
        var queryConfig = new QueryConfig();
        _autoMocker.GetMock<IExecuteQueryConfigMapper>().Setup(m => m.Map(config)).Returns(queryConfig);

        var parameters = new Dictionary<string, ICypherValue> { ["p"] = new CypherInt(1) };
        var mappedParameters = new Dictionary<string, object> { ["p"] = 1L };
        _autoMocker.GetMock<ICypherToNativeMapper>().Setup(m => m.Map(parameters)).Returns(mappedParameters);

        var summaryMock = Mock.Of<IResultSummary>();
        _autoMocker.GetMock<ISummaryMapper>().Setup(m => m.Map(summaryMock)).Returns(StubSummaryResponse);

        var eagerResult = new EagerResult<IReadOnlyList<IRecord>>([], summaryMock, []);
        var queryMock = SetUpExecutableQuery("RETURN $p AS n", queryConfig, eagerResult);

        var handler = _autoMocker.CreateInstance<ExecuteQueryHandler>();
        var request = new ExecuteQueryRequest
        {
            Driver = new RegistryObject<IDriver>("driver-1", _autoMocker.Get<IDriver>()),
            Cypher = "RETURN $p AS n",
            Params = parameters,
            Config = config
        };

        await handler.ProcessAsync(request);

        queryMock.Verify(q => q.WithParameters(mappedParameters), Times.Once);
        queryMock.Verify(q => q.WithConfig(queryConfig), Times.Once);
    }
}
