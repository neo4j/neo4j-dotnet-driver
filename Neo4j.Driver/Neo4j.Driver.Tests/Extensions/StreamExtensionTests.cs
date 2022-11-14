using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests;

public class StreamExtensionTests
{
    [Fact]
    public async Task ShouldReadAndThrowOnTimeout()
    {
        const int timeout = 100;
        var moqMemoryStream = new Mock<Stream>();
        moqMemoryStream.Setup(
                x =>
                    x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100, TimeSpan.FromMilliseconds(200));

        var ex = await Record.ExceptionAsync(
            () =>
                moqMemoryStream.Object.ReadWithTimeoutAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    timeout));

        ex.Should().NotBeNull();
        ex.Should()
            .BeOfType<ConnectionReadTimeoutException>()
            .Which.Message.Should()
            .Be($"Socket/Stream timed out after {timeout}ms, socket closed.");
    }

    [Theory]
    [InlineData(300)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public async Task ShouldReadSuccessfullyWithTimeout(int timeout)
    {
        var moqMemoryStream = new Mock<Stream>();
        moqMemoryStream.Setup(
                x =>
                    x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100, TimeSpan.FromMilliseconds(200));

        var ex = await Record.ExceptionAsync(
            () =>
                moqMemoryStream.Object.ReadWithTimeoutAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    timeout));

        ex.Should().BeNull();
    }
}
