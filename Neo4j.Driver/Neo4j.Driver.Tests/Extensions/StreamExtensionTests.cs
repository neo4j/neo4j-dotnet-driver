// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
// you may not use this file except in compliance with the License.
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
