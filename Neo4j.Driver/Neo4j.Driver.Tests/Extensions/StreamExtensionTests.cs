// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using System.IO;
using System.Net;
using System.Net.Sockets;
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
        async Task RunServer(CancellationToken token)
        {
            var tcpServer = new TcpListener(IPAddress.Loopback, 32411);
            try
            {
                tcpServer.Start();
                using var socket = await tcpServer.AcceptSocketAsync(token);
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(10, token);
                }
            }
            catch (TaskCanceledException)
            {
                // ignore.
            }
            finally
            {
                tcpServer.Stop();
            }
        }
        
        var source = new CancellationTokenSource();
        var server = RunServer(source.Token);

        try
        {
            using var socket = new TcpClient();
            await socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 32411));
            await using var stream = socket.GetStream();
            const int timeout = 100;

            var ex = await Record.ExceptionAsync(() => stream.ReadWithTimeoutAsync(new byte[1], 0, 1, timeout));

            ex.Should()
                .BeOfType<ConnectionReadTimeoutException>()
                .Which.Message.Should()
                .Be($"Socket/Stream timed out after {timeout}ms, socket closed.");
        }
        finally
        {
            source.Cancel();
            try
            {
                await server;
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
        }
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
