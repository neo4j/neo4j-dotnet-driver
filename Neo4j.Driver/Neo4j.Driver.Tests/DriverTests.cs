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

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Xunit;
using InternalDriver = Neo4j.Driver.Internal.Driver;

#pragma warning disable CS0618

namespace Neo4j.Driver.Tests;

public class DriverTests
{
    [Fact]
    public void ShouldUseDefaultPortWhenPortNotSet()
    {
        using (var driver = (InternalDriver)GraphDatabase.Driver("bolt://localhost"))
        {
            driver.Uri.Port.Should().Be(7687);
            driver.Uri.Scheme.Should().Be("bolt");
            driver.Uri.Host.Should().Be("localhost");
        }
    }

    [Fact]
    public void ShouldUseSpecifiedPortWhenPortSet()
    {
        using (var driver = (InternalDriver)GraphDatabase.Driver("bolt://localhost:8888"))
        {
            driver.Uri.Port.Should().Be(8888);
            driver.Uri.Scheme.Should().Be("bolt");
            driver.Uri.Host.Should().Be("localhost");
        }
    }

    [Fact]
    public void ShouldSupportIPv6()
    {
        using (var driver = (InternalDriver)GraphDatabase.Driver("bolt://[::1]"))
        {
            driver.Uri.Port.Should().Be(7687);
            driver.Uri.Scheme.Should().Be("bolt");
            driver.Uri.Host.Should().Be("[::1]");
        }
    }

    [Fact]
    public void ShouldErrorIfUriWrongFormat()
    {
        var exception = Record.Exception(() => GraphDatabase.Driver("bolt://*"));
        exception.Should().BeOfType<UriFormatException>();
    }

    [Fact]
    public void ShouldErrorIfBoltSchemeWithRoutingContext()
    {
        var exception = Record.Exception(() => GraphDatabase.Driver("bolt://localhost/?name=molly&age=1&color=white"));

        exception.Should().BeOfType<ArgumentException>();
        exception.Message.Should().Contain("Routing context are not supported with scheme 'bolt'");
    }

    [Fact]
    public void ShouldAcceptIfRoutingSchemeWithRoutingContext()
    {
        using (var driver =
               (InternalDriver)GraphDatabase.Driver("neo4j://localhost/?name=molly&age=1&color=white"))
        {
            driver.Uri.Port.Should().Be(7687);
            driver.Uri.Scheme.Should().Be("neo4j");
            driver.Uri.Host.Should().Be("localhost");
        }
    }

    [Fact]
    public void DisposeClosesDriver()
    {
        var driver = GraphDatabase.Driver("bolt://localhost");
        driver.Dispose();

        var ex = Record.Exception(() => driver.AsyncSession());
        ex.Should().NotBeNull();
        ex.Should().BeOfType<ObjectDisposedException>();
    }

    [Fact]
    public async Task CloseClosesDriver()
    {
        var driver = GraphDatabase.Driver("bolt://localhost");
        await driver.CloseAsync();

        var ex = Record.Exception(() => driver.AsyncSession());
        ex.Should().NotBeNull();
        ex.Should().BeOfType<ObjectDisposedException>();
    }

    [Fact]
    public async void CloseAsyncClosesDriver()
    {
        var driver = GraphDatabase.Driver("bolt://localhost");
        await driver.CloseAsync();

        var ex = Record.Exception(() => driver.AsyncSession());
        ex.Should().NotBeNull();
        ex.Should().BeOfType<ObjectDisposedException>();
    }

    [Fact]
    public async void MultipleCloseAndDisposeIsValidOnDriver()
    {
        var driver = GraphDatabase.Driver("bolt://localhost");
        await driver.CloseAsync();
        driver.Dispose();
        await driver.CloseAsync();

        var ex = Record.Exception(() => driver.AsyncSession());
        ex.Should().NotBeNull();
        ex.Should().BeOfType<ObjectDisposedException>();
    }

    [Fact]
    public async void ShouldVerifyConnection()
    {
        var mock = new Mock<IConnectionProvider>();
        mock.Setup(x => x.VerifyConnectivityAndGetInfoAsync())
            .Returns(Task.FromResult(new Mock<IServerInfo>().Object));

        var driver = new InternalDriver(new Uri("bolt://localhost"), mock.Object, null, TestDriverContext.MockContext);
        await driver.VerifyConnectivityAsync();

        mock.Verify(x => x.VerifyConnectivityAndGetInfoAsync(), Times.Once);
    }

    [Fact]
    public async void ShouldTryVerifyConnection()
    {
        var mock = new Mock<IConnectionProvider>();
        mock.Setup(x => x.VerifyConnectivityAndGetInfoAsync())
            .Returns(Task.FromResult(new Mock<IServerInfo>().Object));

        var driver = (IDriver)new InternalDriver(
            new Uri("bolt://localhost"),
            mock.Object,
            null,
            TestDriverContext.MockContext);

        var connects = await driver.TryVerifyConnectivityAsync();

        connects.Should().BeTrue();
    }

    [Fact]
    public async void ShouldCatchInTryVerifyConnection()
    {
        var mock = new Mock<IConnectionProvider>();
        mock.Setup(x => x.VerifyConnectivityAndGetInfoAsync())
            .ThrowsAsync(new Exception("broken"));

        var driver = (IDriver)new InternalDriver(
            new Uri("bolt://localhost"),
            mock.Object,
            null,
            TestDriverContext.MockContext);

        var connects = await driver.TryVerifyConnectivityAsync();

        connects.Should().BeFalse();
    }

    [Fact]
    public async void ShouldGetInfoConnection()
    {
        var mockServerInfo = new Mock<IServerInfo>().Object;
        var mock = new Mock<IConnectionProvider>();
        mock.Setup(x => x.VerifyConnectivityAndGetInfoAsync())
            .Returns(Task.FromResult(mockServerInfo));

        var driver = new InternalDriver(
            new Uri("bolt://localhost"),
            mock.Object,
            null,
            TestDriverContext.MockContext);

        var info = await driver.GetServerInfoAsync();

        mock.Verify(x => x.VerifyConnectivityAndGetInfoAsync(), Times.Once);
        info.Should().Be(mockServerInfo);
    }

    [Fact]
    public async void ShouldTestSupportMultiDb()
    {
        var mock = new Mock<IConnectionProvider>();
        mock.Setup(x => x.SupportsMultiDbAsync()).Returns(Task.FromResult(true));
        var driver = new InternalDriver(
            new Uri("bolt://localhost"),
            mock.Object,
            null,
            TestDriverContext.MockContext);

        await driver.SupportsMultiDbAsync();

        mock.Verify(x => x.SupportsMultiDbAsync(), Times.Once);
    }
}
