﻿// Copyright (c) "Neo4j"
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling
{
    /// <summary>
    /// These are close to integration tests as they used tcp, but they are not integration tests as they do not
    /// integrate against another process. 
    /// </summary>
    public class NetworkedPipelinedMessageReaderTests
    {
        const int Port = 9111;
        
        [Fact]
        public async Task ShouldReadSimpleMessage()
        {
            var pipeline = MockPipeline();
            var logger = new Mock<ILogger>();
            using var cts = new CancellationTokenSource(5000);
            
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    var tcp = new TcpListener(IPAddress.Loopback, Port);
                    TcpClient stream = null;
                    try
                    {
                        tcp.Start();
                        stream = await tcp.AcceptTcpClientAsync(cts.Token);
                        stream.GetStream()
                            .Write(
                                new byte[]
                                {
                                    0x00, 0x03,
                                    PackStream.TinyStruct, MessageFormat.MsgSuccess, PackStream.TinyMap,
                                    0x00, 0x00
                                }.AsSpan());

                        try
                        {
                            await Task.Delay(1000, cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                    finally
                    {
                        stream?.Close();
                        tcp.Stop();
                    }
                }),
                Task.Run(async () =>
                {
                    await Task.Delay(100);
                    var client = new TcpClient();
                    try
                    {
                        await client.ConnectAsync(IPAddress.Loopback, Port, cts.Token);
                        var pipereader = new PipelinedMessageReader(client.GetStream(), -1, logger.Object);
                        await pipereader.ReadAsync(
                            pipeline.Object,
                            new MessageFormat(BoltProtocolVersion.V5_0));
                    }
                    finally
                    {
                        client.Close();
                        cts.Cancel();

                    }
                }));
        
            pipeline.Verify(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task ShouldTimeoutWaitingForSize()
        {
            var pipeline = MockPipeline();
            var logger = new Mock<ILogger>();
            using var cts = new CancellationTokenSource(5000);
            
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    var tcp = new TcpListener(IPAddress.Loopback, Port);
                    TcpClient stream = null;
                    try
                    {
                        tcp.Start();
                        stream = await tcp.AcceptTcpClientAsync(cts.Token);
                        stream.GetStream()
                            .Write(
                                new byte[]
                                {
                                    0x00
                                }.AsSpan());
                        try
                        {
                            await Task.Delay(-1, cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                    finally
                    {
                        stream?.Close();
                        tcp.Stop();
                    }
                }),
                Task.Run(async () =>
                {
     
                    await Task.Delay(100);
                    var client = new TcpClient();
                    try
                    {
                        await client.ConnectAsync(IPAddress.Loopback, Port, cts.Token);
                        var pipereader = new PipelinedMessageReader(client.GetStream(), -1, logger.Object);
                        pipereader.SetReadTimeoutInMs(250);
                        var exc = await Record.ExceptionAsync(
                            async () =>
                            {
                                await pipereader.ReadAsync(
                                    pipeline.Object,
                                    new MessageFormat(BoltProtocolVersion.V5_0));
                            });
                        
                        exc.Should().BeOfType<ConnectionReadTimeoutException>();
                    }
                    finally
                    {
                        client.Close();
                        cts.Cancel();

                    }
                }));
        
            pipeline.Verify(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>()), Times.Never);
        }
        
        [Fact]
        public async Task ShouldReadSimpleMessageWithDelays()
        {
            var pipeline = MockPipeline();
            var logger = new Mock<ILogger>();
            using var cts = new CancellationTokenSource(5000);
            
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    var tcp = new TcpListener(IPAddress.Loopback, Port);
                    TcpClient stream = null;
                    try
                    {
                        tcp.Start();
                        stream = await tcp.AcceptTcpClientAsync(cts.Token);
                        stream.GetStream()
                            .Write(
                                new byte[]
                                {
                                    0x00
                                }.AsSpan());

                        await Task.Delay(500);
                        
                        stream.GetStream()
                            .Write(
                                new byte[]
                                {
                                    0x03,
                                    PackStream.TinyStruct, MessageFormat.MsgSuccess, PackStream.TinyMap,
                                }.AsSpan());

                        await Task.Delay(500);
                        stream.GetStream()
                            .Write(
                                new byte[]
                                {
                                    0x00, 0x00
                                }.AsSpan());
                        try
                        {
                            await Task.Delay(1000, cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                    finally
                    {
                        stream?.Close();
                        tcp.Stop();
                    }
                }),
                Task.Run(async () =>
                {
                    await Task.Delay(100);
                    var client = new TcpClient();
                    try
                    {
                        await client.ConnectAsync(IPAddress.Loopback, Port, cts.Token);
                        var pipereader = new PipelinedMessageReader(client.GetStream(), -1, logger.Object);
                        pipereader.SetReadTimeoutInMs(1000);
                        await pipereader.ReadAsync(
                            pipeline.Object,
                            new MessageFormat(BoltProtocolVersion.V5_0));
                    }
                    finally
                    {
                        client.Close();
                        cts.Cancel();

                    }
                }));
        
            pipeline.Verify(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>()), Times.Once);
        }
        
        [Fact]
        public async Task ShouldTimeoutWaitingAfterSize()
        {
            var pipeline = MockPipeline();
            var logger = new Mock<ILogger>();
            using var cts = new CancellationTokenSource(5000);
            
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    var tcp = new TcpListener(IPAddress.Loopback, Port);
                    TcpClient stream = null;
                    try
                    {
                        tcp.Start();
                        stream = await tcp.AcceptTcpClientAsync(cts.Token);
                        stream.GetStream()
                            .Write(
                                new byte[]
                                {
                                    0x00,0x03
                                }.AsSpan());

                        try
                        {
                            await Task.Delay(-1, cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                    finally
                    {
                        stream?.Close();
                        tcp.Stop();
                    }
                }),
                Task.Run(async () =>
                {
     
                    await Task.Delay(100);
                    var client = new TcpClient();
                    try
                    {
                        await client.ConnectAsync(IPAddress.Loopback, Port, cts.Token);
                        var pipereader = new PipelinedMessageReader(client.GetStream(), -1, logger.Object);
                        pipereader.SetReadTimeoutInMs(2000);
                        var exc = await Record.ExceptionAsync(
                            async () =>
                            {
                                await pipereader.ReadAsync(
                                    pipeline.Object,
                                    new MessageFormat(BoltProtocolVersion.V5_0));
                            });
                        
                        exc.Should().BeOfType<ConnectionReadTimeoutException>();
                    }
                    finally
                    {
                        client.Close();
                        cts.Cancel();

                    }
                }));
        
            pipeline.Verify(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>()), Times.Never);
        }
        
        [Fact]
        public async Task ShouldTimeoutWaitingForData()
        {
            var pipeline = MockPipeline();
            var logger = new Mock<ILogger>();
            using var cts = new CancellationTokenSource(5000);
            
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    var tcp = new TcpListener(IPAddress.Loopback, Port);
                    TcpClient stream = null;
                    try
                    {
                        tcp.Start();
                        stream = await tcp.AcceptTcpClientAsync(cts.Token);
                        stream.GetStream()
                            .Write(
                                new byte[]
                                {
                                    0x00,0x03
                                }.AsSpan());

                        await Task.Delay(1000);
                        stream.GetStream()
                            .Write(
                                new byte[]
                                {
                                    0x00
                                }.AsSpan());
                        
                        try
                        {
                            await Task.Delay(-1, cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                    finally
                    {
                        stream?.Close();
                        tcp.Stop();
                    }
                }),
                Task.Run(async () =>
                {
     
                    await Task.Delay(100);
                    var client = new TcpClient();
                    try
                    {
                        await client.ConnectAsync(IPAddress.Loopback, Port, cts.Token);
                        var pipereader = new PipelinedMessageReader(client.GetStream(), -1, logger.Object);
                        pipereader.SetReadTimeoutInMs(2000);
                        var exc = await Record.ExceptionAsync(
                            async () =>
                            {
                                await pipereader.ReadAsync(
                                    pipeline.Object,
                                    new MessageFormat(BoltProtocolVersion.V5_0));
                            });
                        
                        exc.Should().BeOfType<ConnectionReadTimeoutException>();
                    }
                    finally
                    {
                        client.Close();
                        cts.Cancel();

                    }
                }));
        
            pipeline.Verify(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>()), Times.Never);
        }
        
        private static Mock<IResponsePipeline> MockPipeline()
        {
            var done = (object)false;
            var pipeline = new Mock<IResponsePipeline>();
            pipeline.Setup(x => x.OnSuccess(It.IsAny<Dictionary<string, object>>())).Callback(() => done = true);
            pipeline.SetupGet(x => x.HasNoPendingMessages).Returns(() => (bool)done);
            return pipeline;
        }
    }
}
