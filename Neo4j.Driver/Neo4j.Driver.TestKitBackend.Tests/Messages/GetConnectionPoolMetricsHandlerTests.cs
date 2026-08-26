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
using Neo4j.Driver.Internal;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class GetConnectionPoolMetricsHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<GetConnectionPoolMetricsHandler>();

    private static Internal.Driver DriverWithPoolMetrics(string poolId, int inUse, int idle)
    {
        var context = new DriverContext(
            new Uri("bolt://localhost:7687"),
            Mock.Of<IAuthTokenManager>(),
            new Config { MetricsEnabled = true });

        var pool = new Mock<Internal.IConnectionPool>();
        pool.SetupGet(p => p.NumberOfInUseConnections).Returns(inUse);
        pool.SetupGet(p => p.NumberOfIdleConnections).Returns(idle);
        context.Metrics.PutPoolMetrics(poolId, pool.Object);

        return new Internal.Driver(context.InitialUri, Mock.Of<IProtocolAdapter>(), context);
    }

    [Fact]
    public async Task ProcessAsync_reports_the_metrics_for_the_pool_matching_the_address()
    {
        var driver = DriverWithPoolMetrics("127.0.0.1:7687-1", inUse: 3, idle: 2);

        await _autoMocker.CreateInstance<GetConnectionPoolMetricsHandler>()
            .ProcessAsync(new GetConnectionPoolMetricsRequest { Driver = driver, Address = "127.0.0.1:7687" });

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new ConnectionPoolMetricsResponse(3, 2)), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_names_the_address_when_no_pool_matches()
    {
        var driver = DriverWithPoolMetrics("127.0.0.1:7687-1", inUse: 3, idle: 2);

        var process = () => _autoMocker.CreateInstance<GetConnectionPoolMetricsHandler>()
            .ProcessAsync(new GetConnectionPoolMetricsRequest { Driver = driver, Address = "10.0.0.9:7687" });

        var exception = await process.Should().ThrowAsync<TestKitProtocolException>();
        exception.Which.Message.Should().Contain("10.0.0.9:7687");
    }
}
