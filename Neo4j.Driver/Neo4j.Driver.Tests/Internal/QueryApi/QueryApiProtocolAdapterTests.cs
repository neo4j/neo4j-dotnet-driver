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
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiProtocolAdapterTests
{
    private readonly Mock<IConnectivityVerifier> _connectivityVerifier = new();
    private readonly Mock<IResolutionScope> _scope = new();
    private readonly Mock<IQueryApiSessionFactory> _sessionFactory = new();

    private QueryApiProtocolAdapter CreateAdapter() =>
        new(_connectivityVerifier.Object, _sessionFactory.Object, _scope.Object);

    [Fact]
    public void Constructor_NullConnectivityVerifier_Throws()
    {
        var act = () => new QueryApiProtocolAdapter(null!, _sessionFactory.Object, _scope.Object);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("connectivityVerifier");
    }

    [Fact]
    public void Constructor_NullSessionFactory_Throws()
    {
        var act = () => new QueryApiProtocolAdapter(_connectivityVerifier.Object, null!, _scope.Object);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("sessionFactory");
    }

    [Fact]
    public void Constructor_NullScope_Throws()
    {
        var act = () => new QueryApiProtocolAdapter(_connectivityVerifier.Object, _sessionFactory.Object, null!);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("scope");
    }

    [Fact]
    public async Task DisposeAsync_DisposesScope()
    {
        await CreateAdapter().DisposeAsync();

        _scope.Verify(s => s.DisposeAsync(), Times.Once);
    }

    [Fact]
    public void CreateSession_DelegatesToFactory()
    {
        var config = new SessionConfig();
        var expected = new Mock<IInternalAsyncSession>().Object;
        _sessionFactory.Setup(f => f.CreateSession(config, true)).Returns(expected);

        var result = CreateAdapter().CreateSession(config, reactive: false, telemetryEnabled: true);

        result.Should().BeSameAs(expected);
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
    public async Task VerifyConnectivityAndGetInfoAsync_ReturnsServerInfoFromVerifier()
    {
        var serverInfo = new Mock<IServerInfo>().Object;
        _connectivityVerifier
            .Setup(h => h.VerifyAsync(default))
            .ReturnsAsync(serverInfo);

        var result = await CreateAdapter().VerifyConnectivityAndGetInfoAsync();

        result.Should().BeSameAs(serverInfo);
    }

    [Fact]
    public async Task VerifyConnectivityAndGetInfoAsync_VerifierThrows_Propagates()
    {
        var exception = new ServiceUnavailableException("down");
        _connectivityVerifier
            .Setup(h => h.VerifyAsync(default))
            .ThrowsAsync(exception);

        var act = () => CreateAdapter().VerifyConnectivityAndGetInfoAsync();

        await act.Should().ThrowAsync<ServiceUnavailableException>().WithMessage("down");
    }
}
