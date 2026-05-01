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

#nullable enable

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiProtocolAdapterTests
{
    private readonly Mock<IVerifyConnectivityHandler> _connectivityHandler = new();
    private readonly Mock<IQueryApiSessionFactory> _sessionFactory = new();

    private QueryApiProtocolAdapter CreateAdapter() =>
        new(_connectivityHandler.Object, _sessionFactory.Object);


    [Fact]
    public void Constructor_NullVerifyConnectivityHandler_Throws()
    {
        var act = () => new QueryApiProtocolAdapter(null!, _sessionFactory.Object);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("verifyConnectivityHandler");
    }

    [Fact]
    public void Constructor_NullSessionFactory_Throws()
    {
        var act = () => new QueryApiProtocolAdapter(_connectivityHandler.Object, null!);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("sessionFactory");
    }

    [Fact]
    public void CreateSession_DelegatesToFactory()
    {
        var config = new SessionConfig();
        var expected = new Mock<IInternalAsyncSession>().Object;
        _sessionFactory.Setup(f => f.CreateSession(config, true)).Returns(expected);

        var result = CreateAdapter().CreateSession(config, reactive: false, telemetryEnabled: true);

        result.Should().BeSameAs(expected);
        _sessionFactory.Verify(f => f.CreateSession(config, true), Times.Once);
    }

    [Fact]
    public void CreateSession_PassesSessionConfigUnmodified()
    {
        var config = new SessionConfig();
        _sessionFactory
            .Setup(f => f.CreateSession(It.IsAny<SessionConfig>(), It.IsAny<bool>()))
            .Returns(new Mock<IInternalAsyncSession>().Object);

        CreateAdapter().CreateSession(config, reactive: false, telemetryEnabled: false);

        _sessionFactory.Verify(f => f.CreateSession(config, false), Times.Once);
    }

    [Fact]
    public void CreateSession_Reactive_ThrowsNotSupported()
    {
        var act = () => CreateAdapter().CreateSession(new SessionConfig(), reactive: true, telemetryEnabled: false);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public async Task SupportsMultiDbAsync_ReturnsTrue()
    {
        var result = await CreateAdapter().SupportsMultiDbAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SupportsReAuthAsync_ReturnsTrue()
    {
        var result = await CreateAdapter().SupportsReAuthAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public void GetRoutingTable_ThrowsNotSupported()
    {
        var act = () => CreateAdapter().GetRoutingTable("neo4j");
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public async Task VerifyConnectivityAndGetInfoAsync_CallsHandler()
    {
        var serverInfo = new Mock<IServerInfo>().Object;
        _connectivityHandler
            .Setup(h => h.VerifyConnectivityAsync(default))
            .ReturnsAsync(serverInfo);

        var result = await CreateAdapter().VerifyConnectivityAndGetInfoAsync();

        result.Should().BeSameAs(serverInfo);
        _connectivityHandler.Verify(h => h.VerifyConnectivityAsync(default), Times.Once);
    }

    [Fact]
    public async Task VerifyConnectivityAndGetInfoAsync_HandlerThrows_Propagates()
    {
        var exception = new ServiceUnavailableException("down");
        _connectivityHandler
            .Setup(h => h.VerifyConnectivityAsync(default))
            .ThrowsAsync(exception);

        var act = () => CreateAdapter().VerifyConnectivityAndGetInfoAsync();

        await act.Should().ThrowAsync<ServiceUnavailableException>().WithMessage("down");
    }
}
