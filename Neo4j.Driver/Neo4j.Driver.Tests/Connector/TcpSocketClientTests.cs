// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License");
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
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class TcpSocketClientTests
    {
        internal class TcpSocketClientWithDisposeDetection : TcpSocketClient
        {
            public TcpSocketClientWithDisposeDetection(SocketSettings socketSettings, ILogger logger = null) : base(socketSettings, logger)
            {
            }

            protected override void CloseClient()
            {
                DisposeCalled = true;
                base.CloseClient();
            }

            public bool DisposeCalled { get; set; }
        }

        private static Task ServerOnPort(int port)
        {
            var serverSocket = new TcpListener(port);
            serverSocket.Start();
            var server = new Task(() =>
            {
                // wait for a client to connect
                serverSocket.AcceptTcpClient();
                serverSocket.Stop();
            });
            return server;
        }

        public class ConnectSocketAsyncMethod
        {
            [Fact]
            public async Task ShouldThrowExceptionIfConnectionTimedOut()
            {
                var client = new TcpSocketClientWithDisposeDetection(
                    new SocketSettings { ConnectionTimeout = TimeSpan.FromSeconds(0) });

                var exception = await Record.ExceptionAsync(
                    ()=>client.ConnectSocketAsync(IPAddress.Parse("127.0.0.1"), 9999));
                exception.Should().BeOfType<OperationCanceledException>();
                exception.Message.Should().Be("Failed to connect to server 127.0.0.1:9999 within 0ms.");

                client.DisposeCalled.Should().BeTrue();
            }

            [Fact]
            public async Task ShouldBeAbleToConnectAgainIfFirstFailed()
            {
                var socketSettings = new SocketSettings{ConnectionTimeout = TimeSpan.FromSeconds(10)};
                var client = new TcpSocketClient(socketSettings);

                // We fail to connect the first time as there is no server to connect to
                var exception = await Record.ExceptionAsync(
                    async()=> await client.ConnectSocketAsync(IPAddress.Parse("127.0.0.1"), 20003));
                // start a server on port 20003

                ServerOnPort(20003).Start();

                // We should not get any error this time as server in online now.
                await client.ConnectSocketAsync(IPAddress.Parse("127.0.0.1"), 20003);
            }
        }

        public class ConnectAsyncMethod
        {
            [Fact]
            public async Task ShouldThrowIOExceptionIfConnTimedOut()
            {
                var client = new TcpSocketClient(new SocketSettings{ConnectionTimeout = TimeSpan.FromSeconds(0)});
                var exception = await Record.ExceptionAsync(
                    ()=>client.ConnectAsync(new Uri("bolt://127.0.0.1:9999")));
                exception.Should().BeOfType<IOException>();
                exception.Message.Should().Be(
                    "Failed to connect to server 'bolt://127.0.0.1:9999/' via IP addresses'[127.0.0.1]' at port '9999'.");
                var baseException = exception.GetBaseException();
                baseException.Should().BeOfType<OperationCanceledException>();
                baseException.Message.Should().Be("Failed to connect to server 127.0.0.1:9999 within 0ms.");
            }
        }
    }
}
