// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class TcpSocketClientTests
    {
        internal class TcpSocketClientWithDisposeDetection : TcpSocketClient
        {
            public TcpSocketClientWithDisposeDetection(SocketSettings socketSettings, IDriverLogger logger = null) :
                base(socketSettings, logger)
            {
            }

            public override Task DisconnectAsync()
            {
                DisposeCalled = true;
                return base.DisconnectAsync();
            }

            public bool DisposeCalled { get; set; }
        }

        public class ConnectSocketAsyncMethod
        {
            [Fact]
            public async Task ShouldThrowExceptionIfConnectionTimedOut()
            {
                var client = new TcpSocketClientWithDisposeDetection(
                    new SocketSettings
                    {
                        ConnectionTimeout = TimeSpan.FromSeconds(1),
                        HostResolver = new SystemHostResolver(),
                        EncryptionManager =
                            new EncryptionManager(EncryptionLevel.None, null, null)
                    });

                // ReSharper disable once PossibleNullReferenceException
                // use non-routable IP address to mimic a connect timeout
                // https://stackoverflow.com/questions/100841/artificially-create-a-connection-timeout-error
                var exception = await Record.ExceptionAsync(
                    () => client.ConnectSocketAsync(IPAddress.Parse("192.168.0.0"), 9999));
                exception.Should().NotBeNull();
                exception.Should().BeOfType<OperationCanceledException>(exception.ToString());
                exception.Message.Should().Be("Failed to connect to server 192.168.0.0:9999 within 1000ms.");
                client.DisposeCalled.Should().BeTrue();
            }

            [Fact]
            public async Task ShouldBeAbleToConnectAgainIfFirstFailed()
            {
                var socketSettings = new SocketSettings
                {
                    ConnectionTimeout = TimeSpan.FromSeconds(10),
                    HostResolver = new SystemHostResolver(),
                    EncryptionManager =
                        new EncryptionManager(EncryptionLevel.None, null, null)
                };
                var client = new TcpSocketClient(socketSettings);

                // We fail to connect the first time as there is no server to connect to
                // ReSharper disable once PossibleNullReferenceException
                var exception = await Record.ExceptionAsync(
                    async () => await client.ConnectSocketAsync(IPAddress.Parse("127.0.0.1"), 54321));
                // start a server on port 20003

                var serverSocket = new TcpListener(new IPEndPoint(IPAddress.Loopback, 54321));
                try
                {
                    serverSocket.Start();

                    // We should not get any error this time as server in online now.
                    await client.ConnectSocketAsync(IPAddress.Parse("127.0.0.1"), 54321);
                }
                finally
                {
                    serverSocket.Stop();
                }
            }
        }

        public class ConnectAsyncMethod
        {
            [Fact]
            public async Task ShouldThrowExceptionIfConnectionTimedOut()
            {
                var client = new TcpSocketClient(new SocketSettings
                {
                    ConnectionTimeout = TimeSpan.FromSeconds(1),
                    HostResolver = new SystemHostResolver(),
                    EncryptionManager = new EncryptionManager(EncryptionLevel.None, null, null)
                });

                // ReSharper disable once PossibleNullReferenceException
                // use non-routable IP address to mimic a connect timeout
                // https://stackoverflow.com/questions/100841/artificially-create-a-connection-timeout-error
                var exception = await Record.ExceptionAsync(
                    () => client.ConnectAsync(new Uri("bolt://192.168.0.0:9998")));
                exception.Should().NotBeNull();
                exception.Should().BeOfType<IOException>();
                exception.Message.Should().Be(
                    "Failed to connect to server 'bolt://192.168.0.0:9998/' via IP addresses'[192.168.0.0]' at port '9998'.");

                var baseException = exception.GetBaseException();
                baseException.Should().BeOfType<OperationCanceledException>(exception.ToString());
                baseException.Message.Should().Be("Failed to connect to server 192.168.0.0:9998 within 1000ms.");
            }
        }
    }
}